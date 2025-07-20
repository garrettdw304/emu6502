using System.Text;

namespace Emu6502
{
    /// <summary>
    /// Implements a drive which manages a file system and allows access to it over a serial interface. See devices such as the Commadore 1541 etc.
    /// </summary>
    public class SerialDrive : IMachine
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
        private const byte RENAME_COMMAND = (byte)'m';

        public const byte ACK = (byte)'A';
        public const byte NAK = (byte)'N';

        private const int MAX_FILE_NAME_SIZE = 15;

        private readonly SerialInterface port = new SerialInterface();
        private readonly Dictionary<State, Action> stateHandlers;
        private readonly Dictionary<int, CommandStateMachine> commandStateMachines;
        private readonly uint storageSpace;
        private readonly uint storageUsed;

        private State state = State.IDLE;
        private CommandStateMachine command;
        private StringBuilder fileName = new StringBuilder();
        private ushort offset = 0;
        private ushort length = 0;
        private StatusCode statusCode = StatusCode.OK;
        private int count = 0;
        public string? DrivePath { get; private set; }

        public SerialInterface ExternalPort => port.pairedWith;

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
                { State.EXECUTING_COMMAND, ExecutingCommand }
            };

            commandStateMachines = new Dictionary<int, CommandStateMachine>()
            {
                { READ_COMMAND, new ReadSM(this, port) },
                { WRITE_COMMAND, new WriteSM(this, port) },
                { APPEND_COMMAND, new AppendSM(this, port) },
                { DELETE_COMMAND, new DeleteSM(this, port) },
                { INFO_COMMAND, new InfoSM(this, port) },
                { LIST_COMMAND, new ListSM(this, port) },
                { STATUS_COMMAND, new StatusSM(this, port) },
                { RENAME_COMMAND, new RenameSM(this, port) },
            };

            command = commandStateMachines[READ_COMMAND];
        }

        public void Cycle(int hz)
        {
            stateHandlers[state]();
        }

        public void SetDrivePath(string? drivePath)
        {
            if (state != State.IDLE)
                throw new InvalidOperationException("Cannot change drive path when not in IDLE state.");

            if (drivePath != null)
                this.DrivePath = Path.GetFullPath(drivePath);
            else
                this.DrivePath = null;
        }

        private void Idle()
        {
            if (DrivePath == null)
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
            byte cmd = port.Read();

            command = commandStateMachines[cmd] ?? throw new InvalidOperationException(); // TODO: Handle null better
            if (command.NeedsFileName)
            {
                fileName.Clear();
                state = State.RECEIVING_FILE_NAME;
            }
            else if (command.NeedsOffset)
            {
                count = 0;
                state = State.RECEIVING_OFFSET;
            }
            else if (command.NeedsLength)
            {
                count = 0;
                state = State.RECEIVING_LENGTH;
            }
            else
            {
                state = State.ENDING_COMMAND;
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
                if (command.NeedsOffset)
                {
                    count = 0;
                    state = State.RECEIVING_OFFSET;
                }
                else if (command.NeedsLength)
                {
                    count = 0;
                    state = State.RECEIVING_LENGTH;
                }
                else
                {
                    state = State.ENDING_COMMAND;
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

                if (command.NeedsLength)
                {
                    count = 0;
                    state = State.RECEIVING_LENGTH;
                }
                else
                {
                    state = State.ENDING_COMMAND;
                }
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

            state = State.EXECUTING_COMMAND;
            if (command.Start(fileName.ToString(), offset, length))
                state = State.IDLE;
        }

        private void ExecutingCommand()
        {
            if (command.Exe())
                state = State.IDLE;
        }

        /// <summary>
        /// Ensures the file is in the <see cref="DrivePath"/>.
        /// </summary>
        public string? ValidateFileName(string fileName)
        {
            // TODO: Enhance security

            if (DrivePath == null)
                throw new InvalidOperationException();
            
            fileName = Path.Combine(DrivePath, fileName);
            DirectoryInfo? fileDir = new FileInfo(fileName).Directory;
            if (fileDir == null)
                return null;
            DirectoryInfo driveDir = new DirectoryInfo(DrivePath);
            if (fileDir.FullName.TrimEnd('\\').TrimEnd('/') != driveDir.FullName.TrimEnd('\\').TrimEnd('/') || Path.GetFileName(fileName).Length > MAX_FILE_NAME_SIZE)
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
            /// The command's own state machine takes over here.
            /// </summary>
            EXECUTING_COMMAND,
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
