namespace Emu6502
{
    public abstract class Device
    {
        protected readonly ushort baseAddress;
        
        protected abstract int Length { get; }

        public Device(ushort baseAddress)
        {
            this.baseAddress = baseAddress;
        }

        public abstract void OnCycle(IDeviceInterface bc);

        protected bool InRange(ushort address)
        {
            return address >= baseAddress
                && address < (baseAddress + Length);
        }

        protected ushort Relative(ushort address)
        {
            return (ushort)(address - baseAddress);
        }
    }
}
