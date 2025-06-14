﻿namespace Emu6502
{
    internal class WriteSM(SerialDrive drive, SerialInterface port) : CommandStateMachine(drive, port)
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
                    File.WriteAllBytes(name, data);
                    return true;
                }
            }
        }

        public override bool Exe()
        {
            if (!port.Available())
                return false;

            data[count++] = port.Read();
            if (count == data.Length)
            {
                File.WriteAllBytes(fileName, data);
                return true;
            }
            else
                return false;
        }
    }
}
