using BarcodeScanner.Helpers;
using BarcodeScanner.Scanners;
using System.Diagnostics;

namespace BarcodeScanner.Tests
{
    /// <summary>
    /// These are examples, not the full grading suite. Part of the assignment is
    /// adding the tests you think are missing.
    /// </summary>
    public class ScannerTests
    {
        [Fact]
        public void GetScannedString_CleanStream_ReturnsPayload()
        {
            // Arrange
            byte[] stream = HelperFunctions.BuildFrame("123456789");
            IScanner scanner = new ScannerA(new SerialPortMock(stream));

            // Act
            string? result = scanner.GetScannedString(5000);

            // Assert
            Assert.Equal("123456789", result);

            // Cleanup
            scanner.Dispose();
        }

        [Fact]
        public void GetScannedString_NoisyStream_SkipsBadFramesAndReturnsPayload()
        {
            // Arrange
            byte[] stream = HelperFunctions.BuildNoisyStream("123456789");
            IScanner scanner = new ScannerA(new SerialPortMock(stream));

            // Act
            string? result = scanner.GetScannedString(5000);

            // Assert
            Assert.Equal("123456789", result);

            // Cleanup
            scanner.Dispose();
        }

        [Fact]
        public void GetScannedString_FrameSplitAcrossManyReads_ReturnsPayload()
        {
            // Arrange - one byte per read, so every frame is split
            byte[] stream = HelperFunctions.BuildNoisyStream("ABC-123");
            IScanner scanner = new ScannerA(new SerialPortMock(stream, maxChunk: 1, readDelayMs: 0));

            // Act
            string? result = scanner.GetScannedString(5000);

            // Assert
            Assert.Equal("ABC-123", result);

            // Cleanup
            scanner.Dispose();
        }

        [Fact]
        public void GetScannedString_NoValidFrame_ReturnsNullWithinTheWaitingTime()
        {
            // Arrange - noise forever, a valid frame never arrives
            byte[] stream = HelperFunctions.BuildGarbageOnlyStream();
            IScanner scanner = new ScannerA(new SerialPortMock(stream, repeat: true));

            // Act
            var stopwatch = Stopwatch.StartNew();
            string? result = scanner.GetScannedString(500);
            stopwatch.Stop();

            // Assert
            Assert.Null(result);
            Assert.InRange(stopwatch.ElapsedMilliseconds, 400L, 3000L);

            // Cleanup
            scanner.Dispose();
        }

        [Fact]
        public void Dispose_ClosesThePort_AndIsSafeToCallTwice()
        {
            // Arrange
            var port = new SerialPortMock(HelperFunctions.BuildFrame("123456789"));
            IScanner scanner = new ScannerA(port);
            Assert.True(port.IsOpen());

            // Act
            scanner.Dispose();
            scanner.Dispose();

            // Assert
            Assert.False(port.IsOpen());
        }

        [Fact]
        public void GetScannedString_MaximumPayloadLength_ReturnsPayload()
        {
            string payload = " " + new string('A', 62) + "~";
            var scanner = new ScannerA(new SerialPortMock(
                HelperFunctions.BuildFrame(payload), maxChunk: 1, readDelayMs: 0));

            try
            {
                Assert.Equal(payload, scanner.GetScannedString(5000));
            }
            finally
            {
                scanner.Dispose();
            }
        }

        [Theory]
        [InlineData("\u0002\u000300")] // Empty payload.
        [InlineData("\u0002\u007F\u00037F")] // Non-printable payload byte.
        [InlineData("\u0002J\u0003G0")] // Invalid first checksum character.
        [InlineData("\u0002J\u00034G")] // Invalid second checksum character.
        [InlineData("\u0002J\u00034a")] // Lowercase checksum character.
        [InlineData("\u0002J\u0003")] // New STX in first checksum position.
        [InlineData("\u0002J\u00034")] // New STX in second checksum position.
        public void GetScannedString_InvalidFrame_SkipsToNextValidFrame(string invalidFrame)
        {
            byte[] stream = HelperFunctions.Concat(
                System.Text.Encoding.ASCII.GetBytes(invalidFrame),
                HelperFunctions.BuildFrame("GOOD"));
            var scanner = new ScannerA(new SerialPortMock(stream, readDelayMs: 0));

            try
            {
                Assert.Equal("GOOD", scanner.GetScannedString(5000));
            }
            finally
            {
                scanner.Dispose();
            }
        }

        [Fact]
        public void GetScannedString_PayloadTooLong_SkipsToNextValidFrame()
        {
            byte[] stream = HelperFunctions.Concat(
                HelperFunctions.BuildFrame(new string('A', 65)),
                HelperFunctions.BuildFrame("GOOD"));
            var scanner = new ScannerA(new SerialPortMock(stream, readDelayMs: 0));

            try
            {
                Assert.Equal("GOOD", scanner.GetScannedString(5000));
            }
            finally
            {
                scanner.Dispose();
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void GetScannedString_NonpositiveWaitingTime_DoesNotRead(int waitingTime)
        {
            var port = new CountingPort();
            var scanner = new ScannerA(port);

            try
            {
                Assert.Null(scanner.GetScannedString(waitingTime));
                Assert.Equal(0, port.ReadCount);
                Assert.True(port.IsOpen());
            }
            finally
            {
                scanner.Dispose();
            }
        }

        private sealed class CountingPort : ISerialPort
        {
            private bool isOpen;

            public int ReadCount { get; private set; }

            public bool IsOpen() => isOpen;

            public void Open() => isOpen = true;

            public void Close() => isOpen = false;

            public int Read(byte[] buffer, int offset, int count)
            {
                ReadCount++;
                return 0;
            }
        }
    }
}
