using BarcodeScanner.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BarcodeScanner.Scanners
{
    public class ScannerA : IScanner
    {
        private readonly ISerialPort port;

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
            //TODO - implement this method. See README.md for the frame format and the rules.
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            //TODO - implement this method.
            //       The port must end up closed, whatever happened during the scan,
            //       and calling Dispose more than once must not throw.
            throw new NotImplementedException();
        }

        #endregion

        #region Private methods

        // Add whatever you need here. You are free to add fields, types and files.

        #endregion
    }
}
