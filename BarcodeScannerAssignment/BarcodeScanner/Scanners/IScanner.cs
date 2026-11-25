using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BarcodeScanner.Scanners
{
    public interface IScanner
    {
        /// <summary>
        /// Returns the payload of the first valid frame read from the port,
        /// or null if <paramref name="waitingTime"/> milliseconds elapse first.
        /// </summary>
        string? GetScannedString(int waitingTime);

        void Dispose();
    }
}
