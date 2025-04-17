using Emu6502;
using System.Diagnostics;

namespace Emu6502Cli
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine($"Args: {string.Join(' ', args)}");

            // Create emulation and cpu and connect them
            Emulation emu = new Emulation();
            Cpu cpu = new Cpu();
            emu.OnCycle += cpu.Cycle;

            // Create devices
            Ram ram = new Ram(0, 0x8000);
            VirtualUart uart = new VirtualUart(0xB200, "COM95"); // COM95--COM96
            Console.WriteLine("Virtual serial port initialized. Use COM96 to connect.");
            Rom rom = new Rom(0xC000, 0x4000);
            if (args.Length > 0)
                rom.Program(args[0]);
            else
            {
                byte[] bin = new byte[0x4000];
                bin[bin.Length - 4] = 0x00;
                bin[bin.Length - 3] = 0xC0;
                for (int i = 0; i < 15; i++) bin[i] = 0xea; // NOP
                bin[15] = 0x4c; bin[16] = 0x00; bin[17] = 0xC0; // JMP ABS
                rom.Program(bin);
            }

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
            Console.WriteLine($"Stopwatch: {sw.ElapsedMilliseconds} ms");
            Console.WriteLine($"Delta: {sw.ElapsedMilliseconds * 1000 - (long)emu.CycleCount} cycles");
            Console.WriteLine($"Current pc: {cpu.pc:X2}");

            Console.WriteLine("Press enter to exit.");
            Console.ReadLine();
        }

        /// <summary>
        /// Tests the time that it takes to run varying numbers of cycles at a specific
        /// clock rate and compares it to the time that it should ideally take.
        /// </summary>
        private static void RunDeltaTests(int hz, int startMillionCycles, int endMillionCycles)
        {
            Emulation emu = new Emulation();
            Cpu cpu = new Cpu();
            emu.OnCycle += cpu.Cycle;
            Rom rom = new Rom(0, 65536);
            byte[] romData = new byte[65536];
            Array.Fill(romData, (byte)0xEA); // NOP
            cpu.bc.OnCycle += rom.OnCycle;

            for (int i = startMillionCycles; i <= endMillionCycles; i++)
            {
                Console.WriteLine("Starting test for " + i + " million cycles");
                Stopwatch ss = Stopwatch.StartNew();
                emu.Continue(hz, 1_000_000 * i);
                emu.Wait();
                Console.WriteLine($"Elapsed time for {1_000_000 * i} cycles at {hz}Hz = {ss.ElapsedMilliseconds}ms\n");
            }

            Console.WriteLine("Done. Press enter to exit.");
            Console.ReadLine();
        }
    }
}
