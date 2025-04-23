using Emu6502;
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

        private readonly Emulation emu;
        private readonly Cpu cpu;

        private readonly Rom rom;
        private VirtualUart? uart;
        private readonly SimpleTimer timer;
        private readonly Ram ram;
        private readonly PushButtonInterruptors pbi;

        /// <summary>
        /// First row displayed in mem display.
        /// </summary>
        private int memDisplayRow = 0;

        public Form1()
        {
            InitializeComponent();

            // Create emulation, cpu and connect them.
            emu = new Emulation();
            cpu = new Cpu();
            emu.OnCycle += cpu.Cycle;

            // Create devices
            rom = new Rom(ROM_BASE_ADDRESS, ROM_SIZE);
            timer = new SimpleTimer(0xB100, cpu.irq);
            ram = new Ram(RAM_BASE_ADDRESS, RAM_SIZE);
            pbi = new PushButtonInterruptors(cpu.irq, cpu.nmi, cpu.rst);

            // Connect devices
            cpu.bc.OnCycle += ram.OnCycle;
            cpu.bc.OnCycle += timer.OnCycle;
            cpu.bc.OnCycle += rom.OnCycle;
            cpu.bc.OnCycle += pbi.OnCycle;
        }

        private void MemDisplay_RetrieveVirtualItem(object? sender, RetrieveVirtualItemEventArgs e)
        {
            int startByte = e.ItemIndex * 16;
            if (startByte >= ROM_BASE_ADDRESS && startByte < ROM_SIZE)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("0000");
                for (int i = startByte; i < Math.Min(startByte + 16, ROM_SIZE); i++)
                    sb.Append(' ').Append(rom[i]);
                e.Item = new ListViewItem(sb.ToString());
            }
            else if (startByte >= RAM_BASE_ADDRESS && startByte < RAM_SIZE)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("0000");
                for (int i = startByte; i < Math.Min(startByte + 16, RAM_SIZE); i++)
                    sb.Append(' ').Append(ram[i]);
                e.Item = new ListViewItem(sb.ToString());
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            MouseWheel += Form1_MouseWheel;
        }

        private void continueBtn_Click(object sender, EventArgs e)
        {
            if (emu.IsRunning)
            {
                emu.Stop(false);
                continueBtn.Text = "Continue";
                cycleBtn.Enabled = true;
                clockRateTB.Enabled = true;
                loadRomBtn.Enabled = true;
                serialPortDropdown.Enabled = true;
            }
            else
            {
                if (!int.TryParse(clockRateTB.Text, out int hz))
                {
                    clockRateTB.Text = "";
                    return;
                }

                emu.Continue(hz);
                continueBtn.Text = "Stop";
                cycleBtn.Enabled = false;
                clockRateTB.Enabled = false;
                loadRomBtn.Enabled = false;
                serialPortDropdown.Enabled = false;
            }
        }

        private void cycleBtn_Click(object sender, EventArgs e)
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

        private void loadRomBtn_Click(object sender, EventArgs e)
        {
            DialogResult result = romFileDialog.ShowDialog();
            if (result != DialogResult.OK)
                return;

            if (!File.Exists(romFileDialog.FileName))
                return;

            rom.Program(romFileDialog.FileName);
            outputLbl.Text = DateTime.Now.ToShortTimeString() + " ROM programmed";
        }

        private void statusTimer_Tick(object sender, EventArgs e)
        {
            emu.PauseState();
            cpuStatusLbl.Text = cpu.ToString() + "\nCycles: " + emu.CycleCount;
            UpdateMemDisplay();
            UpdateStackDisplay();
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

        private void scrollToBtn_Click(object sender, EventArgs e)
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
                sb.AppendLine($"{PeekMem(0x100 | lowestAddress).ToString().PadLeft(3, '0')} <- 0x{lowestAddress.ToString("X2")}");
                i++;

                // Print lines before the address that the sp points to
                for (; i < cpu.s; i++)
                    sb.AppendLine(PeekMem(0x100 | i).ToString().PadLeft(3, '0'));

                // Print the line that sp points to
                sb.AppendLine($"{PeekMem(0x100 | i++).ToString().PadLeft(3, '0')} <- SP");
            }
            else
            {
                sb.AppendLine($"{PeekMem(0x100 | cpu.s).ToString().PadLeft(3, '0')} <- SP");
                i++;
            }

            // If sp points to the last line, dont print any more lines, else finish up
            if (cpu.s != highestAddress)
            {
                // Print remaining middle lines
                for (; i < highestAddress; i++)
                    sb.AppendLine(PeekMem(0x100 | i).ToString().PadLeft(3, '0'));

                // Print the last line with an address indicator
                sb.AppendLine($"{PeekMem(0x100 | highestAddress).ToString().PadLeft(3, '0')} <- 0x{highestAddress.ToString("X2")}");
            }

            stackLbl.Text = sb.ToString();
        }

        private bool TryParseUShort(string str, out ushort value)
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

        private void nmiBtn_Click(object sender, EventArgs e)
        {
            pbi.TriggerNMI();
        }

        private void irqBtn_Click(object sender, EventArgs e)
        {
            pbi.TriggerIRQ();
        }

        private void rstBtn_Click(object sender, EventArgs e)
        {
            pbi.TriggerRST();
        }

        private void serialPortDropdown_SelectedIndexChanged(object sender, EventArgs e)
        {
            object? selected = serialPortDropdown.SelectedItem;

            if (uart != null)
            {
                if (selected != null && uart.PortName == (string)selected)
                    return;

                cpu.bc.OnCycle -= uart.OnCycle;
                uart.Dispose();
                uart = null;
            }

            if (selected == null || serialPortDropdown.SelectedIndex == 0)
                return;

            string portName = (string)selected;
            try
            {
                uart = new VirtualUart(UART_BASE_ADDRESS, portName);
                cpu.bc.OnCycle += uart.OnCycle;
            } catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show(ex.Message, "Failed to connect to the port.");
                serialPortDropdown.SelectedIndex = 0;
            }
        }

        private void serialPortDropdown_DropDown(object sender, EventArgs e)
        {
            string? oldName = uart?.PortName;

            serialPortDropdown.Items.Clear();
            serialPortDropdown.Items.Add("None");
            serialPortDropdown.Items.AddRange(SerialPort.GetPortNames());
            if (oldName != null)
            {
                int index = serialPortDropdown.Items.IndexOf(oldName);
                if (index != -1)
                    serialPortDropdown.SelectedIndex = index;
                else
                    serialPortDropdown.SelectedIndex = 0;
            }
        }
    }
}
