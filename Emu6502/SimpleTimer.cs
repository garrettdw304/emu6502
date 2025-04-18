namespace Emu6502
{
    public class SimpleTimer : Device
    {
        protected override int Length => 4;

        private const int MODE = 0;
        private const int STATUS = 1;
        private const int TIME_LO = 2;
        private const int TIME_HI = 3;

        private const int MODE_PAUSED = 0;
        /// <summary>
        /// When timer expires an interrupt occurs and the timer IS NOT reset.
        /// </summary>
        private const int MODE_ONESHOT = 1;
        /// <summary>
        /// When the timer expires an interrupt occurs and the timer IS reset.
        /// </summary>
        private const int MODE_FREERUN = 2;

        private readonly InterruptLine irq;

        private byte mode;
        private ushort time;

        // Used to reset the timer in freerun mode.
        private byte timeLoLatch;
        private byte timeHiLatch;

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

        private byte Status => (byte)((mode != MODE_PAUSED ? 0b10 : 0) | (irq.ClearInterrupt(this) ? 0b01 : 0));

        public SimpleTimer(ushort baseAddress, InterruptLine irq) : base(baseAddress)
        {
            this.irq = irq;

            mode = MODE_PAUSED;
            time = 0;
            timeLoLatch = 0;
            timeHiLatch = 0;
        }

        public override void OnCycle(IDeviceInterface bc)
        {
            if (mode != MODE_PAUSED)
            {
                if (time == 0 || --time == 0)
                {
                    irq.TriggerInterrupt(this);

                    if (mode == MODE_FREERUN)
                        time = (ushort)((timeHiLatch << 8) | timeLoLatch);
                    else
                        mode = MODE_PAUSED;
                }
            }

            if (!InRange(bc.Address))
                return;

            ushort reg = Relative(bc.Address);
            if (bc.Rwb)
            {
                if (reg == MODE)
                    bc.Data = mode;
                else if (reg == STATUS)
                    bc.Data = Status;
                else if (reg == TIME_LO)
                    bc.Data = TimeLo;
                else if (reg == TIME_HI)
                    bc.Data = TimeHi;
            }
            else
            {
                if (reg == MODE)
                {
                    mode = bc.Data;
                    if (mode != MODE_ONESHOT && mode != MODE_FREERUN)
                        mode = MODE_PAUSED;
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
