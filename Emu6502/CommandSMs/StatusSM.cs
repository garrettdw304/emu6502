namespace Emu6502
{
    internal class StatusSM(SerialDrive drive, SerialInterface port) : CommandStateMachine(drive, port)
    {
        public override bool NeedsFileName => throw new NotImplementedException();
        public override bool NeedsOffset => throw new NotImplementedException();
        public override bool NeedsLength => throw new NotImplementedException();

        public override bool Start(string fileName, ushort offset, ushort length)
        {
            throw new NotImplementedException();
        }

        public override bool Exe()
        {
            throw new NotImplementedException();
        }
    }
}
