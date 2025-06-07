using Emu6502;
using System.Diagnostics;
using System.IO.Ports;
using System.Text;

namespace Emu6502Gui
{
    public partial class Form1 : Form
    {
        private const ushort ROM_BASE_ADDRESS = 0xC000;
        private const int ROM_SIZE = 0x4000;
        private const ushort RAM_BASE_ADDRESS = 0x0000;
        private const int RAM_SIZE = 0x8000;
        private const ushort UART_BASE_ADDRESS = 0xB000;
        private const ushort DRIVE_BASE_ADDRESS = 0xB100;
        private const ushort TIMER_BASE_ADDRESS = 0xB200;
        private const ushort GRAPHICS_CHIP_BASE_ADDRESS = 0xB300;

        private readonly CommandArguments arguments;

        private readonly Emulation emu;
        private readonly Cpu cpu;
        private readonly SerialDrive serialDrive;

        private readonly Ram ram;
        private readonly Uart uart;
        private readonly Uart driveUart;
        private readonly SimpleTimer timer;
        private readonly GraphicsChip graphicsChip;
        private GraphicsChipOutput? graphicsChipForm;
        private readonly Bitmap graphicsChipBuffer;
        private readonly Rom rom;
        private readonly PushButtonInterruptors pbi;

        private OpenTermDialog? termDialog;

        /// <summary>
        /// First row displayed in mem display.
        /// </summary>
        private int memDisplayRow = 0;

        public Form1()
        {
            InitializeComponent();

            // Create emulation
            emu = new Emulation();
            emu.OnStop += Emu_OnStop;

            // Create emulated cpu
            cpu = new Cpu();
            emu.AddMachine(cpu);

            // Create other emulated machines
            const string path = "serialDrive/";
            Directory.CreateDirectory(path);
            serialDrive = new SerialDrive(1_440_000, path);
            emu.AddMachine(serialDrive);

            // Create devices
            ram = new Ram(RAM_BASE_ADDRESS, RAM_SIZE);
            uart = new Uart(UART_BASE_ADDRESS);
            driveUart = new Uart(DRIVE_BASE_ADDRESS, serialDrive.ExternalPort);
            timer = new SimpleTimer(TIMER_BASE_ADDRESS, cpu.irq);
            graphicsChipBuffer = new Bitmap(320, 240);
            graphicsChip = new GraphicsChip(GRAPHICS_CHIP_BASE_ADDRESS, Graphics.FromImage(graphicsChipBuffer));
            rom = new Rom(ROM_BASE_ADDRESS, ROM_SIZE);
            // Devices not in memory map
            pbi = new PushButtonInterruptors(cpu.irq, cpu.nmi, cpu.rst);

            // Connect devices
            cpu.bc.AddDevice(uart);
            cpu.bc.AddDevice(driveUart);
            cpu.bc.AddDevice(ram);
            cpu.bc.AddDevice(timer);
            cpu.bc.AddDevice(graphicsChip);
            cpu.bc.AddDevice(rom);
            cpu.bc.AddDevice(pbi);

            arguments = new CommandArguments(Environment.CommandLine);
            if (arguments.romFilePath != null)
            {
                if (!File.Exists(arguments.romFilePath))
                    MessageBox.Show("ROM file does not exist.");
                else
                    rom.Program(arguments.romFilePath);
            }
            if (arguments.termPort != null) Process.Start($"plink.exe", $"-serial {arguments.termPort} -sercfg 9600,8,n,1,N");
            if (arguments.uartPort != null)
            {
                uartDropdown.Items.AddRange(["None", arguments.uartPort]);
                uartDropdown.SelectedIndex = 1;
            }
            if (arguments.serialDrivePath != null) serialDrive.SetDrivePath(arguments.serialDrivePath);
            if (arguments.hz != null) clockRateTB.Text = arguments.hz.ToString();
            if (arguments.openGraphics) GraphicsBtn_Click(null, null);
            if (arguments.autoContinue) ContinueBtn_Click(null, null);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            MouseWheel += Form1_MouseWheel;

            outputLbl.Text = DateTime.Now.ToShortTimeString() + " Program Opened";
        }

        private void ContinueBtn_Click(object sender, EventArgs e)
        {
            if (emu.IsRunning)
            {
                continueBtn.Text = "Stopping";
                continueBtn.Enabled = false;
                emu.Stop(false);
            }
            else
            {
                if (!int.TryParse(clockRateTB.Text, out int hz))
                {
                    clockRateTB.Text = "";
                    return;
                }

                ushort stopAt = 0;
                if (stopAtCB.Checked && !TryParseUShort(stopAtAddrTB.Text, out stopAt))
                {
                    stopAtAddrTB.Text = "";
                    return;
                }

                continueBtn.Text = "Stop";
                cycleBtn.Enabled = false;
                clockRateTB.Enabled = false;
                loadRomBtn.Enabled = false;
                uartDropdown.Enabled = false;
                stopAtCB.Enabled = false;
                stopAtAddrTB.Enabled = false;
                stepCB.Enabled = false;

                if (stepCB.Checked && stopAtCB.Checked)
                    emu.Continue(hz, () => cpu.step == Cpu.NEXT_INSTR_STEP || cpu.step == Cpu.PIPELINED_FETCH_STEP || cpu.pc == stopAt);
                else if (stepCB.Checked)
                    emu.Continue(hz, () => cpu.step == Cpu.NEXT_INSTR_STEP || cpu.step == Cpu.PIPELINED_FETCH_STEP);
                else if (stopAtCB.Checked)
                    emu.Continue(hz, () => cpu.pc == stopAt);
                else
                    emu.Continue(hz);
            }
        }

        private void Emu_OnStop()
        {
            Invoke(() =>
            {
                continueBtn.Text = "Continue";
                continueBtn.Enabled = true;
                cycleBtn.Enabled = true;
                clockRateTB.Enabled = true;
                loadRomBtn.Enabled = true;
                uartDropdown.Enabled = true;
                stopAtCB.Enabled = true;
                stopAtAddrTB.Enabled = true;
                stepCB.Enabled = true;
            });
        }

        private void CycleBtn_Click(object sender, EventArgs e)
        {
            if (emu.IsRunning)
                return;

            if (!int.TryParse(clockRateTB.Text, out int hz))
            {
                clockRateTB.Text = "";
                return;
            }

            emu.Cycle(hz);
        }

        private void LoadRomBtn_Click(object sender, EventArgs e)
        {
            DialogResult result = romFileDialog.ShowDialog();
            if (result != DialogResult.OK)
                return;

            if (!File.Exists(romFileDialog.FileName))
                return;

            rom.Program(romFileDialog.FileName);
            outputLbl.Text = DateTime.Now.ToShortTimeString() + " ROM programmed";
        }

        private void StatusTimer_Tick(object sender, EventArgs e)
        {
            emu.PauseState();
            cpuStatusLbl.Text = cpu.ToString()
                + "\nNextInstr: " + cpu.NameOfInstruction((byte)PeekMem(cpu.pc))
                + "\nCycles: " + emu.CycleCount
                + "\nIRQ Line: " + (cpu.irq.Triggered ? "HIGH" : "LOW")
                + "\nNMI Line: " + (cpu.nmi.Triggered ? "HIGH" : "LOW")
                + "\nRST Line: " + (cpu.rst.Triggered ? "HIGH" : "LOW");
            UpdateMemDisplay();
            UpdateStackDisplay();
            UpdateArgStackDisplay();
            emu.ResumeState();
        }

        private void Form1_MouseWheel(object? sender, MouseEventArgs e)
        {
            if (e.Delta > 0)
            {
                memDisplayRow -= 1;
                if (memDisplayRow < 0)
                    memDisplayRow = 0;
            }
            if (e.Delta < 0)
            {
                memDisplayRow += 1;
                if (memDisplayRow > 65536 / 16 - 16)
                    memDisplayRow = 65536 / 16 - 16;
            }
        }

        private void ScrollToBtn_Click(object sender, EventArgs e)
        {
            if (!TryParseUShort(scrollToTB.Text, out ushort value))
                scrollToTB.Text = "0x0000";
            else
                memDisplayRow = value / 16;
        }

        private void UpdateMemDisplay()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("      0  1  2  3  4  5  6  7  8  9  A  B  C  D  E  F");

            for (int i = memDisplayRow * 16; i < memDisplayRow * 16 + 256; i += 16)
            {
                sb.Append(((ushort)i).ToString("X4")).Append(' ');

                for (int j = 0; j < 16; j++)
                {
                    int value = PeekMem((ushort)(i + j));
                    if (value == -1)
                        sb.Append("XX ");
                    else
                        sb.Append(value.ToString("X2")).Append(' ');
                }

                sb.AppendLine();
            }

            memDisplay.Text = sb.ToString();
        }

        private void UpdateStackDisplay()
        {
            StringBuilder sb = new StringBuilder();

            int highestAddress;
            int lowestAddress;

            if (cpu.s > 0xF0) // we want to move the SP arrow around
            {

                lowestAddress = 0xF0;
                highestAddress = 0xFF;
            }
            else // we want to move the memory range around
            {
                lowestAddress = cpu.s - (cpu.s == 0 ? 0 : 1);
                highestAddress = lowestAddress + 0xF;
            }

            int i = lowestAddress;
            if (cpu.s != 0)
            {
                // Print the first line with an address indicator
                sb.AppendLine($"{PeekMem(0x100 | lowestAddress).ToString().PadLeft(3, '0')} <-{lowestAddress.ToString("X2")}");
                i++;

                // Print lines before the address that the sp points to
                for (; i < cpu.s; i++)
                    sb.AppendLine(PeekMem(0x100 | i).ToString().PadLeft(3, '0'));

                // Print the line that sp points to
                sb.AppendLine($"{PeekMem(0x100 | i++).ToString().PadLeft(3, '0')} <-{cpu.s.ToString("X2")} S");
            }
            else
            {
                sb.AppendLine($"{PeekMem(0x100 | cpu.s).ToString().PadLeft(3, '0')} <-00 S");
                i++;
            }

            // If sp points to the last line, dont print any more lines, else finish up
            if (cpu.s != highestAddress)
            {
                // Print remaining middle lines
                for (; i < highestAddress; i++)
                    sb.AppendLine(PeekMem(0x100 | i).ToString().PadLeft(3, '0'));

                // Print the last line with an address indicator
                sb.AppendLine($"{PeekMem(0x100 | highestAddress).ToString().PadLeft(3, '0')} <-{highestAddress.ToString("X2")}");
            }

            stackLbl.Text = sb.ToString();
        }

        private void UpdateArgStackDisplay()
        {
            StringBuilder sb = new StringBuilder();

            int highestAddress;
            int lowestAddress;
            int sp = PeekMem(0) | (PeekMem(1) << 8);

            if (sp > 0xFFF0) // we want to move the SP arrow around
            {
                lowestAddress = 0xFFF0;
                highestAddress = 0xFFFF;
            }
            else // we want to move the memory range around
            {
                lowestAddress = sp - (sp == 0 ? 0 : 1);
                highestAddress = lowestAddress + 0xF;
            }

            int i = lowestAddress;
            if (sp != 0)
            {
                // Print the first line with an address indicator
                sb.AppendLine($"{Peek(lowestAddress).PadLeft(3, '0')} <-{lowestAddress.ToString("X4")}");
                i++;

                // Print lines before the address that the sp points to
                for (; i < sp; i++)
                    sb.AppendLine(Peek(i).PadLeft(3, '0'));

                // Print the line that sp points to
                sb.AppendLine($"{Peek(i++).PadLeft(3, '0')} <-{sp.ToString("X4")} S");
            }
            else
            {
                sb.AppendLine($"{Peek(sp).PadLeft(3, '0')} <-0000 S");
                i++;
            }

            // If sp points to the last line, dont print any more lines, else finish up
            if (sp != highestAddress)
            {
                // Print remaining middle lines
                for (; i < highestAddress; i++)
                    sb.AppendLine(Peek(i).PadLeft(3, '0'));

                // Print the last line with an address indicator
                sb.AppendLine($"{Peek(highestAddress).PadLeft(3, '0')} <-{highestAddress.ToString("X4")}");
            }

            argStackLbl.Text = sb.ToString();

            string Peek(int address)
            {
                int value = PeekMem(address);
                if (value == -1)
                    return "XXX";
                return value.ToString().PadLeft(3, '0');
            }
        }

        private static bool TryParseUShort(string str, out ushort value)
        {
            try
            {
                if (str.StartsWith("0x") || str.StartsWith("0X"))
                    value = Convert.ToUInt16(str.Substring(2), 16);
                else if (str.StartsWith("0b") || str.StartsWith("0B"))
                    value = Convert.ToUInt16(str.Substring(2), 2);
                else
                    value = ushort.Parse(str);

                return true;
            }
            catch (FormatException)
            {
                value = 0;
                return false;
            }
        }

        /// <summary>
        /// Get data from RAM or ROM.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        int PeekMem(int index)
        {
            if (index < 0 || index > 65535)
                throw new IndexOutOfRangeException();

            ushort indexCast = (ushort)index;
            if (rom.InRange(indexCast))
                return rom[rom.Relative(indexCast)];
            else if (ram.InRange(indexCast))
                return ram[ram.Relative(indexCast)];
            return -1;
        }

        private void NmiBtn_Click(object sender, EventArgs e)
        {
            pbi.TriggerNMI();
        }

        private void IrqBtn_Click(object sender, EventArgs e)
        {
            pbi.TriggerIRQ();
        }

        private void RstBtn_Click(object sender, EventArgs e)
        {
            pbi.TriggerRST();
        }

        private void UartDropdown_SelectedIndexChanged(object sender, EventArgs e)
        {
            object? selected = uartDropdown.SelectedItem;

            if (uart.Port != null)
            {
                if (selected != null && ((SerialPortInterface)uart.Port).port.PortName == (string)selected)
                    return;

                ((SerialPortInterface)uart.Port).port.Dispose();
                uart.Port = null;
            }

            if (selected == null || uartDropdown.SelectedIndex == 0)
                return;

            string portName = (string)selected;
            try
            {
                SerialPort port = new SerialPort(portName, 9600, Parity.None, 8, StopBits.One);
                port.Open();
                uart.Port = new SerialPortInterface(port);
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show(ex.Message, "Failed to connect to the port.");
                uartDropdown.SelectedIndex = 0;
            }
        }

        private void SerialPortDropdown_DropDown(object sender, EventArgs e)
        {
            string? oldName = uart.Port == null ? null : ((SerialPortInterface)uart.Port).port.PortName;

            uartDropdown.Items.Clear();
            uartDropdown.Items.Add("None");
            uartDropdown.Items.AddRange(SerialPort.GetPortNames());
            if (oldName != null)
            {
                int index = uartDropdown.Items.IndexOf(oldName);
                if (index != -1)
                    uartDropdown.SelectedIndex = index;
                else
                    uartDropdown.SelectedIndex = 0;
            }
        }

        private void GraphicsBtn_Click(object sender, EventArgs e)
        {
            graphicsBtn.Enabled = false;
            graphicsChipForm = new GraphicsChipOutput(graphicsChipBuffer);
            graphicsChipForm.FormClosed += (o, e) => graphicsBtn.Enabled = true;
            graphicsChipForm.Show();
        }

        private void TerminalBtn_Click(object sender, EventArgs e)
        {
            if (termDialog != null)
            {
                terminalBtn.Enabled = false;
                return;
            }
            terminalBtn.Enabled = false;

            termDialog = new OpenTermDialog();
            termDialog.FormClosed += (o, e) => { termDialog = null; terminalBtn.Enabled = true; };
            termDialog.Show();
        }

        private void DrivePathBtn_Click(object sender, EventArgs e)
        {
            DialogResult result = drivePathDialog.ShowDialog();
            if (result != DialogResult.OK)
                return;

            if (!Directory.Exists(drivePathDialog.SelectedPath))
                return;

            try
            {
                serialDrive.SetDrivePath(drivePathDialog.SelectedPath);
                outputLbl.Text = DateTime.Now.ToShortTimeString() + " Drive Path Set";
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
