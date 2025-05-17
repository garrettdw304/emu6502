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
        private readonly Dictionary<State, Action> stateHandlers;
        private readonly uint storageSpace;
        private readonly uint storageUsed;

        private State state = State.IDLE;
        private byte command = 0;
        private StringBuilder fileName = new StringBuilder();
        private ushort offset = 0;
        private ushort length = 0;
        private byte[] data = new byte[0];
        private StatusCode statusCode = StatusCode.OK;
        private int count = 0;
        public RS232Interface ExternalPort => port.pairedWith;
        private string? drivePath;

        public SerialDrive(uint storageSpace, string? drivePath = null)
        {
            SetDrivePath(drivePath);
            this.storageSpace = storageSpace;
            // TODO: Calc based on files in the drivePath. Add 16 bytes per file name, 4 bytes per file date, * bytes per file contents.
            storageUsed = storageSpace;

            stateHandlers = new Dictionary<State, Action>()
            {
                { State.IDLE, Idle },
                { State.RECEIVING_COMMAND, ReceivingCommand },
                { State.RECEIVING_FILE_NAME, ReceivingFileName },
                { State.RECEIVING_OFFSET, ReceivingOffset },
                { State.RECEIVING_LENGTH, ReceivingLength },
                { State.ENDING_COMMAND, EndingCommand },
                { State.SENDING_LENGTH, SendingLength },
                { State.SENDING_DATA, SendingData },
                { State.RECEIVING_DATA, ReceivingData }
            };
        }

        public void OnCycle(int hz)
        {
            stateHandlers[state]();
        }

        public void SetDrivePath(string? drivePath)
        {
            if (state != State.IDLE)
                throw new InvalidOperationException("Cannot change drive path when not in IDLE state.");

            if (drivePath != null)
                this.drivePath = Path.GetFullPath(drivePath);
            else
                this.drivePath = null;
        }

        private void Idle()
        {
            if (drivePath == null)
                return;

            if (!port.Available())
                return;

            if (port.Read() == START_COMMAND)
                state = State.RECEIVING_COMMAND;
        }

        private void ReceivingCommand()
        {
            if (!port.Available())
                return;
            command = port.Read();

            switch (command)
            {
                case READ_COMMAND:
                    fileName.Clear();
                    state = State.RECEIVING_FILE_NAME;
                    break;
                case WRITE_COMMAND:
                    fileName.Clear();
                    state = State.RECEIVING_FILE_NAME;
                    break;
                case APPEND_COMMAND:
                    fileName.Clear();
                    state = State.RECEIVING_FILE_NAME;
                    break;
                case DELETE_COMMAND:
                    fileName.Clear();
                    state = State.RECEIVING_FILE_NAME;
                    break;
                case INFO_COMMAND:
                    fileName.Clear();
                    state = State.RECEIVING_FILE_NAME;
                    break;
                case LIST_COMMAND:
                    // TODO: Implement list command
                    throw new NotImplementedException();
                case STATUS_COMMAND:
                    state = State.ENDING_COMMAND;
                    break;
                default:
                    throw new InvalidOperationException(); // TODO: Handle
            }
        }

        private void ReceivingFileName()
        {
            if (!port.Available())
                return;
            byte input = port.Read();

            if (input != 0)
                fileName.Append((char)input);
            else
            {
                switch (command)
                {
                    case READ_COMMAND:
                        count = 0;
                        state = State.RECEIVING_OFFSET;
                        break;
                    case WRITE_COMMAND:
                        count = 0;
                        state = State.RECEIVING_LENGTH;
                        break;
                    case APPEND_COMMAND:
                        count = 0;
                        state = State.RECEIVING_LENGTH;
                        break;
                    case DELETE_COMMAND:
                        state = State.ENDING_COMMAND;
                        break;
                    case INFO_COMMAND:
                        state = State.ENDING_COMMAND;
                        break;
                    default:
                        throw new InvalidOperationException();
                }
            }
        }

        private void ReceivingOffset()
        {
            if (!port.Available())
                return;
            byte input = port.Read();

            if (count == 0)
            {
                count++;
                offset = input;
            }
            else if (count == 1)
            {
                offset |= (ushort)(input << 8);
                count = 0;
                state = State.RECEIVING_LENGTH;
            }
            else
                throw new InvalidOperationException();
        }

        private void ReceivingLength()
        {
            if (!port.Available())
                return;
            byte input = port.Read();

            if (count == 0)
            {
                count++;
                length = input;
            }
            else if (count == 1)
            {
                length |= (ushort)(input << 8);
                state = State.ENDING_COMMAND;
            }
            else
                throw new InvalidOperationException();
        }

        private void EndingCommand()
        {
            if (!port.Available())
                return;
            byte input = port.Read();

            if (input != END_COMMAND)
                return;

            string? name;
            switch (command)
            {
                case READ_COMMAND:
                    // Read file and store into data array
                    name = ValidateFileName(fileName.ToString());
                    if (name == null)
                    {
                        port.Write(NAK); // TODO: Port may not be clear to send (fix by adding new states for sending ACK/NAK and wait in those states until clear to send)
                        statusCode = StatusCode.FILE_NAME_INVALID;
                        state = State.IDLE;
                    }
                    else if (!File.Exists(name))
                    {
                        port.Write(NAK);
                        statusCode = StatusCode.FILE_NOT_FOUND;
                        state = State.IDLE;
                    }
                    else
                    {
                        port.Write(ACK);
                        statusCode = StatusCode.OK;
                        count = 0;
                        data = File.ReadAllBytes(name);
                        if (data.Length < length) // TODO: Take into account offset (also use offset elsewhere, we are reading offset but we are never using it)
                            length = (ushort)data.Length;
                        state = State.SENDING_LENGTH;
                    }
                    break;
                case WRITE_COMMAND:
                    name = ValidateFileName(fileName.ToString());
                    if (name == null)
                    {
                        port.Write(NAK);
                        statusCode = StatusCode.FILE_NAME_INVALID;
                        state = State.IDLE;
                    }
                    else
                    {
                        fileName.Clear().Append(name);
                        port.Write(ACK);
                        statusCode = StatusCode.OK;
                        count = 0;
                        data = new byte[length];
                        if (length > 0)
                            state = State.RECEIVING_DATA;
                        else
                        {
                            File.WriteAllBytes(fileName.ToString(), data);
                            state = State.IDLE;
                        }
                    }
                    break;
                case APPEND_COMMAND:
                    name = ValidateFileName(fileName.ToString());
                    if (name == null)
                    {
                        port.Write(NAK);
                        statusCode = StatusCode.FILE_NAME_INVALID;
                        state = State.IDLE;
                    }
                    else if (!File.Exists(name))
                    {
                        port.Write(NAK);
                        statusCode = StatusCode.FILE_NOT_FOUND;
                        state = State.IDLE;
                    }
                    else
                    {
                        fileName.Clear().Append(name);
                        port.Write(ACK);
                        statusCode = StatusCode.OK;
                        count = 0;
                        data = new byte[length];
                        if (length > 0)
                            state = State.RECEIVING_DATA;
                        else
                        {
                            using (var stream = new FileStream(fileName.ToString(), FileMode.Append))
                                stream.Write(data, 0, length);
                            state = State.IDLE;
                        }
                    }
                    break;
                case DELETE_COMMAND:
                    name = ValidateFileName(fileName.ToString());
                    if (name == null)
                    {
                        port.Write(NAK);
                        statusCode = StatusCode.FILE_NAME_INVALID;
                    }
                    else if (!File.Exists(name))
                    {
                        port.Write(NAK);
                        statusCode = StatusCode.FILE_NOT_FOUND;
                    }
                    else
                    {
                        port.Write(ACK);
                        statusCode = StatusCode.OK;
                        File.Delete(name);
                    }
                    state = State.IDLE;
                    break;
                case INFO_COMMAND:
                    name = ValidateFileName(fileName.ToString());
                    if (name == null)
                    {
                        port.Write(NAK);
                        statusCode = StatusCode.FILE_NAME_INVALID;
                        state = State.IDLE;
                    }
                    else if (!File.Exists(name))
                    {
                        port.Write(NAK);
                        statusCode = StatusCode.FILE_NOT_FOUND;
                        state = State.IDLE;
                    }
                    else
                    {
                        port.Write(ACK);
                        statusCode = StatusCode.OK;
                        count = 0;
                        length = 6;
                        MemoryStream ms = new MemoryStream(length);
                        ushort len = (ushort)new FileInfo(name).Length;
                        ms.WriteByte((byte)len);
                        ms.WriteByte((byte)(len >> 8));

                        ulong date = (ulong)((DateTimeOffset)File.GetCreationTimeUtc(name)).ToUnixTimeSeconds();
                        ms.WriteByte((byte)date);
                        ms.WriteByte((byte)(date >> 8));
                        ms.WriteByte((byte)(date >> 16));
                        ms.WriteByte((byte)(date >> 24));

                        data = ms.ToArray();
                        state = State.SENDING_DATA;
                    }
                    break;
                case LIST_COMMAND:
                    // TODO: Implement list
                    throw new NotImplementedException();
                case STATUS_COMMAND:
                    throw new NotImplementedException();
                    // TODO: Serialize status into data array and set length to length of data array
                    state = State.SENDING_DATA;
                    break;
                default:
                    throw new InvalidOperationException();
            }
        }

        private void SendingLength()
        {
            if (!port.ClearToSend())
                return;

            if (count == 0)
            {
                count++;
                port.Write((byte)length);
            }
            else if (count == 1)
            {
                count = 0;
                port.Write((byte)(length >> 8));

                if (length == 0)
                    state = State.IDLE;
                else
                    state = State.SENDING_DATA;
            }
            else
                throw new InvalidOperationException();
        }

        private void SendingData()
        {
            if (!port.ClearToSend())
                return;

            port.Write(data[count++]);
            if (count == length)
                state = State.IDLE;
        }

        private void ReceivingData()
        {
            if (!port.Available())
                return;

            data[count++] = port.Read();
            if (count == length)
            {
                if (command == WRITE_COMMAND)
                {
                    File.WriteAllBytes(fileName.ToString(), data);
                }
                else if (command == APPEND_COMMAND)
                {
                    using (var stream = new FileStream(fileName.ToString(), FileMode.Append))
                        stream.Write(data, 0, length);
                }
                else
                    throw new InvalidOperationException();

                    state = State.IDLE;
            }
        }

        /// <summary>
        /// Ensures the file is in the <see cref="drivePath"/>.
        /// </summary>
        private string? ValidateFileName(string fileName)
        {
            // TODO: Enhance security

            if (drivePath == null)
                throw new InvalidOperationException();
            
            fileName = Path.Combine(drivePath, fileName);
            DirectoryInfo? fileDir = new FileInfo(fileName).Directory;
            if (fileDir == null)
                return null;
            DirectoryInfo driveDir = new DirectoryInfo(drivePath);
            if (fileDir.FullName.TrimEnd('\\').TrimEnd('/') != driveDir.FullName.TrimEnd('\\').TrimEnd('/') || Path.GetFileName(fileName).Length > 15)
                return null;
            else
                return fileName;
        }

        private enum State
        {
            /// <summary>
            /// Waiting for a <see cref="START_COMMAND"/>. Continues to <see cref="RECEIVING_COMMAND"/>.
            /// </summary>
            IDLE,
            /// <summary>
            /// Waiting for a command byte.
            /// </summary>
            RECEIVING_COMMAND,
            /// <summary>
            /// Reading a file name. Continues to next state on \0. Next state is determined by the command.
            /// </summary>
            RECEIVING_FILE_NAME,
            /// <summary>
            /// Reading an offset. Continues to next state when count = 2. Next state is always <see cref="RECEIVING_LENGTH"/>.
            /// </summary>
            RECEIVING_OFFSET,
            /// <summary>
            /// Reading a length. Continues to next state when count = 2. Next state is always <see cref="ENDING_COMMAND"/>.
            /// </summary>
            RECEIVING_LENGTH,
            /// <summary>
            /// Waiting for a <see cref="END_COMMAND"/>. Next state is determined by the command.
            /// Writes either <see cref="ACK"/> or <see cref="NAK"/> based on any errors and if <see cref="NAK"/>, returns to <see cref="IDLE"/>.
            /// </summary>
            ENDING_COMMAND,
            /// <summary>
            /// Sending a length, either data.length or length, whichever is lower. If data.length is lower, set length to data.length. Continues to next state when count = 2. Next state is determined by the command.
            /// </summary>
            SENDING_LENGTH,
            /// <summary>
            /// Sending bytes from the data array. Continues to next state when count = length. Next state is always <see cref="IDLE"/>.
            /// </summary>
            SENDING_DATA,
            /// <summary>
            /// Receiving bytes of a file. Continues to next state when count = length. Next state is always <see cref="IDLE"/>.
            /// </summary>
            RECEIVING_DATA
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
