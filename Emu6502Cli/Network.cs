using Emu6502;

namespace Emu6502Cli
{
    internal class Network
    {
        /// <summary>
        /// Starts a network of 6502 emulators connected via serial ports and a serial port switch.
        /// </summary>
        public static void Start()
        {
            // Could use multiple emulations to have each machine run on a different thread or run all on one.
            Emulation emu = new Emulation();

            Cpu router = CreateSystem("router.bin", "COM90"); // Connect with COM91
            router.bc.OnCycle += new VirtualUart(0xB300, "COM92").OnCycle; // Connect with COM93
            Cpu echo = CreateSystem("echo.bin", "COM91");
            Cpu user = CreateSystem("weather.bin", "COM93");

            emu.OnCycle += router.Cycle;
            emu.OnCycle += echo.Cycle;
            emu.OnCycle += user.Cycle;

            emu.Continue(1_000_000);
        }

        public static Cpu CreateSystem(string fileName, string serialPort)
        {
            Cpu cpu = new Cpu();

            Ram ram = new Ram(0, 0x8000);
            VirtualUart uart = new VirtualUart(0xB200, serialPort);
            Rom rom = new Rom(0xC000, 0x4000);

            rom.Program(fileName);

            cpu.bc.OnCycle += ram.OnCycle;
            cpu.bc.OnCycle += uart.OnCycle;
            cpu.bc.OnCycle += rom.OnCycle;

            return cpu;
        }
    }
}
