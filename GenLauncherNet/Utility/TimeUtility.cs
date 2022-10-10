using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace GenLauncherNet.Utility
{
    /// <summary>
    ///     Utility class providing generally all around useful functions regarding date and time.
    /// </summary>
    internal static class TimeUtility
    {
        /// <summary>
        ///     Sets the system clock to a specified date and time.
        /// </summary>
        /// <param name="systemTime"></param>
        /// <returns></returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetSystemTime(ref SystemTime systemTime);

        /// <summary>
        ///     Syncs the system clock to the default windows time server.
        /// </summary>
        internal static void SyncSystemDateTimeWithWorldTime()
        {
            var dateTime = GetNetworkTime();
            var offset = DateTimeOffset.Now;
            var dateTimeWithOffset = dateTime.Subtract(offset.Offset);

            var systemTime = new SystemTime
            {
                year = Convert.ToInt16(dateTimeWithOffset.Year),
                month = Convert.ToInt16(dateTimeWithOffset.Month),
                day = Convert.ToInt16(dateTimeWithOffset.Day),
                hour = Convert.ToInt16(dateTimeWithOffset.Hour),
                minute = Convert.ToInt16(dateTimeWithOffset.Minute),
                second = Convert.ToInt16(dateTimeWithOffset.Second)
            };
            SetSystemTime(ref systemTime);
        }

        /// <summary>
        ///     Retrieves the time from the default windows time server.
        /// </summary>
        /// <returns>The time from the default windows time server in locale format.</returns>
        private static DateTime GetNetworkTime()
        {
            // Default Windows time server
            const string ntpServer = "time.windows.com";
            // NTP message size - 16 bytes of the digest (RFC 2030)
            var ntpData = new byte[48];
            // Setting the Leap Indicator, Version Number and Mode values
            ntpData[0] = 0x1B; // LI = 0 (no warning), VN = 3 (IPv4 only), Mode = 3 (Client Mode)

            var addresses = Dns.GetHostEntry(ntpServer).AddressList;

            // The UDP port number assigned to NTP is 123
            var ipEndPoint = new IPEndPoint(addresses[0], 123);
            // NTP uses UDP

            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                socket.Connect(ipEndPoint);

                // Stops code hang if NTP is blocked
                socket.ReceiveTimeout = 3000;

                socket.Send(ntpData);
                socket.Receive(ntpData);
                socket.Close();
            }

            /* Offset to get to the "Transmit Timestamp" field (time at which the reply
             departed the server for the client, in 64-bit timestamp format)."*/
            const byte serverReplyTime = 40;
            // Get the seconds part
            ulong intPart = BitConverter.ToUInt32(ntpData, serverReplyTime);
            // Get the seconds fraction
            ulong fractionPart = BitConverter.ToUInt32(ntpData, serverReplyTime + 4);
            // Convert from big-endian to little-endian
            intPart = SwapEndianness(intPart);
            fractionPart = SwapEndianness(fractionPart);

            var milliseconds = intPart * 1000 + fractionPart * 1000 / 0x100000000L;

            // **UTC** time
            var networkDateTime =
                new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds((long)milliseconds);

            return networkDateTime.ToLocalTime();
        }

        private static uint SwapEndianness(ulong x)
        {
            return (uint)(((x & 0x000000ff) << 24) +
                          ((x & 0x0000ff00) << 8) +
                          ((x & 0x00ff0000) >> 8) +
                          ((x & 0xff000000) >> 24));
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SystemTime
        {
            internal short year;
            internal short month;
            internal short day;
            internal short hour;
            internal short minute;
            internal short second;
        }
    }
}