using System.IO.Ports;

namespace Emu6502
{
    public class VirtualUart : Device, IDisposable
    {
        // Status Bits
        private const byte RDR_FULL_MASK =  0b0000_0001;
        private const byte TDR_EMPTY_MASK = 0b0000_0010;

        // Registers
        private const ushort RXTX_REG = 0;
        private const ushort STATUS_REG = 1;

        private readonly SerialPort port;
        public string PortName => port.PortName;

        /// <summary>
        /// Receiver Data Register
        /// </summary>
        private byte rdr;

        public override int Length => 2;

        private byte Status
        {
            get
            {
                byte toReturn = 0;
                if (port.BytesToRead > 0)
                    toReturn |= RDR_FULL_MASK;
                if (port.BytesToWrite == 0)
                    toReturn |= TDR_EMPTY_MASK;
                return toReturn;
            }
        }

        public VirtualUart(ushort baseAddress, string portName) : base(baseAddress)
        {
            port = new SerialPort(portName, 9600, Parity.None, 8, StopBits.One);
            port.WriteTimeout = 1;
            port.ReadTimeout = 1;
            port.Open();

            rdr = 0;
        }

        public override void OnCycle(IDeviceInterface bc)
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
                    if (port.BytesToWrite == 0 && port.CtsHolding)
                        port.Write([bc.Data], 0, 1);
                }
                else if (address == STATUS_REG)
                    _ = 0;
            }
        }

        public void Dispose()
        {
            port.Close();
            port.Dispose();
        }
    }
}
