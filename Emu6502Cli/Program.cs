using Emu6502;
using System.Diagnostics;

namespace Emu6502Cli
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //RunKlausTests();
            RunDeltaTests2(1_000_000, 1000, 10000);
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
            rom.Program(romData);
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

        private static void RunDeltaTests2(int hz, int startMilliseconds, int endMilliseconds)
        {
            //byte[] romData = new byte[65536];
            //Array.Fill(romData, (byte)0xEA); // NOP

            for (int i = startMilliseconds; i <= endMilliseconds; i+=1000)
            {
                Emulation emu = new Emulation();
                Cpu cpu = new Cpu();
                emu.OnCycle += cpu.Cycle;
                Rom rom = new Rom(0, 65536);
                rom.Program("C:\\Users\\garre\\Downloads\\6502_65C02_functional_tests-master\\6502_functional_test.bin");
                cpu.bc.OnCycle += rom.OnCycle;

                Console.WriteLine("Starting test for ~" + i + " milliseconds.");
                Stopwatch ss = Stopwatch.StartNew();
                emu.Continue(hz);
                while (ss.ElapsedMilliseconds < i);
                emu.Stop(true);
                Console.WriteLine($"Elapsed time for {emu.CycleCount} cycles at {hz}Hz = {ss.ElapsedMilliseconds}ms\n");
            }

            Console.WriteLine("Done. Press enter to exit.");
            Console.ReadLine();
        }

        /// <summary>
        /// Runs tests from this repo: https://github.com/Klaus2m5/6502_65C02_functional_tests
        /// </summary>
        private static void RunKlausTests()
        {
            Emulation emu = new Emulation();
            Cpu cpu = new Cpu();
            emu.OnCycle += cpu.Cycle;

            Ram ram = new Ram(0, 65536); // zero page and stack page
            ram.Program(File.ReadAllBytes("C:\\Users\\garre\\Downloads\\6502_65C02_functional_tests-master\\6502_functional_test.bin"), 0x000a);
            cpu.bc.OnCycle += ram.OnCycle;

            cpu.pc = 0x400;
            cpu.step = -1; // NEXT_OPCODE_STEP, cause a new opcode to be loaded on next cycle

            // AUTOMATIC EXECUTION
            //Console.WriteLine($"Cycles: {emu.CycleCount}; PC: {cpu.pc:X2}");
            //emu.Continue(1_000_000);
            //while (true)
            //{
            //    Thread.Sleep(1000);

            //    emu.PauseState();
            //    Console.WriteLine($"Cycles: {emu.CycleCount}; PC: {cpu.pc:X2}");
            //    emu.ResumeState();
            //}

            // MANUAL EXECUTION
            //Console.WriteLine($"Cycles: {emu.CycleCount}; PC: {cpu.pc:X2}");
            //while (true)
            //{
            //    Console.ReadLine();

            //    emu.Cycle(1_000_000);

            //    Console.WriteLine($"Cycles: {emu.CycleCount}; PC: {cpu.pc:X2}");
            //}

            // SEMI AUTO
            Console.WriteLine($"Cycles: {emu.CycleCount}; PC: {cpu.pc:X2}");
            while (true)
            {
                emu.Cycle(1_000_000);

                Console.WriteLine(cpu.ToString());
                // Console.WriteLine($"Cycles: {emu.CycleCount}; PC: {cpu.pc:X2}");
                
                if (cpu.pc == 0x3699)
                {
                    while (true)
                    {
                        Thread.Sleep(1000);
                        Console.Beep();
                        Console.WriteLine("SUCCESS!");
                    }
                }
            }
        }
    }
}
