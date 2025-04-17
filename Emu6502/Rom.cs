using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emu6502
{
    public class Rom
    {
        private byte[] data;
        private ushort baseAddress;

        public Rom(ushort baseAddress, int size)
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
