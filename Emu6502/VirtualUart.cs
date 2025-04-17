using System.IO.Ports;

namespace Emu6502
{
    public class VirtualUart
    {
        // Status Bits
        private const byte RX_NOT_EMPTY_MASK = 0b0000_0001;

        // Registers
        private const ushort RXTX_REG = 0;
        private const ushort STATUS_REG = 1;

        private const byte REGISTERS = 2;

        private readonly ushort baseAddress;
        private readonly SerialPort port;

        /// <summary>
        /// Receiver Data Register
        /// </summary>
        private byte rdr;

        private byte Status
        {
            get
            {
                byte toReturn = 0;
                if (port.BytesToRead > 0)
                    toReturn |= RX_NOT_EMPTY_MASK;
                return toReturn;
            }
        }

        public VirtualUart(ushort baseAddress, string portName)
        {
            this.baseAddress = baseAddress;
            port = new SerialPort(portName, 9600, Parity.None, 8, StopBits.One);
            port.Open();

            rdr = 0;
        }

        public void OnCycle(IDeviceInterface bc)
        {
            if (!InRange(bc.Address))
                return;

            ushort address = Relative(bc.Address);

            if (bc.Rwb)
            {
                if (address == RXTX_REG)
                {
                    if (port.BytesToRead > 0)
                    {
                        int result = port.ReadByte();
                        if (result == -1)
                            throw new Exception("End of stream.");
                        rdr = (byte)result;
                    }
                    bc.Data = rdr;
                }
                else if (address == STATUS_REG)
                    bc.Data = Status;
            }
            else
            {
                if (address == RXTX_REG)
                {
                    byte[] buf = [bc.Data];
                    port.Write(buf, 0, 1);
                }
                else if (address == STATUS_REG)
                    _ = 0;
            }
        }

        private bool InRange(ushort address)
        {
            return address >= baseAddress
                && address < (baseAddress + REGISTERS);
        }

        private ushort Relative(ushort address)
        {
            return (ushort)(address - baseAddress);
        }
    }
}
