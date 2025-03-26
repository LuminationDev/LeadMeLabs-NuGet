using System;
using System.IO;
using Microsoft.Win32;
using Microsoft.Win32.TaskScheduler;

namespace LeadMeLabsLibrary;

public static class RegistryHelper
{
    /// <summary>
    /// Validates the Electron launcher's installation directory registry key by checking for the presence of an
    /// old task and, if not found or disabled, checks for the new task. The task's action path is used as the location
    /// for the registry key entry.
    /// </summary>
    /// <param name="softwareType">The software type ("Station" or "NUC").</param>
    /// <returns>
    /// A tuple containing a boolean indicating whether either the old or new task is found, enabled, and the registry key is adapted.
    /// The second element of the tuple is a string message providing additional information.
    /// </returns>
    public static Tuple<bool, string> ValidateElectronInstallDirectory(string softwareType)
    {
        // Check for the old task before the new one
        Tuple<bool, string> result = CheckForOldTask(softwareType);

        // If the old task is not there or disabled check for the new task
        if (!result.Item1)
        {
            result = CheckForNewTask();
        }

        return result;
    }
    
    /// <summary>
    /// Checks for the existence and enabled status of an old scheduled task based on the specified software type.
    /// Returns true if the old task is found, enabled, and associated actions are successfully collected; otherwise, false.
    /// </summary>
    /// <param name="softwareType">The software type ("Station" or "NUC").</param>
    /// <returns>
    /// A tuple containing a boolean indicating whether the old task is found, enabled, and actions are successfully collected.
    /// The second element of the tuple is a string message providing additional information.
    /// </returns>
    private static Tuple<bool, string> CheckForOldTask(string softwareType)
    {
        string taskPath = softwareType == "Station" ? @"\Station\Station_Checker" : @"\NUC\NUC_Checker";
        return CheckForTask(taskPath);
    }

    /// <summary>
    /// Checks for the existence and enabled status of a new scheduled task.
    /// Returns true if the new task is found, enabled, and associated actions are successfully collected; otherwise, false.
    /// </summary>
    /// <returns>
    /// A tuple containing a boolean indicating whether the new task is found, enabled, and actions are successfully collected.
    /// The second element of the tuple is a string message providing additional information.
    /// </returns>
    private static Tuple<bool, string> CheckForNewTask()
    {
        const string taskPath = @"\LeadMe\Software_Checker";
        return CheckForTask(taskPath);
    }
    
    /// <summary>
    /// Checks for the existence and enabled status of a scheduled task based on the specified task path.
    /// Returns true if the task is found, enabled, and associated actions are successfully collected; otherwise, false.
    /// </summary>
    /// <param name="taskPath">The path of the scheduled task.</param>
    /// <returns>
    /// A tuple containing a boolean indicating whether the task is found, enabled, and actions are successfully collected.
    /// The second element of the tuple is a string message providing additional information.
    /// </returns>
    private static Tuple<bool, string> CheckForTask(string taskPath)
    {
        string? actionPath = SearchTaskScheduler(taskPath);
        if (actionPath == null) return Tuple.Create(false, $"Error: Task is disabled or could not find task's action path. {taskPath}");

        actionPath = CollectBatchParent(actionPath);
        if (actionPath == null) return Tuple.Create(false, $"Error: Could not find _batch parent folder. {actionPath}");

        return SearchRegistryKey(actionPath);
    }

    /// <summary>
    /// Given a path, extracts the parent directory path by removing the last segment of the path.
    /// </summary>
    /// <param name="path">The path from which to extract the parent directory path.</param>
    /// <returns>
    /// The parent directory path, or null if an error occurs. The second element of the tuple is a string message providing additional information.
    /// </returns>
    private static string? CollectBatchParent(string path)
    {
        string targetDirectoryName = "_batch";

        // Get the directory path
        string? directoryPath = Path.GetDirectoryName(path);

        if (directoryPath == null) return null;

        // Find the index of the target directory in the path
        int index = directoryPath.LastIndexOf(targetDirectoryName, StringComparison.OrdinalIgnoreCase);

        if (index == -1) return null;
        
        // Extract the parent directory path
        string parentDirectoryPath = directoryPath.Substring(0, index - 1);

        // Check that the parent directory exists and that the LeadMe.exe exists within it.
        if (!Directory.Exists(parentDirectoryPath)) return null;
        if (!File.Exists(Path.Join(parentDirectoryPath, "LeadMe.exe"))) return null;
        
        return parentDirectoryPath;
    }
    
    /// <summary>
    /// Searches the Task Scheduler for the specified task path and returns the path of the associated action (executable).
    /// Returns null if the task is not found, disabled, or no action is associated.
    /// </summary>
    /// <param name="taskPath">The path of the scheduled task.</param>
    /// <returns>
    /// The path of the associated action (executable), or null if the task is not found, disabled, or no action is associated.
    /// The second element of the tuple is a string message providing additional information.
    /// </returns>
    private static string? SearchTaskScheduler(string taskPath)
    {
        // Create a new TaskService instance
        using TaskService taskService = new TaskService();
        
        // Get the scheduled task
        Task task = taskService.GetTask(taskPath);

        // Bail out early if no task is found
        if (task == null) return null;

        // Bail out if task is disabled
        if (!task.Enabled) return null;
        
        // Collect information about the actions
        TaskDefinition taskDefinition = task.Definition;

        string actionPath = "";

        foreach (var action in taskDefinition.Actions)
        {
            if (action is not ExecAction execAction) continue;
            actionPath = execAction.Path;
        }

        return actionPath;
    }
    
    /// <summary>
    /// Searches for a specific entry in the Windows Registry under the SOFTWARE key,
    /// based on the provided expected installation path and target shortcut name.
    /// If a matching entry is found, checks and updates the InstallLocation field.
    /// </summary>
    /// <param name="expectedInstallPath">The expected installation path to match in the registry.</param>
    /// <returns>
    /// A tuple containing a boolean indicating whether a matching entry is found in the registry,
    /// and the InstallLocation field is updated or already correct; otherwise, false.
    /// The second element of the tuple is a string message providing additional information.
    /// </returns>
    private static Tuple<bool, string> SearchRegistryKey(string expectedInstallPath)
    {
        // Specify the base registry key (LocalMachine in this case)
        RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
        
        // Specify the path to the registry key you want to search
        string registryPath = @"SOFTWARE";

        // Specify the value name you want to search for
        string targetShortcutName = "LeadMe Launcher";

        // Specify the sub key field to change
        string fieldToUpdate = "InstallLocation";
        
        // Specify the location of the Launcher
        string launcherInstallPath = expectedInstallPath;
        
        try
        {
            // Open the registry key under SOFTWARE
            using RegistryKey? key = baseKey.OpenSubKey(registryPath, true);
            
            if (key != null)
            {
                // Get the names of all sub keys (entries) under the SOFTWARE key
                string[] subKeyNames = key.GetSubKeyNames();

                // Iterate through each sub key and check for the specified ShortcutName
                foreach (string subKeyName in subKeyNames)
                {
                    using RegistryKey? subKey = key.OpenSubKey(subKeyName, true);

                    if (subKey == null) continue;
                    
                    // Check if the sub key contains the specified ShortcutName
                    object? shortcutNameValue = subKey.GetValue("ShortcutName");
                    
                    // Check the current value of the install location
                    object? installLocationValue = subKey.GetValue("InstallLocation");

                    // Check if the ShortcutName value matches the target value
                    if (shortcutNameValue != null && string.Equals(shortcutNameValue.ToString(), targetShortcutName, StringComparison.OrdinalIgnoreCase))
                    {
                        if (installLocationValue != null && string.Equals(installLocationValue.ToString(),
                                launcherInstallPath, StringComparison.OrdinalIgnoreCase))
                        {
                            return Tuple.Create(true, "Install location already correct.");
                        }
                        
                        try
                        {
                            // Update another field in the registry entry
                            subKey.SetValue(fieldToUpdate, launcherInstallPath);
                        }
                        catch (Exception e)
                        {
                            return Tuple.Create(false, $"Error: Could not update value: {e}");
                        }

                        // Exit the loop if a match is found
                        return Tuple.Create(true, $"Updated {fieldToUpdate} in registry: {registryPath}\\{subKeyName}"); 
                    }
                }

                return Tuple.Create(false, $"Error: No matching entry found with ShortcutName = {targetShortcutName}");
            }

            return Tuple.Create(false, $"Error: Registry key not found: {registryPath}");
        }
        catch (Exception ex)
        {
            return Tuple.Create(false, $"An error occurred: {ex.Message}");
        }
    }

    /// <summary>
    /// This method searches for a specified registry value under the SOFTWARE key in the CurrentUser registry hive.
    /// It looks for a sub-key with a specific "ShortcutName" value and returns the corresponding "InstallLocation" value.
    /// </summary>
    /// <param name="targetShortcutName">A string of the shortcut name of the program to be searched for.</param>
    /// <returns>A string of the install location</returns>
    public static string? GetProgramInstallationPath(string targetShortcutName)
    {
         // Specify the base registry key (LocalMachine in this case)
        RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
        
        // Specify the path to the registry key you want to search
        string registryPath = @"SOFTWARE";
        
        try
        {
            // Open the registry key under SOFTWARE
            using RegistryKey? key = baseKey.OpenSubKey(registryPath, true);
            
            if (key != null)
            {
                // Get the names of all sub keys (entries) under the SOFTWARE key
                string[] subKeyNames = key.GetSubKeyNames();

                // Iterate through each sub key and check for the specified ShortcutName
                foreach (string subKeyName in subKeyNames)
                {
                    using RegistryKey? subKey = key.OpenSubKey(subKeyName, true);

                    if (subKey == null) continue;
                    
                    // Check if the sub key contains the specified ShortcutName
                    object? shortcutNameValue = subKey.GetValue("ShortcutName");
                    
                    // Check the current value of the install location
                    object? installLocationValue = subKey.GetValue("InstallLocation");

                    // Check if the ShortcutName value matches the target value
                    if (shortcutNameValue != null && string.Equals(shortcutNameValue.ToString(), targetShortcutName, StringComparison.OrdinalIgnoreCase))
                    {
                        return installLocationValue?.ToString();
                    }
                }

                return null;
            }

            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
