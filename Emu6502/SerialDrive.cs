using System.Text;

namespace Emu6502
{
    /// <summary>
    /// Implements a drive which manages a file system and allows access to it over a serial interface. See devices such as the Commadore 1541 etc.
    /// </summary>
    public class SerialDrive
    {
        public RS232Interface ExternalPort => port.pairedWith;

        byte START_COMMAND = (byte)'>';
        byte END_COMMAND = (byte)'<';

        private readonly RS232Interface port = new RS232Interface();

        private State currentState = State.idle;
        private List<byte> currentCommand = new List<byte>();

        public SerialDrive()
        {

        }

        public void OnCycle(int hz)
        {
            if (!port.Available())
                return;

            ProgressState(port.Read());
        }

        private void ProgressState(byte input)
        {
            if (currentState == State.idle)
            {
                if (input != START_COMMAND)
                    return;

                currentState = State.receivingCommand;
            }
            else if (currentState == State.receivingCommand)
            {
                if (input != END_COMMAND)
                {
                    ValidifyCommandInput(input);
                    currentCommand.Add(input);
                }
                else
                    HandleCommand();
            }
        }

        private bool ValidifyCommandInput(byte input)
        {
            return input > 32 && input < 127;
        }

        private void HandleCommand()
        {
            string command = currentCommand.ToString();

            // Reads from an existing file.
            if (command == "read")
            {

            }
            // Creates a new file or overwrites the existing file.
            else if (command == "write")
            {
                
            }
            // Appends to an existing file.
            else if (command == "append")
            {

            }
        }

        private enum State
        {
            // Waiting for the next command.
            idle,
            // Receiving a command.
            receivingCommand,
            
        }
    }
}
