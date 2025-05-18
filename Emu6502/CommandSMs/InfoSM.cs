namespace Emu6502
{
    internal class InfoSM(SerialDrive drive, RS232Interface port) : CommandStateMachine(drive, port)
    {
        public override bool NeedsFileName => true;
        public override bool NeedsOffset => false;
        public override bool NeedsLength => false;

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
                port.Write(SerialDrive.ACK);
                //statusCode = StatusCode.OK;
                count = 0;
                length = 6;
                MemoryStream ms = new MemoryStream(length);
                ushort len = (ushort)new FileInfo(name).Length;
                ms.WriteByte((byte)len);
                ms.WriteByte((byte)(len >> 8));

                ulong date = (ulong)((DateTimeOffset)File.GetCreationTimeUtc(name)).ToUnixTimeSeconds();
                ms.WriteByte((byte)date);
                ms.WriteByte((byte)(date >> 8));
                ms.WriteByte((byte)(date >> 16));
                ms.WriteByte((byte)(date >> 24));
                
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
