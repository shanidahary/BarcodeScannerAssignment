using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BarcodeScanner.Helpers
{
    /// <summary>
    /// Already implemented. You do not need to change anything in this file.
    ///
    /// Stands in for a real serial port. It hands out a fixed byte stream in small,
    /// arbitrarily sized chunks, exactly the way a real port does: a chunk may contain
    /// part of a frame, several frames, or nothing at all.
    /// </summary>
    public class SerialPortMock : ISerialPort
    {
        private readonly byte[] data;
        private readonly Random rng;
        private readonly int maxChunk;
        private readonly int readDelayMs;
        private readonly bool repeat;

        private int position;
        private bool isOpen;

        /// <summary>Used by the console app. Produces the default noisy stream.</summary>
        public SerialPortMock(string com, int baudrate)
            : this(HelperFunctions.BuildNoisyStream("123456789"))
        {
        }

        /// <summary>Used by tests, so you can feed the scanner any stream you like.</summary>
        /// <param name="data">The bytes the port will emit.</param>
        /// <param name="seed">Fixes the chunk sizes, so a test is repeatable.</param>
        /// <param name="maxChunk">Largest number of bytes a single Read can return.</param>
        /// <param name="readDelayMs">How long each Read takes.</param>
        /// <param name="repeat">When true the stream loops forever instead of running dry.</param>
        public SerialPortMock(byte[] data, int seed = 20260813, int maxChunk = 7,
                              int readDelayMs = 5, bool repeat = false)
        {
            this.data = data ?? throw new ArgumentNullException(nameof(data));
            this.rng = new Random(seed);
            this.maxChunk = Math.Max(1, maxChunk);
            this.readDelayMs = Math.Max(0, readDelayMs);
            this.repeat = repeat;
        }

        public bool IsOpen()
        {
            return isOpen;
        }

        public void Open()
        {
            isOpen = true;
        }

        public void Close()
        {
            isOpen = false;
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (offset < 0 || count < 0 || offset + count > buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            if (!isOpen)
            {
                throw new InvalidOperationException("Port is not open.");
            }

            Thread.Sleep(readDelayMs);

            if (position >= data.Length)
            {
                if (!repeat)
                {
                    return 0;
                }

                position = 0;
            }

            int available = data.Length - position;
            int bytesToCopy = Math.Min(Math.Min(rng.Next(1, maxChunk + 1), count), available);

            Array.Copy(data, position, buffer, offset, bytesToCopy);
            position += bytesToCopy;

            return bytesToCopy;
        }
    }
}
