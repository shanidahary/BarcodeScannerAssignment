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
    }
}
