using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BarcodeScanner.Helpers
{
    /// <summary>
    /// Already implemented. You do not need to change anything in this file.
    /// The Build* methods are here so you can compose your own test streams
    /// without hand-writing bytes.
    /// </summary>
    public static class HelperFunctions
    {
        public const byte STX = 0x02;
        public const byte ETX = 0x03;
        public const int MaxPayloadLength = 64;

        public static (string com, int baudrate) GetComAndBaudRate()
        {
            return ("COM1", 9600);
        }

        /// <summary>XOR of every payload byte.</summary>
        public static byte Checksum(string payload)
        {
            byte checksum = 0;
            foreach (byte b in Encoding.ASCII.GetBytes(payload))
            {
                checksum ^= b;
            }
            return checksum;
        }

        /// <summary>A well formed frame: STX payload ETX HH</summary>
        public static byte[] BuildFrame(string payload)
        {
            return BuildFrame(payload, Checksum(payload));
        }

        /// <summary>A complete frame whose checksum digits do not match the payload.</summary>
        public static byte[] BuildFrameWithBadChecksum(string payload)
        {
            return BuildFrame(payload, (byte)(Checksum(payload) ^ 0xFF));
        }

        /// <summary>A frame that starts but never ends: STX payload, no ETX and no checksum.</summary>
        public static byte[] BuildTruncatedFrame(string payload)
        {
            var frame = new List<byte> { STX };
            frame.AddRange(Encoding.ASCII.GetBytes(payload));
            return frame.ToArray();
        }

        /// <summary>Random printable ASCII. Never contains STX or ETX.</summary>
        public static byte[] Garbage(int count, int seed)
        {
            var rng = new Random(seed);
            var bytes = new byte[count];
            for (int i = 0; i < count; i++)
            {
                bytes[i] = (byte)rng.Next(0x20, 0x7F);
            }
            return bytes;
        }

        public static byte[] Concat(params byte[][] parts)
        {
            return parts.SelectMany(part => part).ToArray();
        }

        /// <summary>
        /// The default stream used by the console app: line noise, a frame with a
        /// bad checksum, more noise, a truncated frame that runs straight into the
        /// good frame, and trailing noise.
        /// </summary>
        public static byte[] BuildNoisyStream(string payload)
        {
            return Concat(
                Garbage(11, 1),
                BuildFrameWithBadChecksum("BADCHECKSUM"),
                Garbage(5, 2),
                BuildTruncatedFrame("TRUNCATED"),
                BuildFrame(payload),
                Garbage(9, 3));
        }

        /// <summary>A stream that never contains a valid frame.</summary>
        public static byte[] BuildGarbageOnlyStream()
        {
            return Garbage(256, 4);
        }

        private static byte[] BuildFrame(string payload, byte checksum)
        {
            var frame = new List<byte> { STX };
            frame.AddRange(Encoding.ASCII.GetBytes(payload));
            frame.Add(ETX);
            frame.AddRange(Encoding.ASCII.GetBytes(checksum.ToString("X2")));
            return frame.ToArray();
        }
    }
}
