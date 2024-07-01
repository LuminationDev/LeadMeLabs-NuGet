using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace LeadMeLabsLibrary;

/// <summary>
/// A class designed to check the currently installed version of .net against the stored version in the LeadMe Labs
/// vultr bucket. If a newer version is detected, this class will handle the download and silent installation of it.
/// </summary>
public static class DotNet
{
    // A callback for the logger function
    public delegate void CallbackDelegate(string logMessage, Enums.LogLevel logLevel, bool writeToLogFile = true);
    private static CallbackDelegate? _logCallback;

    private const string BaseUrl = "https://leadme-external.sgp1.vultrobjects.com/dot-net";
    private static readonly string DownloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
    
    /// <summary>
    /// Check if there is a pending dotnet update. Only perform the check if it is a Friday, if the local version
    /// is below that of the version hosted on Vultr at 'https://leadme-external.sgp1.vultrobjects.com/dot-net' then
    /// perform the download and update.
    /// </summary>
    /// <param name="callback">The logger function with the expected parameters types of (string, Enums.LogLevel bool)</param>
    /// <returns>A string of the newly downloaded file path or an empty string if no action is required.</returns>
    public static async Task<string> CheckForDotNetUpdate(CallbackDelegate? callback)
    {
        _logCallback = callback;

        if (!IsFriyay()) return string.Empty; 
        
        //Check the local and remote .net version
        Version required = await CompareVersions();
        if (required == new Version()) return string.Empty;
        
        //Attempt to download the .exe file
        string? filePath = await DownloadDotNet(required);
        return filePath ?? string.Empty;
    }
    
    /// <summary>
    /// Get the most recent dotnet version installed on the station.
    /// </summary>
    /// <returns>A string of the current dotnet version in the format x.x.x</returns>
    public static string GetMostRecentRuntimeVersion()
    {
        string[] runtimePaths = {
            // @"C:\Program Files\dotnet\shared\Microsoft.NETCore.App",
            // @"C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App",
            @"C:\Program Files\dotnet\shared\Microsoft.WindowsDesktop.App" //only require this one
        };

        string? mostRecentVersion = null;

        foreach (string path in runtimePaths)
        {
            if (!Directory.Exists(path)) continue;
            
            var versions = Directory.GetDirectories(path)
                .Select(Path.GetFileName)
                .Where(v => Version.TryParse(v, out _))
                .OrderByDescending(v =>
                {
                    if (v != null) return new Version(v);
                    return null;
                })
                .ToList();
            if (!versions.Any()) continue;
            
            var latestVersion = versions.First();
            if (latestVersion == null) continue;
            if (mostRecentVersion == null || new Version(latestVersion) > new Version(mostRecentVersion))
            {
                mostRecentVersion = latestVersion;
            }
        }

        return mostRecentVersion ?? string.Empty;
    }
    
    /// <summary>
    /// Trigger the supplied logger callback.
    /// </summary>
    /// <param name="message">A string of the message to log.</param>
    /// <param name="level">A LogLevel enum of the type of message that is supplied.</param>
    private static void TriggerLogCallback(string message, Enums.LogLevel level)
    {
        _logCallback?.Invoke(message, level);
    }

    /// <summary>
    /// Compares the local .NET runtime version with a remote version obtained from the specified Vultr URL.
    /// </summary>
    /// <returns>
    /// Returns the remote version if it is later than the local version; otherwise, returns a new <see cref="Version"/> with default value (0.0.0).
    /// </returns>
    /// <remarks>
    /// The function attempts to fetch the remote version from the given base URL and compare it with the local environment version.
    /// If the remote version is earlier than or the same as the local version, a default version is returned. 
    /// If the request to the remote URL fails, an exception message is logged, and the comparison defaults to indicating no update is necessary.
    /// </remarks>
    private static async Task<Version> CompareVersions()
    {
        string mostRecent = GetMostRecentRuntimeVersion();
        if (string.IsNullOrEmpty(mostRecent)) return new Version();
        
        Version local = new Version(mostRecent);
        Version remote = new Version();
        
        try
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);
            var response = httpClient.GetAsync($"{BaseUrl}/version").GetAwaiter().GetResult();
            if (response.IsSuccessStatusCode)
            {
                // Read the content of the response as a string
                string content = await response.Content.ReadAsStringAsync();
                remote = new Version(content);
            }
            else
            {
                TriggerLogCallback($"Unable to reach dotnet version", Enums.LogLevel.Update);
            }
        }
        catch (Exception e)
        {
            TriggerLogCallback($"Unable to reach dotnet version: {e}", Enums.LogLevel.Update);
            return new Version();
        }

        //Compare differences - return a 0 version if no difference or ahead
        int comparison = local.CompareTo(remote);
        switch (comparison)
        {
            case < 0:
                TriggerLogCallback($"{local} is earlier than {remote}", Enums.LogLevel.Update);
                return remote;
            case > 0:
                TriggerLogCallback($"{local} is later than {remote}", Enums.LogLevel.Update);
                return new Version();
            default:
                TriggerLogCallback($"{local} is the same as {remote}", Enums.LogLevel.Update);
                return new Version();
        }
    }

    /// <summary>
    /// Downloads the .NET SDK from Vultr and saves it to the Downloads folder.
    /// </summary>
    /// <param name="required">The <see cref="Version"/> of the .NET SDK to save the file as.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <remarks>
    /// The function constructs the download URL using the base URL and the required version,
    /// sends an asynchronous GET request to fetch the installer, and saves it to the Downloads folder.
    /// The download progress is logged to the console. If any error occurs during the HTTP request
    /// or file I/O operations, an appropriate error message is logged to the console.
    /// </remarks>
    private static async Task<string?> DownloadDotNet(Version required)
    {
        string filePath = Path.Combine(DownloadsPath, $"windowsdesktop-runtime-{required}-win-x64.exe");
        
        try
        {
            using HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync($"{BaseUrl}/dotnet-desktop.exe", HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            
            var totalBytes = response.Content.Headers.ContentLength;
            await using (Stream contentStream = await response.Content.ReadAsStreamAsync(), fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None)) {
                byte[] buffer = new byte[81920];
                long totalRead = 0;
                int bytesRead;
                int lastLoggedPercent = 0;

                Console.WriteLine($"Download progress: 0%");
                while ((bytesRead = await contentStream.ReadAsync(buffer)) != 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                    totalRead += bytesRead;

                    // Calculate and log the progress percentage every 10%
                    if (totalBytes.HasValue)
                    {
                        double progress = (double)totalRead / totalBytes.Value * 100;
                        int currentPercent = (int)progress;

                        if (currentPercent < lastLoggedPercent + 10) continue;
                        
                        TriggerLogCallback($"Download progress: {currentPercent}%", Enums.LogLevel.Update);
                        lastLoggedPercent = currentPercent;
                    }
                    else
                    {
                        TriggerLogCallback($"Downloaded {totalRead} bytes", Enums.LogLevel.Update);
                    }
                }
            }

            TriggerLogCallback($"File downloaded and saved to {filePath}", Enums.LogLevel.Update);
        }
        catch (HttpRequestException e)
        {
            TriggerLogCallback($"Request error: {e.Message}", Enums.LogLevel.Update);
            return null;
        }
        catch (IOException e)
        {
            TriggerLogCallback($"File error: {e.Message}", Enums.LogLevel.Update);
            return null;
        }

        return filePath;
    }
    
    /// <summary>
    /// Check if the current day is a Friyay. The dot-net check/update will only be performed on a Friyay.
    /// </summary>
    /// <returns>A bool, true if Friyay otherwise false.</returns>
    private static bool IsFriyay()
    {
        DateTime currentDate = DateTime.Now;
        return currentDate.DayOfWeek is DayOfWeek.Friday;
    }
}
