namespace Emu6502
{
    public class Ram : Device
    {
        private readonly byte[] data;

        public override int Length => data.Length;

        public Ram(ushort baseAddress, int size) : base(baseAddress)
        {
            if (size < 0 || size > 65536)
                throw new ArgumentOutOfRangeException(nameof(size));

            if (baseAddress + size > 65536)
                throw new ArgumentException(
                    "Mapped memory is out of bounds. Final address: "
                    + (baseAddress + size));

            data = new byte[size];
        }

        /// <summary>
        /// For display use.
        /// </summary>
        public byte this[int address]
        {
            get
            {
                return data[address];
            }
        }

        public override void OnCycle(IDeviceInterface bc)
        {
            if (!InRange(bc.Address))
                return;

            if (bc.Rwb)
                bc.Data = data[Relative(bc.Address)];
            else
                data[Relative(bc.Address)] = bc.Data;
        }
    }
}
