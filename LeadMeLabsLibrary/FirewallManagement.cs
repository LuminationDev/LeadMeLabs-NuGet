﻿using NetFwTypeLib;
using System;
using System.Diagnostics;

namespace LeadMeLabsLibrary
{
    public static class FirewallManagement
    {
        /// <summary>
        /// Checks if the program at the current executable path is allowed through the firewall.
        /// </summary>
        /// <returns>
        /// Returns "Allowed" if the program is allowed through the firewall,
        /// otherwise returns "Not allowed".
        /// </returns>
        public static string IsProgramAllowedThroughFirewall()
        {
            string? programPath = GetExecutablePath();

            INetFwPolicy2? firewallPolicy = GetFirewallPolicy();

            if(firewallPolicy == null)
            {
                return "Unknown";
            }

            NET_FW_ACTION_ action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW;

            foreach (INetFwRule rule in firewallPolicy.Rules)
            {
                if (rule.ApplicationName != null)
                {
                    if (rule.Action == action && rule.ApplicationName.Equals(programPath, StringComparison.OrdinalIgnoreCase))
                    {
                        return "Allowed";
                    }
                }
            }

            return "Not allowed";
        }

        /// <summary>
        /// Retrieves the firewall policy using the HNetCfg.FwPolicy2 COM object.
        /// </summary>
        /// <returns>The INetFwPolicy2 object representing the firewall policy.</returns>
        private static INetFwPolicy2? GetFirewallPolicy()
        {
            Type? type = Type.GetTypeFromProgID("HNetCfg.FwPolicy2");
            if(type == null) return null;
            return Activator.CreateInstance(type) as INetFwPolicy2;
        }

        /// <summary>
        /// Retrieves the path of the current executable.
        /// </summary>
        /// <returns>The path of the current executable, or null if it cannot be determined.</returns>
        private static string? GetExecutablePath()
        {
            string? executablePath = null;

            try
            {
                Process currentProcess = Process.GetCurrentProcess();
                if (currentProcess != null)
                {
                    executablePath = currentProcess.MainModule.FileName;
                }
            }
            catch
            {
                // Handle any exceptions that occur during path retrieval
            }

            return executablePath;
        }
    }
}
