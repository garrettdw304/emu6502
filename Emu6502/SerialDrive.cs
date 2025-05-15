using System.Text;

namespace Emu6502
{
    /// <summary>
    /// Implements a drive which manages a file system and allows access to it over a serial interface. See devices such as the Commadore 1541 etc.
    /// </summary>
    public class SerialDrive
    {
        private const byte START_COMMAND = (byte)'>';
        private const byte END_COMMAND = (byte)'<';

        private const byte READ_COMMAND = (byte)'r';
        private const byte WRITE_COMMAND = (byte)'w';
        private const byte APPEND_COMMAND = (byte)'a';
        private const byte DELETE_COMMAND = (byte)'d';
        private const byte INFO_COMMAND = (byte)'i';
        private const byte LIST_COMMAND = (byte)'l';
        private const byte STATUS_COMMAND = (byte)'s';

        private const byte ACK = (byte)'A';
        private const byte NAK = (byte)'N';

        private readonly RS232Interface port = new RS232Interface();

        private State state = State.idle;
        private byte command;
        private StringBuilder fileName;
        private ushort offset;
        private ushort length;
        private byte[] data;
        private StatusCode statusCode;
        private int count;
        public RS232Interface ExternalPort => port.pairedWith;

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
            if (state == State.idle)
            {
                if (input == START_COMMAND)
                    state = State.receivingCommand;
            }
            else if (state == State.receivingCommand)
            {
                switch (input)
                {
                    case READ_COMMAND:
                        command = input;
                        state = State.receivingFileName;
                        break;
                    case WRITE_COMMAND:
                        command = input;
                        state = State.receivingFileName;
                        break;
                    case APPEND_COMMAND:
                        command = input;
                        state = State.receivingFileName;
                        break;
                    case DELETE_COMMAND:
                        command = input;
                        state = State.receivingFileName;
                        break;
                    case INFO_COMMAND:
                        command = input;
                        state = State.receivingFileName;
                        break;
                    case LIST_COMMAND:
                        // TODO: Implement list command
                        throw new NotImplementedException();
                    case STATUS_COMMAND:
                        command = input;
                        state = State.endingCommand;
                        break;
                    default:
                        break;
                }
            }
            else if (state == State.receivingFileName)
            {
                if (input != 0)
                    fileName.Append((char)input);
                else
                {
                    switch (input)
                    {
                        case READ_COMMAND:
                            command = input;
                            count = 0;
                            state = State.receivingOffset;
                            break;
                        case WRITE_COMMAND:
                            command = input;
                            state = State.receivingLength;
                            break;
                        case APPEND_COMMAND:
                            command = input;
                            state = State.receivingLength;
                            break;
                        case DELETE_COMMAND:
                            command = input;
                            state = State.endingCommand;
                            break;
                        case INFO_COMMAND:
                            command = input;
                            state = State.endingCommand;
                            break;
                        default:
                            break;
                    }
                }
            }
            else if (state == State.receivingOffset)
            {
                if (count == 0)
                {
                    count++;
                    offset = input;
                }
                else if (count == 1)
                {
                    offset |= (ushort)(input << 8);
                    count = 0;
                    state = State.receivingLength;
                }
                else
                    throw new InvalidOperationException();
            }
            else if (state == State.receivingLength)
            {
                if (count == 0)
                {
                    count++;
                    length = input;
                }
                else if (count == 1)
                {
                    length |= (ushort)(input << 8);
                    state = State.endingCommand;
                }
                else
                    throw new InvalidOperationException();
            }
            else if (state == State.endingCommand)
            {
                if (input != END_COMMAND)
                    return;

                string name;
                switch (command)
                {
                    case READ_COMMAND:
                        // Read file and store into data array
                        name = fileName.ToString();
                        if (!File.Exists(name))
                        {
                            port.Write(NAK);
                            state = State.idle;
                        }
                        else
                        {
                            port.Write(ACK);
                            count = 0;
                            data = File.ReadAllBytes(name);
                            state = State.sendingLength;
                        }
                        break;
                    case WRITE_COMMAND:
                        port.Write(ACK);
                        count = 0;
                        data = new byte[length];
                        state = State.receivingData;
                        break;
                    case APPEND_COMMAND:
                        name = fileName.ToString();
                        if (!File.Exists(name))
                        {
                            port.Write(NAK);
                            state = State.idle;
                        }
                        else
                        {
                            port.Write(ACK);
                            count = 0;
                            data = new byte[length];
                            state = State.receivingData;
                        }
                        break;
                    case DELETE_COMMAND:
                        name = fileName.ToString();
                        if (!File.Exists(name))
                            port.Write(NAK);
                        else
                        {
                            port.Write(ACK);
                            File.Delete(name);
                        }
                        state = State.idle;
                        break;
                    case INFO_COMMAND:
                        name = fileName.ToString();
                        if (!File.Exists(name))
                        {
                            port.Write(NAK);
                            state = State.idle;
                        }
                        else
                        {
                            count = 0;
                            // TODO: Serialize info into data array and set length to length of data array
                            state = State.sendingData;
                        }
                        break;
                    case LIST_COMMAND:
                        // TODO: Implement list command
                        throw new NotImplementedException();
                    case STATUS_COMMAND:
                        // TODO: Serialize status into data array and set length to length of data array
                        state = State.sendingData;
                        break;
                    default:
                        break;
                }
            }
            else if (state == State.sendingLength)
            {
                if (count == 0)
                {
                    count++;
                    if (data.Length < length)
                        port.Write((byte)data.Length);
                    else
                        port.Write((byte)length);
                }
                else if (count == 1)
                {
                    count = 0;
                    if (data.Length < length)
                        port.Write((byte)(data.Length >> 8));
                    else
                        port.Write((byte)(length >> 8));
                    state = State.sendingData;
                }
                else
                    throw new InvalidOperationException();
            }
            else if (state == State.sendingData)
            {

            }
            else if (state == State.receivingData)
            {

            }
        }

        private enum State
        {
            /// <summary>
            /// Waiting for a <see cref="START_COMMAND"/>. Continues to <see cref="receivingCommand"/>.
            /// </summary>
            idle,
            /// <summary>
            /// Waiting for a command byte.
            /// </summary>
            receivingCommand,
            /// <summary>
            /// Reading a file name. Continues to next state on \0. Next state is determined by the command.
            /// </summary>
            receivingFileName,
            /// <summary>
            /// Reading an offset. Continues to next state when count = 2. Next state is always <see cref="receivingLength"/>.
            /// </summary>
            receivingOffset,
            /// <summary>
            /// Reading a length. Continues to next state when count = 2. Next state is always <see cref="endingCommand"/>.
            /// </summary>
            receivingLength,
            /// <summary>
            /// Waiting for a <see cref="END_COMMAND"/>. Next state is determined by the command.
            /// Writes either ACK or NAK based on any errors and if NAK, returns to <see cref="idle"/>.
            /// </summary>
            endingCommand,
            /// <summary>
            /// Sending a length. Continues to next state when count = 2. Next state is determined by the command.
            /// </summary>
            sendingLength,
            /// <summary>
            /// Sending bytes from the data array. Continues to next state when count = length or count = data.length. Next state is always <see cref="idle"/>.
            /// </summary>
            sendingData,
            /// <summary>
            /// Receiving bytes of a file. Continues to next state when count = length. Next state is always <see cref="idle"/>.
            /// </summary>
            receivingData
        }

        private enum StatusCode
        {
            /// <summary>
            /// No problems.
            /// </summary>
            OK,
            /// <summary>
            /// The command does not exist.
            /// </summary>
            COMMAND_NOT_RECOGNIZED,
            /// <summary>
            /// A file with the provided file name was not found.
            /// </summary>
            FILE_NOT_FOUND,
            /// <summary>
            /// There is not enough space.
            /// </summary>
            NOT_ENOUGH_SPACE,
            /// <summary>
            /// The file name provided cannot be used to create a new file because it is invalid.
            /// </summary>
            FILE_NAME_INVALID
        }
    }
}
