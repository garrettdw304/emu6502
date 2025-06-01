namespace Emu6502
{
    public class SerialInterface : ISerialInterface
    {
        public readonly SerialInterface pairedWith;
        private readonly object lck = new object();

        private bool available = false;
        private byte data = 0;
        
        public SerialInterface()
        {
            pairedWith = new SerialInterface(this);
        }

        private SerialInterface(SerialInterface pairedWith)
        {
            this.pairedWith = pairedWith;
        }

        public static (SerialInterface, SerialInterface) CreatePair()
        {
            SerialInterface portA = new SerialInterface();

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
