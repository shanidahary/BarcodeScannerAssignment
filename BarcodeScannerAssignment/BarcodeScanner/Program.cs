using BarcodeScanner.Helpers;
using BarcodeScanner.Scanners;

namespace BarcodeScanner
{
    class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Please enter the waitingTime in milliseconds to wait for an operator to scan:");
            int waitingTime = int.Parse(Console.ReadLine() ?? "0");

            (string com, int baudrate) = HelperFunctions.GetComAndBaudRate();//already implemented

            IScanner scanner = new ScannerA(com, baudrate);

            try
            {
                string? scannedString = scanner.GetScannedString(waitingTime);
                Console.WriteLine(scannedString ?? "<no scan within the waiting time>");
            }
            finally
            {
                scanner.Dispose();
            }
        }
    }
}
