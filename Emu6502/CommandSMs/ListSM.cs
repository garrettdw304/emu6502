using System.Text;

namespace Emu6502
{
    internal class ListSM(SerialDrive drive, RS232Interface port) : CommandStateMachine(drive, port)
    {
        public override bool NeedsFileName => false;
        public override bool NeedsOffset => true;
        public override bool NeedsLength => false;

        private string[] files = [];
        private int count = 0;
        private bool waitingForACK = false;
        private bool initialFileACK = false;

        private byte[] name = [];
        private int ncount = 0;

        public override bool Start(string fileName, ushort offset, ushort length)
        {
            string drivePath = drive.DrivePath ?? throw new InvalidOperationException(); // TODO: Handle better
            files = Directory.GetFiles(drivePath);
            for (int i = 0; i < files.Length; i++)
                files[i] = Path.GetFileName(files[i]);
            count = offset;
            waitingForACK = true;
            initialFileACK = true;

            port.Write(SerialDrive.ACK);
            // statusCode = StatusCode.OK;
            return false;
        }

        public override bool Exe()
        {
            if (initialFileACK)
                return InitialFileACK();
            if (waitingForACK)
                return Waiting();
            else
                return Writing();
        }

        private bool InitialFileACK()
        {
            if (!port.ClearToSend())
                return false;
            initialFileACK = false;
            
            if (count >= files.Length)
            {
                port.Write(SerialDrive.NAK);
                return true;
            }
            else
            {
                port.Write(SerialDrive.ACK);
                return false;
            }
        }

        private bool Waiting()
        {
            if (!port.Available())
                return false;
            else if (port.Read() != SerialDrive.ACK)
                return true;
            else
            {
                waitingForACK = false;
                ncount = 0;
                MemoryStream ms = new MemoryStream();
                ms.Write(Encoding.ASCII.GetBytes(files[count++]));
                ms.WriteByte(0); // Terminate string
                ms.WriteByte(count == files.Length ? SerialDrive.NAK : SerialDrive.ACK); // Indicate weather or not there is another file after this one.
                name = ms.ToArray();
                return false;
            }
        }

        private bool Writing()
        {
            if (!port.ClearToSend())
                return false;

            port.Write(name[ncount++]);
            if (ncount < name.Length)
                return false;
            if (count == files.Length)
                return true;

            waitingForACK = true;
            return false;
        }
    }
}
