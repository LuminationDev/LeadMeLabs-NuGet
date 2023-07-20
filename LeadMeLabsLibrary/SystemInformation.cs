using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace LeadMeLabsLibrary
{
    public static class SystemInformation
    {
        /// <summary>
        /// Collect just the IPv4 address, filter out any virtual boxes that may be on the system.
        /// </summary>
        /// <returns>An IPAddress object of the local IP Address</returns>
        public static IPAddress? GetIPAddress()
        {
            try
            {
                IPHostEntry hostEntry = Dns.GetHostEntry(Dns.GetHostName());
                IPAddress? ipAddress = hostEntry.AddressList.FirstOrDefault(ip =>
                    ip.AddressFamily == AddressFamily.InterNetwork &&
                    !IsVirtualBoxInterface(ip));
                return ipAddress;
            }
            catch (Exception ex)
            {
                throw new Exception("Manager class: Server IP Address could not be found", ex);
            }
        }

        /// <summary>
        /// Scan the network interfaces and check if the supplied IPAddress is connected to a virtual
        /// box.
        /// </summary>
        /// <param name="ipAddress">An IPAddress</param>
        /// <returns>A boolean representing if the IPAddress is owned by a virtual box</returns>
        public static bool IsVirtualBoxInterface(IPAddress ipAddress)
        {
            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface adapter in interfaces)
            {
                IPInterfaceProperties ipProperties = adapter.GetIPProperties();
                foreach (UnicastIPAddressInformation addrInfo in ipProperties.UnicastAddresses)
                {
                    if (addrInfo.Address.Equals(ipAddress))
                    {
                        if (adapter.Description.ToLower().Contains("virtual"))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
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
