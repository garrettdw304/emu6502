using System.Text;

namespace Emu6502Gui
{
    internal class CommandArguments
    {
        /// <summary>
        /// -r [fileName]<para/>
        /// If not null, will load the rom with the file provided.
        /// </summary>
        public readonly string? romFilePath;
        /// <summary>
        /// -t [portName]<para/>
        /// If not null, will open a terminal connected to the given port.
        /// </summary>
        public readonly string? termPort;
        /// <summary>
        /// -u [portName]<para/>
        /// If not null, will connect the uart to the given port.
        /// </summary>
        public readonly string? uartPort;
        /// <summary>
        /// -d [serialDrivePath]<para/>
        /// If not null, initializes the serial drive path to something other than serialDrive/.
        /// Since spaces wont work, astrisks will be replaced with spaces bafore this variable is set.
        /// </summary>
        public readonly string? serialDrivePath;
        /// <summary>
        /// -h [hz]<para/>
        /// If not null, sets the clock rate.
        /// </summary>
        public readonly int? hz;
        /// <summary>
        /// -g<para/>
        /// Opens the graphics window.
        /// </summary>
        public readonly bool openGraphics = false;
        /// <summary>
        /// -c<para/>
        /// Begins the emulation automatically upon application start.
        /// </summary>
        public readonly bool autoContinue = false;

        public CommandArguments(string command)
        {
            StringBuilder sb = new StringBuilder();
            string[] args = command.Split();

            for (int i = 1; i < args.Length; i++) // start at 1 to skip command name
            {
                if (args[i].Length == 0)
                    continue;

                if (!args[i].StartsWith('-'))
                {
                    sb.AppendLine($"Value is not preceeded by an option: {args[i]}");
                    continue;
                }

                switch (args[i])
                {
                    case "-r":
                        if (args.Length <= i + 1 || args[i + 1].StartsWith('-'))
                            sb.AppendLine($"-r expects a file name.");
                        else
                            romFilePath = args[++i];
                        break;
                    case "-t":
                        if (args.Length <= i + 1 || args[i + 1].StartsWith('-'))
                            sb.AppendLine($"-t expects a port name.");
                        else
                            termPort = args[++i];
                        break;
                    case "-u":
                        if (args.Length <= i + 1 || args[i + 1].StartsWith('-'))
                            sb.AppendLine($"-u expects a port name.");
                        else
                            uartPort = args[++i];
                        break;
                    case "-d":
                        if (args.Length <= i + 1 || args[i + 1].StartsWith('-'))
                            sb.AppendLine($"-d expects a path.");
                        else
                            serialDrivePath = args[++i];
                        break;
                    case "-h":
                        if (args.Length <= i + 1 || args[i + 1].StartsWith('-'))
                            sb.AppendLine($"-h expects a hz.");
                        else
                        {
                            if (int.TryParse(args[i + 1], out int hz))
                                this.hz = hz;
                            else
                                sb.AppendLine($"Invalid hz:{args[i + 1]}");
                            i++;
                        }
                        break;
                    case "-g":
                        if (args.Length > i + 1 && !args[i + 1].StartsWith('-'))
                        {
                            sb.AppendLine("-g does not expect a value.");
                            i++;
                        }
                        openGraphics = true;
                        break;
                    case "-c":
                        if (args.Length > i + 1 && !args[i + 1].StartsWith('-'))
                        {
                            sb.AppendLine("-c does not expect a value.");
                            i++;
                        }
                        autoContinue = true;
                        break;
                    default:
                        sb.AppendLine($"Unknown option: {args[i]}");
                        break;
                }
            }

            if (sb.Length != 0)
                MessageBox.Show(sb.ToString());
        }
    }
}
