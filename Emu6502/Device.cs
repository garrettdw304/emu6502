namespace Emu6502
{
    public abstract class Device
    {
        public readonly ushort baseAddress;
        
        public abstract int Length { get; }

        public Device(ushort baseAddress)
        {
            this.baseAddress = baseAddress;
        }

        public abstract void OnCycle(IDeviceInterface bc);

        public bool InRange(ushort address)
        {
            return address >= baseAddress
                && address < (baseAddress + Length);
        }

        public ushort Relative(ushort address)
        {
            return (ushort)(address - baseAddress);
        }
    }
}
