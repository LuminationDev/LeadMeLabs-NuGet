using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using SysVersion = System.Version;

namespace LeadMeLabsLibrary
{
    public static class SystemInformation
    {
        /// <summary>
        /// Collect just the IP address.
        /// </summary>
        /// <returns>An IPAddress object of the local IP Address</returns>
        public static IPAddress? GetIPAddress()
        {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("8.8.8.8", 65530);
                IPEndPoint? endPoint = socket.LocalEndPoint as IPEndPoint;

                if (endPoint is not null)
                {
                    return endPoint.Address;
                }
                else
                {
                    throw new Exception("Manager class: Server IP Address could not be found");
                }
            }
        }

        /// <summary>
        /// Retrieve the MAC address of the current machine.
        /// </summary>
        /// <returns>A string of the mac address</returns>
        public static string? GetMACAddress()
        {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_NetworkAdapterConfiguration where IPEnabled=true");
            IEnumerable<ManagementObject> objects = searcher.Get().Cast<ManagementObject>();
            string? mac = (from o in objects orderby o["IPConnectionMetric"] select o["MACAddress"].ToString()).FirstOrDefault();
            return mac;
        }

        /// <summary>
        /// Query the program to get the current version number of the software that is running.
        /// </summary>
        /// <returns>A string of the version number in the format X.X.X.X</returns>
        public static string? GetVersionNumber()
        {
            Assembly? assembly = Assembly.GetExecutingAssembly();
            if (assembly == null) return "N/A";

            SysVersion? version = assembly.GetName().Version;
            if (version == null) return "N/A";

            // Format the version number as Major.Minor.Build
            return $"{version.Major}.{version.Minor}.{version.Build}";
        }

        /// <summary>
        /// Print out the currently running software version to a text file at 'programLocation\_logs\version.txt'. 
        /// If the version number or program directory cannot be found the function returns false, bailing out before
        /// writing the version. 
        /// </summary>
        public static bool GenerateVersion()
        {
            string? programLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (programLocation == null)
            {
                return false;
            }

            Assembly? assembly = Assembly.GetExecutingAssembly();
            if (assembly == null)
            {
                WriteFile(programLocation, "0.0.0");
                return false;
            }

            SysVersion? version = assembly.GetName().Version;
            if (version == null)
            {
                WriteFile(programLocation, "0.0.0");
                return false;
            }

            string? formattedVersion = $"{version.Major}.{version.Minor}.{version.Build}";
            WriteFile(programLocation, formattedVersion);

            return version != null;
        }

        private static void WriteFile(string location, string version)
        {
            File.WriteAllText($"{location}\\_logs\\version.txt", version);
        }
    }
}
