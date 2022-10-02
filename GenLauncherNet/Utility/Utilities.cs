using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Windows;
using Microsoft.Win32;

namespace GenLauncherNet.Utility
{
    internal static class Utilities
    {
        /// <summary>
        ///     Checks the registry if there is a installed .NET Framework version matching or exceeding a specified version.
        /// </summary>
        /// <param name="requiredVersionReleaseKey">Numerical release key of the .NET Framework version to check for.</param>
        /// <returns></returns>
        internal static bool IsRequiredNetFrameworkVersionInstalled(uint requiredVersionReleaseKey)
        {
            const string registrySubKey = @"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\";

            using (var ndpKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32)
                       .OpenSubKey(registrySubKey))
            {
                if (ndpKey?.GetValue("Release") != null)
                    // Does it match or exceed the required version?
                    return (int)ndpKey.GetValue("Release") >=
                           requiredVersionReleaseKey;
            }

            return false; // Not installed
        }

        /// <summary>
        ///     Opens a specific webpage in the user's default browser based on a given URL.
        /// </summary>
        /// <param name="webpageUrl">URL of the webpage to open.</param>
        internal static void OpenWebpageInBrowser(string webpageUrl)
        {
            try
            {
                Process.Start(webpageUrl);
            }
            catch (Win32Exception noBrowser)
            {
                if (noBrowser.ErrorCode == -2147467259)
                    MessageBox.Show(noBrowser.Message);
            }
            catch (Exception other)
            {
                MessageBox.Show(other.Message);
            }
        }

        internal static void SyncSystemDateTimeWithWorldTime()
        {
            var dt = GetNetworkTime();

            var offset = DateTimeOffset.Now;
            var dtWithOffset = dt.Subtract(offset.Offset);

            SYSTEMTIME st = new SYSTEMTIME();
            st.wYear = Convert.ToInt16(dtWithOffset.Year);
            st.wMonth = Convert.ToInt16(dtWithOffset.Month);
            st.wDay = Convert.ToInt16(dtWithOffset.Day);
            st.wHour = Convert.ToInt16(dtWithOffset.Hour);
            st.wMinute = Convert.ToInt16(dtWithOffset.Minute);
            st.wSecond = Convert.ToInt16(dtWithOffset.Second);

            SetSystemTime(ref st);
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetSystemTime(ref SYSTEMTIME st);

        [StructLayout(LayoutKind.Sequential)]
        public struct SYSTEMTIME
        {
            public short wYear;
            public short wMonth;
            public short wDayOfWeek;
            public short wDay;
            public short wHour;
            public short wMinute;
            public short wSecond;
            public short wMilliseconds;
        }

        private static DateTime GetNetworkTime()
        {
            //default Windows time server
            const string ntpServer = "time.windows.com";

            // NTP message size - 16 bytes of the digest (RFC 2030)
            var ntpData = new byte[48];

            //Setting the Leap Indicator, Version Number and Mode values
            ntpData[0] = 0x1B; //LI = 0 (no warning), VN = 3 (IPv4 only), Mode = 3 (Client Mode)

            var addresses = Dns.GetHostEntry(ntpServer).AddressList;

            //The UDP port number assigned to NTP is 123
            var ipEndPoint = new IPEndPoint(addresses[0], 123);
            //NTP uses UDP

            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                socket.Connect(ipEndPoint);

                //Stops code hang if NTP is blocked
                socket.ReceiveTimeout = 3000;

                socket.Send(ntpData);
                socket.Receive(ntpData);
                socket.Close();
            }

            //Offset to get to the "Transmit Timestamp" field (time at which the reply 
            //departed the server for the client, in 64-bit timestamp format."
            const byte serverReplyTime = 40;

            //Get the seconds part
            ulong intPart = BitConverter.ToUInt32(ntpData, serverReplyTime);

            //Get the seconds fraction
            ulong fractPart = BitConverter.ToUInt32(ntpData, serverReplyTime + 4);

            //Convert From big-endian to little-endian
            intPart = SwapEndianness(intPart);
            fractPart = SwapEndianness(fractPart);

            var milliseconds = (intPart * 1000) + ((fractPart * 1000) / 0x100000000L);

            //**UTC** time
            var networkDateTime = (new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc)).AddMilliseconds((long)milliseconds);

            return networkDateTime.ToLocalTime();
        }

        static uint SwapEndianness(ulong x)
        {
            return (uint)(((x & 0x000000ff) << 24) +
                           ((x & 0x0000ff00) << 8) +
                           ((x & 0x00ff0000) >> 8) +
                           ((x & 0xff000000) >> 24));
        }
    }
}