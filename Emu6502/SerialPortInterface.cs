using System.IO.Ports;

namespace Emu6502
{
    public class SerialPortInterface : ISerialInterface
    {
        public readonly SerialPort port;
        private readonly object lck = new object();

        private bool available = false;
        private byte data = 0;

        public SerialPortInterface(SerialPort port)
        {
            this.port = port;
            port.DataReceived += DataReceived;
        }

        public bool Available()
        {
            return available;
        }

        public bool ClearToSend()
        {
            return true;
        }

        public byte Read()
        {
            lock (lck)
            {
                int toRead = port.BytesToRead;
                if (toRead > 0)
                {
                    int val = port.ReadByte();
                    if (val != -1)
                        data = (byte)val;

                    available = toRead > 1;
                }

                return data;
            }
        }

        public void Write(byte value)
        {
            port.Write([value], 0, 1);
        }

        private void DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            lock (lck)
                available = true;
        }
    }
}
