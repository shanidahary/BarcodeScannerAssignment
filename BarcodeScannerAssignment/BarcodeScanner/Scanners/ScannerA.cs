using BarcodeScanner.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BarcodeScanner.Scanners
{
    public class ScannerA : IScanner
    {
        private readonly ISerialPort port;

        private enum ParseState
        {
            WaitingForStart,
            ReadingPayload,
            ReadingChecksumHigh,
            ReadingChecksumLow
        }

        private ParseState state = ParseState.WaitingForStart;
        private readonly byte[] payload = new byte[HelperFunctions.MaxPayloadLength];
        private int payloadLength = 0;
        private byte checksum = 0;
        private int checksumHigh = 0;
        private readonly byte[] readBuffer = new byte[256];
        private int readOffset;
        private int readLength;
        private Task<int>? pendingRead;
        private bool disposed;

        public ScannerA(string com, int baudrate)
            : this(new SerialPortMock(com, baudrate))
        {
        }

        /// <summary>Lets a test drive the scanner with a stream of its own choosing.</summary>
        public ScannerA(ISerialPort port)
        {
            this.port = port ?? throw new ArgumentNullException(nameof(port));

            if (!this.port.IsOpen())
            {
                this.port.Open();
            }
        }

        #region Public methods

        public string? GetScannedString(int waitingTime)
        {
            var stopwatch = Stopwatch.StartNew();
            if (waitingTime <= 0)
            {
                return null;
            }

            while (stopwatch.ElapsedMilliseconds < waitingTime)
            {
                if (readOffset < readLength)
                {
                    string? result = ParseByte(readBuffer[readOffset]);
                    readOffset++;

                    if (result != null)
                    {
                        return result;
                    }

                    continue;
                }

                // Keep a pending read between calls so reads never share the buffer.
                pendingRead ??= Task.Run(() => port.Read(readBuffer, 0, readBuffer.Length));
                int remaining = (int)Math.Max(0, waitingTime - stopwatch.ElapsedMilliseconds);
                if (Task.WaitAny(new Task[] { pendingRead }, remaining) == -1 ||
                    stopwatch.ElapsedMilliseconds >= waitingTime)
                {
                    return null;
                }

                Task<int> completedRead = pendingRead;
                pendingRead = null;
                readLength = completedRead.GetAwaiter().GetResult();
                readOffset = 0;

                if (readLength == 0)
                {
                    Thread.Sleep(1);
                }
            }

            return null;
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            port.Close();
            disposed = true;

            // Closing the port can cause a read that timed out to fail later.
            _ = pendingRead?.ContinueWith(
                read => { _ = read.Exception; },
                TaskContinuationOptions.OnlyOnFaulted);
        }

        #endregion

        #region Private methods

        private string? ParseByte(byte value)
        {
            // STX starts a new frame, even when a previous frame was incomplete.
            if (value == HelperFunctions.STX)
            {
                state = ParseState.ReadingPayload;
                payloadLength = 0;
                checksum = 0;
                return null;
            }

            switch (state)
            {
                case ParseState.WaitingForStart:
                    // Ignore noise until a frame starts.
                    break;

                case ParseState.ReadingPayload:
                    if (value == HelperFunctions.ETX)
                    {
                        if (payloadLength > 0)
                        {
                            state = ParseState.ReadingChecksumHigh;
                        }
                        else
                        {
                            // An empty payload is invalid.
                            state = ParseState.WaitingForStart;
                        }
                    }
                    else if (value >= 0x20 && value <= 0x7E &&
                             payloadLength < HelperFunctions.MaxPayloadLength)
                    {
                        payload[payloadLength] = value;
                        payloadLength++;
                        checksum ^= value;
                    }
                    else
                    {
                        // Reject invalid characters and oversized payloads.
                        state = ParseState.WaitingForStart;
                    }

                    break;

                case ParseState.ReadingChecksumHigh:
                    checksumHigh = HexValue(value);
                    if (checksumHigh < 0)
                    {
                        state = ParseState.WaitingForStart;
                    }
                    else
                    {
                        state = ParseState.ReadingChecksumLow;
                    }

                    break;

                case ParseState.ReadingChecksumLow:
                    int checksumLow = HexValue(value);
                    state = ParseState.WaitingForStart;

                    if (checksumLow >= 0 && checksumHigh * 16 + checksumLow == checksum)
                    {
                        return Encoding.ASCII.GetString(payload, 0, payloadLength);
                    }

                    break;
            }

            return null;
        }

        private static int HexValue(byte value)
        {
            if (value >= '0' && value <= '9')
            {
                return value - '0';
            }

            if (value >= 'A' && value <= 'F')
            {
                return value - 'A' + 10;
            }

            return -1;
        }

        #endregion
    }
}
