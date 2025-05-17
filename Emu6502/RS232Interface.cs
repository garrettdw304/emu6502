namespace Emu6502
{
    public class RS232Interface
    {
        public readonly RS232Interface pairedWith;
        private readonly object lck = new object();

        private bool available = false;
        private byte data = 0;

        public RS232Interface()
        {
            pairedWith = new RS232Interface(this);
        }

        private RS232Interface(RS232Interface pairedWith)
        {
            this.pairedWith = pairedWith;
        }

        public static (RS232Interface, RS232Interface) CreatePair()
        {
            RS232Interface portA = new RS232Interface();

            return (portA, portA.pairedWith);
        }

        public bool Available()
        {
            return available;
        }

        public bool ClearToSend()
        {
            return !pairedWith.available;
        }

        public byte Read()
        {
            lock (lck)
            {
                available = false;
                return data;
            }
        }

        public void Write(byte value)
        {
            lock (pairedWith.lck)
            {
                pairedWith.data = value;
                pairedWith.available = true;
            }
        }
    }
}
