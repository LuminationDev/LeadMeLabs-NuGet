using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Sockets;

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
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_NetworkAdapterConfiguration WHERE IPEnabled = true AND NOT Description LIKE '%VirtualBox%'");
            IEnumerable<ManagementObject> objects = searcher.Get().Cast<ManagementObject>();
            string mac = (from o in objects orderby o["IPConnectionMetric"] select o["MACAddress"].ToString()).FirstOrDefault()?.Replace(":", "-") ?? "Unknown";
            return mac;
        }
    }
}
