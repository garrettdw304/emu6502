namespace Emu6502
{
    internal class ReadSM(SerialDrive drive, RS232Interface port) : CommandStateMachine(drive, port)
    {
        public override bool NeedsFileName => true;
        public override bool NeedsOffset => true;
        public override bool NeedsLength => true;

        private ushort offset = 0;

        private byte[] data = [];
        private int count = 0;

        public override bool Start(string fileName, ushort offset, ushort length)
        {
            // Read file and store into data array
            string? name = drive.ValidateFileName(fileName.ToString());
            if (name == null)
            {
                port.Write(SerialDrive.NAK); // TODO: Port may not be clear to send (fix by adding new states for sending ACK/NAK and wait in those states until clear to send)
                //statusCode = StatusCode.FILE_NAME_INVALID;
                return true;
            }
            else if (!File.Exists(name))
            {
                port.Write(SerialDrive.NAK);
                //statusCode = StatusCode.FILE_NOT_FOUND;
                return true;
            }
            else
            {
                port.Write(SerialDrive.ACK);
                //statusCode = StatusCode.OK;
                count = 0;
                this.offset = offset;

                // Read file data
                data = File.ReadAllBytes(name);
                // Get length of file.
                length = Math.Min(length, (ushort)data.Length); // TODO: Take into account offset (also use offset elsewhere, we are reading offset but we are never using it)

                // Combine length and file data to be sent.
                MemoryStream ms = new MemoryStream(length + 2);
                ms.WriteByte((byte)length); ms.WriteByte((byte)(length >> 8)); ms.Write(data, 0, length);
                // Set length to the length of all data to be sent.
                // Set data to be sent.
                data = ms.ToArray();

                return false;
            }
        }

        public override bool Exe()
        {
            if (!port.ClearToSend())
                return false;

            port.Write(data[count++]);
            if (count == data.Length)
                return true;
            else
                return false;
        }
    }
}
