using NetFwTypeLib;
using System;
using System.Diagnostics;
using System.Security.Principal;

namespace LeadMeLabsLibrary
{
    public static class FirewallManagement
    {
        /// <summary>
        /// Checks if the current user is running with administrative privileges.
        /// </summary>
        /// <returns>True if the user is an administrator; otherwise, false.</returns>
        private static bool IsUserAdmin()
        {
            WindowsIdentity currentIdentity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(currentIdentity);

            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

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

        /// <summary>
        /// Check if the basic Steam.exe (SteamBlocker) is enabled, if it is treat the rest like they are enabled.
        /// </summary>
        /// <returns>A boolean of if the rule is enabled.</returns>
        public static string InitialCheck()
        {
            string ruleName = "SteamBlocker";
            if (!IsUserAdmin())
            {
                return $"Could not check rule: '{ruleName}'. Software requires Administator permissions.";
            }

            INetFwPolicy2? firewallPolicy = GetFirewallPolicy();
            if (firewallPolicy == null)
            {
                return $"Could not check rule: '{ruleName}'. Software requires Administator permissions.";
            }
            INetFwRule? existingRule = GetRuleByName(ruleName, firewallPolicy);

            if (existingRule == null)
            {
                return "false";
            }

            return existingRule.Enabled.ToString();
        }

        /// <summary>
        /// Creates a firewall rule with the specified name and executable path. If the rule already exists, it is disabled or enabled based on its current state.
        /// </summary>
        /// <param name="ruleName">The name of the rule.</param>
        /// <param name="exePath">The executable path associated with the rule.</param>
        /// <param name="enableRule">Enable or disable the rule.</param>
        public static string ToggleRule(string ruleName, string exePath, bool enableRule)
        {
            if (!IsUserAdmin())
            {
                return $"Could not check rule: '{ruleName}'. Please run software as Administator.";
            }

            // Create an instance of the Windows Firewall Manager
            Type? type = Type.GetTypeFromProgID("HNetCfg.FwMgr");

            if (type == null)
            {
                return $"Could not create rule: '{ruleName}'. Could not find 'HNetCfg.FwMgr'.";
            }

            object? obj = Activator.CreateInstance(type);
            if (obj == null)
            {
                return $"Could not create rule: '{ruleName}'. Unable to create firewall instance. Software requires Administator permissions.";
            }

            INetFwPolicy2? firewallPolicy = GetFirewallPolicy();
            if (firewallPolicy == null)
            {
                return $"Could not create rule: '{ruleName}'. Software requires Administator permissions.";
            }

            // Check if the rule already exists
            bool ruleExists = RuleExists(ruleName, firewallPolicy);

            if (ruleExists)
            {
                INetFwRule? existingRule = GetRuleByName(ruleName, firewallPolicy);

                if (existingRule == null)
                {
                    return $"Could not find local rule '{ruleName}'. Admin account might block local account.";
                }

                if (enableRule)
                {
                    EnableRule(existingRule);
                    return "true";
                }
                else
                {
                    DisableRule(existingRule);
                    return "true";
                }
            }
            else
            {
                CreateOutboundRule(ruleName, exePath, firewallPolicy);
                return "created";
            }
        }

        /// <summary>
        /// Checks if a firewall rule with the specified name exists.
        /// </summary>
        /// <param name="ruleName">The name of the rule to check.</param>
        /// <param name="firewallPolicy">The INetFwPolicy2 instance representing the firewall policy.</param>
        /// <returns>True if the rule exists, false otherwise.</returns>
        private static bool RuleExists(string ruleName, INetFwPolicy2 firewallPolicy)
        {
            foreach (INetFwRule rule in firewallPolicy.Rules)
            {
                if (rule.Name.Equals(ruleName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Retrieves a firewall rule by its name from the specified firewall policy.
        /// </summary>
        /// <param name="ruleName">The name of the rule to retrieve.</param>
        /// <param name="firewallPolicy">The INetFwPolicy2 instance representing the firewall policy.</param>
        /// <returns>The INetFwRule instance representing the firewall rule, or null if not found.</returns>
        private static INetFwRule? GetRuleByName(string ruleName, INetFwPolicy2 firewallPolicy)
        {
            foreach (INetFwRule rule in firewallPolicy.Rules)
            {
                if (rule.Name.Equals(ruleName, StringComparison.OrdinalIgnoreCase))
                {
                    return rule;
                }
            }
            return null;
        }

        /// <summary>
        /// Enables a firewall rule.
        /// </summary>
        /// <param name="rule">The INetFwRule instance representing the firewall rule to enable.</param>
        private static void EnableRule(INetFwRule rule)
        {
            rule.Enabled = true;
        }

        /// <summary>
        /// Disables a firewall rule.
        /// </summary>
        /// <param name="rule">The INetFwRule instance representing the firewall rule to disable.</param>
        private static void DisableRule(INetFwRule rule)
        {
            rule.Enabled = false;
        }

        /// <summary>
        /// Creates a new outbound firewall rule to block the specified application path.
        /// </summary>
        /// <param name="ruleName">The name of the rule.</param>
        /// <param name="applicationPath">The application path to block.</param>
        /// <param name="firewallPolicy">The INetFwPolicy2 instance representing the firewall policy.</param>
        private static void CreateOutboundRule(string ruleName, string applicationPath, INetFwPolicy2 firewallPolicy)
        {
            Type ruleType = Type.GetTypeFromProgID("HNetCfg.FwRule");
            INetFwRule outboundRule = (INetFwRule)Activator.CreateInstance(ruleType);

            outboundRule.Name = ruleName;
            outboundRule.ApplicationName = applicationPath;
            outboundRule.Action = NET_FW_ACTION_.NET_FW_ACTION_BLOCK;
            outboundRule.Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_OUT;

            firewallPolicy.Rules.Add(outboundRule);
        }
    }
}
