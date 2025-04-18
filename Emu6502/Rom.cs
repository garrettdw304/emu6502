namespace Emu6502
{
    public class Rom : Device
    {
        private readonly byte[] data;

        protected override int Length => data.Length;

        public Rom(ushort baseAddress, int size) : base(baseAddress)
        {
            if (size < 0 || size > 65536)
                throw new ArgumentOutOfRangeException(nameof(size));

            if (baseAddress + size > 65536)
                throw new ArgumentException(
                    "Mapped memory is out of bounds. Final address: "
                    + (baseAddress + size));

            data = new byte[size];
        }

        public override void OnCycle(IDeviceInterface bc)
        {
            if (!InRange(bc.Address))
                return;

            if (bc.Rwb)
                bc.Data = data[Relative(bc.Address)];
        }

        public void Program(byte[] data)
        {
            if (data.Length > this.data.Length)
                throw new ArgumentException(
                    "Provided data is larger than this ROM.");

            data.CopyTo(this.data, 0);
        }

        public void Program(string fileName)
        {
            Program(File.ReadAllBytes(fileName));
        }
    }
}
