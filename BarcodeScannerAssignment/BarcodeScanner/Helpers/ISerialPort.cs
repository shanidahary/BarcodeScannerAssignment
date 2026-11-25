using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BarcodeScanner.Helpers
{
    public interface ISerialPort
    {
        bool IsOpen();
        void Open();
        void Close();

        /// <summary>
        /// Copies whatever bytes are currently available into <paramref name="buffer"/>,
        /// starting at <paramref name="offset"/>, at most <paramref name="count"/> bytes.
        /// Returns the number of bytes copied, which may be 0 if nothing has arrived yet.
        /// The number of bytes returned is NOT related to message boundaries.
        /// </summary>
        int Read(byte[] buffer, int offset, int count);
    }
}
