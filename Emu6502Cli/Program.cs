using Emu6502;
using System.Diagnostics;

namespace Emu6502Cli
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Create emulation and cpu and connect them
            Emulation emu = new Emulation();
            Cpu cpu = new Cpu();
            emu.OnCycle += cpu.Cycle;

            // Create devices
            Ram ram = new Ram(0, 0x8000);
            SimpleUart uart = new SimpleUart(0xD000);
            Rom rom = new Rom(0xC000, 0x4000);
            byte[] bin = new byte[0x4000];
            bin[bin.Length - 4] = 0x00;
            bin[bin.Length - 3] = 0xC0;
            for (int i = 0; i < 15; i++) bin[i] = 0xea; // NOP
            bin[15] = 0x4c; bin[16] = 0x00; bin[17] = 0xC0; // JMP ABS
            rom.Program(bin);

            // Connect devices
            cpu.bc.OnCycle += ram.OnCycle;
            cpu.bc.OnCycle += uart.OnCycle;
            cpu.bc.OnCycle += rom.OnCycle;

            // Start a stopwatch
            Stopwatch sw = Stopwatch.StartNew();

            // Run the emulation at 1MHz
            emu.Continue(1_000_000);

            // Stop the emulation
            Console.WriteLine("Press enter to stop the emulation.");
            Console.ReadLine();
            emu.Stop(true);
            sw.Stop();

            // Print some stats
            Console.WriteLine($"Cycles emulated: {emu.CycleCount}");
            Console.WriteLine($"Current pc: {cpu.pc:X2}");

            Console.WriteLine("Press enter to exit.");
            Console.ReadLine();
        }
    }
}
