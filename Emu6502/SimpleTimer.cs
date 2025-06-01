namespace Emu6502
{
    public class SimpleTimer : Device
    {
        public override int Length => 4;

        /// <summary>
        /// dddddIMM
        /// <br/>I - 0 for no interrupt, 1 for interrupt.
        /// <br/>M - 0 for paused, 1 for oneshot, 2 for freerun.
        /// </summary>
        private const int CTRL = 0;
        /// <summary>
        /// ddddddRI
        /// I - 0 for not interrupting, 1 for interrupting.
        /// R - 0 for paused, 1 for running.
        /// </summary>
        private const int STATUS = 1;
        private const int TIME_LO = 2;
        private const int TIME_HI = 3;

        private const int MODE_MASK = 0b11;
        private const int MODE_PAUSED = 0b00;
        /// <summary>
        /// When the timer expires an interrupt occurs and the timer IS NOT reset.
        /// </summary>
        private const int MODE_ONESHOT = 0b01;
        /// <summary>
        /// When the timer expires an interrupt occurs and the timer IS reset.
        /// </summary>
        private const int MODE_FREERUN = 0b10;

        private const int INTR_MASK = 0b100;
        private const int INTR_DISABLED = 0b000;
        private const int INTR_ENABLED = 0b100;

        private const int STATUS_INTERRUPTING_MASK = 0b1;
        private const int STATUS_RUNNING_MASK = 0b10;

        private readonly InterruptLine irq;

        private byte ctrl;
        private ushort time;

        // Used to reset the timer in freerun mode.
        private byte timeLoLatch;
        private byte timeHiLatch;

        private byte timeHiLatchOut;

        private byte Mode
        {
            get => (byte)(ctrl & MODE_MASK);
            set => ctrl = (byte)((ctrl & ~MODE_MASK) | (value & MODE_MASK)); // AND first to make sure it can't change settings other than mode.
        }

        private byte InterruptEnabled
        {
            get => (byte)(ctrl & INTR_MASK);
            set => ctrl = (byte)((ctrl & ~INTR_MASK) | (value & INTR_MASK));
        }

        private byte TimeLo
        {
            get => (byte) time;
            set => time = (ushort) ((time & 0xFF00) | value);
        }

        private byte TimeHi
        {
            get => (byte) (time >> 8);
            set => time = (ushort) ((time & 0x00FF) | (value << 8));
        }

        private byte Status => (byte)((ctrl != MODE_PAUSED ? STATUS_RUNNING_MASK : 0) | (irq.ClearInterrupt(this) ? STATUS_INTERRUPTING_MASK : 0));

        public SimpleTimer(ushort baseAddress, InterruptLine irq) : base(baseAddress)
        {
            this.irq = irq;

            ctrl = MODE_PAUSED;
            time = 0;
            timeLoLatch = 0;
            timeHiLatch = 0;
        }

        public override void OnCycle(IDeviceInterface bc)
        {
            if (Mode != MODE_PAUSED)
            {
                if (time == 0 || --time == 0)
                {
                    if (InterruptEnabled == INTR_ENABLED)
                        irq.TriggerInterrupt(this);

                    if (Mode == MODE_FREERUN)
                        time = (ushort)((timeHiLatch << 8) | timeLoLatch);
                    else
                        Mode = MODE_PAUSED;
                }
            }

            if (!InRange(bc.Address))
                return;

            ushort reg = Relative(bc.Address);
            if (bc.Rwb)
            {
                if (reg == CTRL)
                    bc.Data = ctrl;
                else if (reg == STATUS)
                    bc.Data = Status;
                else if (reg == TIME_LO)
                {
                    bc.Data = TimeLo;
                    timeHiLatchOut = TimeHi;
                }
                else if (reg == TIME_HI)
                    bc.Data = timeHiLatchOut;
            }
            else
            {
                if (reg == CTRL)
                {
                    ctrl = bc.Data;
                    if (Mode != MODE_ONESHOT && Mode != MODE_FREERUN)
                        Mode = MODE_PAUSED;
                }
                else if (reg == STATUS) // Reloads counter with latches.
                    time = (ushort)((timeHiLatch << 8) | timeLoLatch);
                else if (reg == TIME_LO)
                    TimeLo = timeLoLatch = bc.Data;
                else if (reg == TIME_HI)
                    TimeHi = timeHiLatch = bc.Data;
            }
        }
    }
}
