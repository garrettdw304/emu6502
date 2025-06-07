namespace Emu6502
{
    /// <summary>
    /// A device that allows interrupts to be triggered until acknowledged and then cleared.
    /// The device detects when the interrupt is being handled by the CPU by monitoring the vector pull signal
    /// and address lines. The reset is simply held for one cycle instead of monitoring the CPU because the CPU
    /// will handle reset signals no matter the current state.
    /// </summary>
    public class PushButtonInterruptors : IDevice
    {
        private bool triggerIrq;
        private bool triggerNmi;
        private bool triggerRst;
        private bool clearRst;

        private readonly InterruptLine irq;
        private readonly InterruptLine nmi;
        private readonly InterruptLine rst;

        public PushButtonInterruptors(InterruptLine irq, InterruptLine nmi, InterruptLine rst)
        {
            triggerIrq = triggerNmi = triggerRst = clearRst = false;
            this.irq = irq;
            this.nmi = nmi;
            this.rst = rst;
        }

        public void OnCycle(IDeviceInterface bc)
        {
            if (triggerIrq)
            {
                triggerIrq = false;
                irq.TriggerInterrupt(this);
            }
            else if (bc.Vbp && (bc.Address == 0xFFFE || bc.Address == 0xFFFF))
                irq.ClearInterrupt(this);

            if (triggerNmi)
            {
                triggerNmi = false;
                nmi.TriggerInterrupt(this);
            }
            else if (bc.Vbp && (bc.Address == 0xFFFA || bc.Address == 0xFFFB))
                nmi.ClearInterrupt(this);

            if (triggerRst)
            {
                triggerRst = false;
                clearRst = true;
                rst.TriggerInterrupt(this);
            }
            else if (clearRst)
                rst.ClearInterrupt(this);
        }

        public void TriggerIRQ()
        {
            triggerIrq = true;
        }

        public void TriggerNMI()
        {
            triggerNmi = true;
        }

        public void TriggerRST()
        {
            triggerRst = true;
        }
    }
}
