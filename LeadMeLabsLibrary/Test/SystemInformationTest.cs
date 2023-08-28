using System;
using System.Net;
using LeadMeLabsLibrary;
using Xunit;

namespace Test
{
    public class SystemInformationTest
    {
        /// <summary>
        /// Checks that GetIPAddress generates a valid IP address
        /// </summary>
        [Fact]
        public void GetIPAddress_Should_Return_System_IP_Address()
        {
            // Arrange
            // Act
            IPAddress? address = SystemInformation.GetIPAddress();

            // Assert
            Assert.NotNull(address);
            Assert.Matches("^(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$", address.MapToIPv4().ToString());
        }
        
        /// <summary>
        /// Checks that generate ARP table returns an arp table
        /// </summary>
        [Fact]
        public void GenerateARPTable_Should_Return_ARP_Table()
        {
            // Arrange
            // Act
            string[]? arpTable = SystemInformation.GenerateARPTable();

            // Assert
            Assert.NotNull(arpTable);
            Assert.True(arpTable.Length > 8);
            Assert.Equal("  Internet Address      Physical Address      Type", arpTable[6]);
        }
    }
}

