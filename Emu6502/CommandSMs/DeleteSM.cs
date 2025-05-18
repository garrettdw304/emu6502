namespace Emu6502
{
    internal class DeleteSM(SerialDrive drive, RS232Interface port) : CommandStateMachine(drive, port)
    {
        public override bool NeedsFileName => true;
        public override bool NeedsOffset => false;
        public override bool NeedsLength => false;

        public override bool Start(string fileName, ushort offset, ushort length)
        {
            string? name = drive.ValidateFileName(fileName.ToString());
            if (name == null)
            {
                port.Write(SerialDrive.NAK);
                //statusCode = StatusCode.FILE_NAME_INVALID;
            }
            else if (!File.Exists(name))
            {
                port.Write(SerialDrive.NAK);
                //statusCode = StatusCode.FILE_NOT_FOUND;
            }
            else
            {
                port.Write(SerialDrive.ACK);
                //statusCode = StatusCode.OK;
                File.Delete(name);
            }

            return true;
        }

        public override bool Exe()
        {
            throw new InvalidOperationException();
        }
    }
}
