using System.Diagnostics;
using System.Security.Principal;
using System.Text;

namespace LeadMeLabsLibrary;

public static class WindowsUpdates
{
    private const string PsGalleryRepo = "PSGallery";
    private const string ModuleName = "PSWindowsUpdate";
    private const string ModuleVersion = "2.2.0.3";
    
    // A callback for the logger function
    public delegate void CallbackDelegate(string logMessage, Enums.LogLevel logLevel, bool writeToLogFile = true);
    private static CallbackDelegate? _logCallback;
    
    /// <summary>
    /// Perform a Windows update check. Automatically install the necessary powershell module and check for pending
    /// Windows updates.
    /// </summary>
    /// <param name="callback">The logger function with the expected parameters types of (string, Enums.LogLevel bool)</param>
    public static void Update(CallbackDelegate? callback)
    {
        _logCallback = callback;
        
        bool success = PerformUpdate();
        if (success)
        {
            TriggerLogCallback("Updates complete, restarting computer", Enums.LogLevel.Info);
            //RestartComputer();
        }
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
    /// Run through the update process, checking if the program has admin privileges before checking for powershell
    /// modules and windows updates.
    /// </summary>
    /// <returns>A bool representing if the update process was completed successfully or if updates are available.</returns>
    private static bool PerformUpdate()
    {
        //Check if the program has Admin rights otherwise bail out early
        if (!IsAdmin())
        {
            TriggerLogCallback("Station is not running with admin privileges", Enums.LogLevel.Info);
            return false;
        }
        
        //Bail out if any of the functions produce powershell error messages (lodged with Sentry)
        //Check if the module is installed
        if (!CheckForPowershellModule())
        {
            TriggerLogCallback("PSWindowsUpdate module is not installed", Enums.LogLevel.Info);
            
            //Allow the PSGallery as a trusted powershell repository
            if (!AddPsGalleryRepository()) return false;
            TriggerLogCallback("PSGallery added trusted repositories", Enums.LogLevel.Info);
            
            //Attempt to install the module if it is not installed
            if (!InstallPowershellModule()) return false;
            TriggerLogCallback("PSWindowsUpdate powershell module installed successfully", Enums.LogLevel.Info);
        }

        //Check if there are any Windows updates available
        TriggerLogCallback("Checking for Windows updates...", Enums.LogLevel.Info);
        
        return CheckForWindowsUpdates();
    }
    
    /// <summary>
    /// Check if the current program has administrator privileges.
    /// </summary>
    /// <returns>A bool representing if the program is running with admin privileges</returns>
    private static bool IsAdmin()
    {
        WindowsIdentity identity = WindowsIdentity.GetCurrent();
        WindowsPrincipal principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    /// <summary>
    /// Check if the PSWindowsUpdate module is installed on the local machine.
    /// </summary>
    /// <returns>A bool of if the module is installed.</returns>
    private static bool CheckForPowershellModule()
    {
        string command = $"Get-Module -Name {ModuleName} -ListAvailable";
        string output = ExecutePowerShellCommand(command);
        return output.Contains(ModuleName); // Check if the output contains information about the module
    }
    
    /// <summary>
    /// Add the PSGallery repository to the trusted Powershell repositories for the local machine. NOTE: this does not
    /// require Admin privileges.
    /// </summary>
    /// <returns>A bool of if the command was successful.</returns>
    private static bool AddPsGalleryRepository()
    {
        TriggerLogCallback("Adding PSGallery to trusted repositories", Enums.LogLevel.Info);
        string success = ExecutePowerShellCommand($"Set-PSRepository -Name {PsGalleryRepo} -InstallationPolicy Trusted -Verbose");
        return success is not "failed";
    }
    
    /// <summary>
    /// Attempt to install the PSWindowsUpdate module onto the local machine. NOTE: this requires Admin privileges.
    /// </summary>
    /// <returns>A bool of if the command was successful.</returns>
    private static bool InstallPowershellModule()
    {
        TriggerLogCallback("Attempting to install PSWindowsUpdate powershell module", Enums.LogLevel.Info);
        string command = $"Install-Module -Name {ModuleName} -RequiredVersion {ModuleVersion} -Force -Verbose";
        string success = ExecutePowerShellCommand(command);
        return success is not "failed";
    }

    /// <summary>
    /// Using the PSWindowsUpdate module, check if there are any Windows updates that are pending.
    /// </summary>
    /// <returns>A bool if there are Windows updates available.</returns>
    private static bool CheckForWindowsUpdates()
    {
        string command = "Get-WindowsUpdate";
        string success = ExecutePowerShellCommand(command);

        if (success is "failed")
        {
            TriggerLogCallback("CheckForWindowsUpdates - Error whilst checking for Windows updates", Enums.LogLevel.Error);
            return false;
        }

        if (string.IsNullOrEmpty(success))
        {
            TriggerLogCallback("PerformAllWindowsUpdates - No updates found", Enums.LogLevel.Error);
            return false;
        }
        
        // Perform updates if there are any pending
        if (PerformAllWindowsUpdates()) return true;
        
        TriggerLogCallback("PerformAllWindowsUpdates - Error whilst performing Windows updates", Enums.LogLevel.Error);
        return false;
    }
    
    /// <summary>
    /// Attempt to perform all the pending windows updates
    /// </summary>
    /// <returns></returns>
    private static bool PerformAllWindowsUpdates()
    {
        TriggerLogCallback("Attempting to perform all Windows updates", Enums.LogLevel.Info);
        string command = $"Install-WindowsUpdate -AcceptAll";
        string success = ExecutePowerShellCommand(command);
        return success is not "failed";
    }

    /// <summary>
    /// Restart the computer.
    /// </summary>
    private static void RestartComputer()
    {
        string command = $"Restart-Computer";
        ExecutePowerShellCommand(command);
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="command"></param>
    /// <returns></returns>
    private static string ExecutePowerShellCommand(string command)
    {
        // Create a ProcessStartInfo object
        ProcessStartInfo psi = new ProcessStartInfo
        {
            // Specify the path for PowerShell
            FileName = "powershell.exe",
            // Add necessary arguments to the PowerShell command
            Arguments = $"-NoProfile -Command \"{command}\"",
            // Redirect the standard output & error so we can read it
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            // Specify that the process should run as administrator
            Verb = "runas",
            // Hide the command window
            CreateNoWindow = true
        };

        // Start the PowerShell process
        Process? process = Process.Start(psi);

        // Unable to start process
        if (process == null) return "failed";
        
        // Read the output of the PowerShell command asynchronously
        StringBuilder output = new StringBuilder();
        process.OutputDataReceived += (_, e) =>
        {
            if (string.IsNullOrEmpty(e.Data)) return;
            output.AppendLine(e.Data);
            
            // Check if the output contains a prompt asking for confirmation [Y / N]
            // This should only be present when asked to restart the computer after updates are installed
            if (e.Data.Contains("[Y / N]"))
            {
                process.StandardInput.WriteLine("N"); // Input N to the process
                process.StandardInput.WriteLine(); // Input Enter to the process
            }
                
            // Output the command execution result
            TriggerLogCallback(e.Data, Enums.LogLevel.Info);
        };
        process.BeginOutputReadLine();
        
        string error = process.StandardError.ReadToEnd();

        // Wait for the process to exit
        process.WaitForExit();
        
        // Check if there were any errors
        if (string.IsNullOrWhiteSpace(error)) return output.ToString();
        
        TriggerLogCallback(error, Enums.LogLevel.Error);
        return "failed"; // Command failed
    }
}