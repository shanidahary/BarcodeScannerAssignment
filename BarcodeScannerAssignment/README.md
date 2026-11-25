# Barcode scanner

The scanner reads bytes from the port and checks them one at a time. First, it waits for STX, which marks the start of a message. Then it saves the barcode bytes until it reaches ETX. After that, it reads the two checksum characters. The scanner keeps track of which part it is reading, so it can handle a message that arrives over several reads.

While reading the barcode, the scanner calculates a checksum by applying XOR to its bytes. It then compares that result with the two checksum characters sent by the scanner. If they match and the barcode is valid, it returns the barcode.

The barcode must contain 1 to 64 printable ASCII characters. If a message contains an invalid character, is too long, or has an invalid checksum, the scanner looks for the next message. If another STX arrives in the middle of a message, it starts again from that byte.

The scanner uses a stopwatch to track the total waiting time. Each port read runs in a task so the scanner can stop waiting when the time is up. If a read is still running, the next call can use its result. Bytes already read after a valid message are also kept for the next call. The port stays open after a timeout and is closed by `Dispose`.

With more time, I would add a way to cancel a port read when the waiting time is over.
