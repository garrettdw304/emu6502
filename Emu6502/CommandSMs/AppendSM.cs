namespace Emu6502
{
    internal class AppendSM(SerialDrive drive, RS232Interface port) : CommandStateMachine(drive, port)
    {
        public override bool NeedsFileName => true;
        public override bool NeedsOffset => false;
        public override bool NeedsLength => true;

        private string fileName = "";

        private byte[] data = [];
        private int count = 0;

        public override bool Start(string fileName, ushort offset, ushort length)
        {
            string? name = drive.ValidateFileName(fileName.ToString());
            if (name == null)
            {
                port.Write(SerialDrive.NAK);
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
                this.fileName = name;
                port.Write(SerialDrive.ACK);
                //statusCode = StatusCode.OK;
                count = 0;
                data = new byte[length];
                if (length > 0)
                    return false;
                else
                {
                    using (var stream = new FileStream(fileName.ToString(), FileMode.Append))
                        stream.Write(data, 0, length);
                    return true;
                }
            }
        }

        public override bool Exe()
        {
            if (!port.Available())
                return false;

            data[count++] = port.Read();
            if (count < data.Length)
                return false;

            using (var stream = new FileStream(fileName.ToString(), FileMode.Append))
                stream.Write(data);

            return true;
        }
    }
}
