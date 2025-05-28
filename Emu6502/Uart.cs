namespace Emu6502
{
    public class Uart : Device
    {
        // Status Bits
        private const byte RDR_FULL_MASK = 0b0000_0001;
        private const byte TDR_EMPTY_MASK = 0b0000_0010;

        // Registers
        private const ushort RXTX_REG = 0;
        private const ushort STATUS_REG = 1;

        private const int TX_BAUD_RATE = 9600;
        private const int RX_BAUD_RATE = 9600;
        /// <summary>
        /// Transmit time based on 1 start bit, 1 stop bit, 8 bit word, no parity and <see cref="TX_BAUD_RATE"/>.
        /// </summary>
        private const float TRANSMIT_TIME = 1f / (((float)TX_BAUD_RATE) / (1 + 8 + 1 + 0));
        /// <summary>
        /// Receive time based on 1 start bit, 1 stop bit, 8 bit word, no parity and <see cref="RX_BAUD_RATE"/>.
        /// </summary>
        private const float RECEIVE_TIME = 1f / (((float)RX_BAUD_RATE) / (1 + 8 + 1 + 0));

        private static readonly IRS232Interface EMPTY_PORT = new Empty();

        private IRS232Interface port;
        public IRS232Interface? Port
        {
            get
            {
                return port == EMPTY_PORT ? null : port;
            }

            set
            {
                port = value ?? EMPTY_PORT;
            }
        }

        /// <summary>
        /// Receive Data Register
        /// </summary>
        private byte rdr = 0;
        /// <summary>
        /// Transmit Data Register
        /// </summary>
        private byte tdr = 0;
        /// <summary>
        /// Receive Shift Register
        /// </summary>
        private byte rsr = 0;
        /// <summary>
        /// Transmit Shift Register
        /// </summary>
        private byte tsr = 0;

        /// <summary>
        /// How many seconds until the data in the rsr will be received into the rdr.
        /// </summary>
        private float receiveInSeconds = 0;
        /// <summary>
        /// How many seconds until the data in the tsr will be transmitted out the serial port.
        /// </summary>
        private float transmitInSeconds = 0;
        /// <summary>
        /// If there is a byte in the rsr that will be transfered to the rdr when <see cref="receiveInSeconds"/> reaches 0.
        /// </summary>
        private bool receiving = false;
        /// <summary>
        /// If there is a byte in the tsr that will be transfered out the serial port when <see cref="transmitInSeconds"/> reaches 0.
        /// </summary>
        private bool transmitting = false;
        /// <summary>
        /// If a byte is in the rdr that has not been read by the cpu yet.
        /// </summary>
        private bool rdrFull = false;
        /// <summary>
        /// If the byte in the tdr has been sent to the tsr.
        /// </summary>
        private bool tdrEmpty = true;

        public override int Length => 2;

        private byte Status
        {
            get
            {
                byte toReturn = 0;
                if (rdrFull)
                    toReturn |= RDR_FULL_MASK;
                if (tdrEmpty)
                    toReturn |= TDR_EMPTY_MASK;
                return toReturn;
            }
        }

        public Uart(ushort baseAddress, IRS232Interface? port = null) : base(baseAddress)
        {
            this.port = port ?? EMPTY_PORT;
        }

        public override void OnCycle(IDeviceInterface bc)
        {
            HandleTransfers(bc.Hz);

            if (!InRange(bc.Address))
                return;

            ushort reg = Relative(bc.Address);

            if (bc.Rwb)
            {
                if (reg == RXTX_REG)
                {
                    bc.Data = rdr;
                    rdrFull = false;
                }
                else if (reg == STATUS_REG)
                    bc.Data = Status;
            }
            else
            {
                if (reg == RXTX_REG)
                {
                    tdr = bc.Data;
                    tdrEmpty = false;

                    if (!transmitting)
                    {
                        tsr = tdr;
                        transmitInSeconds = TRANSMIT_TIME;
                        tdrEmpty = true;
                        transmitting = true;
                    }
                }
                else if (reg == STATUS_REG)
                    _ = 0;
            }
        }

        private void HandleTransfers(int hz)
        {
            if (transmitting)
            {
                transmitInSeconds -= 1f / hz;

                if (transmitInSeconds <= 0)
                {
                    port.Write(tsr);

                    if (!tdrEmpty)
                    {
                        tsr = tdr;
                        transmitInSeconds = TRANSMIT_TIME;
                        tdrEmpty = true;
                    }
                    else
                        transmitting = false;
                }
            }

            if (receiving)
            {
                receiveInSeconds -= 1f / hz;

                if (receiveInSeconds <= 0)
                {
                    rdr = rsr;
                    rdrFull = true;

                    if (port.Available())
                    {
                        rsr = port.Read();
                        receiveInSeconds = RECEIVE_TIME;
                    }
                    else
                        receiving = false;
                }
            }
            else if (port.Available())
            {
                rsr = port.Read();
                receiveInSeconds = RECEIVE_TIME;
                receiving = true;
            }
        }

        private class Empty : IRS232Interface
        {
            public bool Available() => false;
            public bool ClearToSend() => false;
            public byte Read() => 0;
            public void Write(byte value) { }
        }
    }
}
