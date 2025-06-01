namespace Emu6502
{
    internal abstract class CommandStateMachine
    {
        public abstract bool NeedsFileName { get; }
        public abstract bool NeedsOffset { get; }
        public abstract bool NeedsLength { get; }

        protected readonly SerialDrive drive;
        protected readonly SerialInterface port;

        public CommandStateMachine(SerialDrive drive, SerialInterface port)
        {
            this.drive = drive;
            this.port = port;
        }

        /// <returns>False if this SM is not done handling the command and the serial drive should stay in the exe state. True if this SM is done handling the command and the serial drive should go to the idle state.</returns>
        public abstract bool Start(string fileName, ushort offset, ushort length);

        /// <returns>False if this SM is not done handling the command and the serial drive should stay in the exe state. True if this SM is done handling the command and the serial drive should go to the idle state.</returns>
        public abstract bool Exe();
    }
}
