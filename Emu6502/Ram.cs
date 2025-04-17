namespace Emu6502
{
    public class Ram
    {
        private byte[] data;
        private ushort baseAddress;

        public Ram(ushort baseAddress, int size)
        {
            if (size < 0 || size > 65536)
                throw new ArgumentOutOfRangeException(nameof(size));

            if (baseAddress + size > 65536)
                throw new ArgumentException(
                    "Mapped memory is out of bounds. Final address: "
                    + (baseAddress + size));

            data = new byte[size];
            this.baseAddress = baseAddress;
        }

        public void OnCycle(IDeviceInterface bc)
        {
            if (!InRange(bc.Address))
                return;

            if (bc.Rwb)
                bc.Data = data[Relative(bc.Address)];
            else
                data[Relative(bc.Address)] = bc.Data;
        }

        private bool InRange(ushort address)
        {
            return address >= baseAddress
                && address < (baseAddress + data.Length);
        }

        private ushort Relative(ushort address)
        {
            return (ushort)(address - baseAddress);
        }
    }
}
