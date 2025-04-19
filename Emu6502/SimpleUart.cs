using System.Collections.Concurrent;

namespace Emu6502
{
    public class SimpleUart : Device
    {
        // Status Bits
        private const byte RX_NOT_EMPTY_MASK = 0b0000_0001;

        // Registers
        private const ushort RXTX_REG = 0;
        private const ushort STATUS_REG = 1;

        protected override int Length => 2;

        /// <summary>
        /// To be used by the external device (aka not by the CPU) connected to this serial port.
        /// </summary>
        public event Action? OnDataAvailable;

        private readonly ConcurrentQueue<byte> tx;
        private readonly ConcurrentQueue<byte> rx;

        private byte Status
        {
            get
            {
                byte toReturn = 0;
                if (!rx.IsEmpty)
                    toReturn |= RX_NOT_EMPTY_MASK;
                return toReturn;
            }
        }

        public SimpleUart(ushort baseAddress) : base(baseAddress)
        {
            tx = new ConcurrentQueue<byte>();
            rx = new ConcurrentQueue<byte>();
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
                    if (rx.TryDequeue(out byte data))
                        bc.Data = data;
                    else
                        bc.Data = 0;
                }
                else if (address == STATUS_REG)
                    bc.Data = Status;
            }
            else
            {
                if (address == RXTX_REG)
                {
                    tx.Enqueue(bc.Data);
                    OnDataAvailable?.Invoke();
                }
                else if (address == STATUS_REG)
                    _ = 0; // TODO: Software reset
            }
        }

        public void Send(byte data)
        {
            rx.Enqueue(data);
        }

        public bool TryReceive(out byte data)
        {
            return tx.TryDequeue(out data);
        }
    }
}
