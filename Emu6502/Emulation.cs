using System.Diagnostics;

namespace Emu6502
{
    /// <summary>
    /// This class is not thread safe (don't access an instance of this class
    /// in more than one thread).
    /// </summary>
    public class Emulation
    {
        public bool IsRunning => exeSem.CurrentCount == 0;

        /// <summary>
        /// The number of cycles that have been executed.
        /// </summary>
        public ulong CycleCount { get; set; }

        /// <summary>
        /// A CPU should subscribe to this event.
        /// <br/>int - The hz that the clock is running at.
        /// </summary>
        public event Action<int>? OnCycle;

        /// <summary>
        /// Called when the emulation stops. Does not get called when using Cycle.
        /// </summary>
        public event Action OnStop;

        /// <summary>
        /// A single resource which gives a thread permission to execute cycles.
        /// </summary>
        private readonly SemaphoreSlim exeSem;
        /// <summary>
        /// A single resource which gives the executing thread permission to 
        /// change state. When a thread has exeSem but not this, it will
        /// continue to count CPU cycles but will not execute them until after
        /// it gets this resource.
        /// </summary>
        private readonly SemaphoreSlim stateAccessSem;

        private CancellationTokenSource exeCts;

        public Emulation()
        {
            exeSem = new SemaphoreSlim(1, 1);
            stateAccessSem = new SemaphoreSlim(1, 1);
            exeCts = new CancellationTokenSource();
            exeCts.Cancel();
        }

        /// <summary>
        /// Executes a single cycle on the current thread at a specific hz.
        /// </summary>
        public void Cycle(int hz)
        {
            ExpectExePerms();

            stateAccessSem.Wait();
            ExeCycle(hz);
            stateAccessSem.Release();

            ReleaseExePerms();
        }

        /// <summary>
        /// Executes a specified number of cycles at a specific clock rate
        /// on another thread.
        /// </summary>
        public void Continue(int hz, int cycles)
        {
            ExpectExePerms();

            exeCts = new CancellationTokenSource(); // TODO: I'm not even using this class correctly... (exeCts.Token!)
            Task.Run(() =>
            {
                Stopwatch sw = Stopwatch.StartNew();

                long ticksPerCycle = Stopwatch.Frequency / hz;
                long nextCycleTime = ticksPerCycle;
                Console.WriteLine("Ticks per cycle: " + ticksPerCycle);
                while (!exeCts.IsCancellationRequested)
                {
                    if (sw.ElapsedTicks < nextCycleTime)
                        continue;

                    // Continue to wait and accumulate cycles until we have
                    // permission to modify state.
                    // Also don't wanna block incase Emu.Stop() is called
                    // so that we can service that in a timely manner.
                    if (!stateAccessSem.Wait(0)) continue;
                    ExeCycle(hz);
                    stateAccessSem.Release();
                    nextCycleTime += ticksPerCycle;
                    if (--cycles <= 0)
                        break;
                }

                ReleaseExePerms();
                OnStop();
            });
        }

        public void Continue(int hz)
        {
            ExpectExePerms();

            exeCts = new CancellationTokenSource(); // TODO: I'm not even using this class correctly... (exeCts.Token!)
            Task.Run(() =>
            {
                Stopwatch sw = Stopwatch.StartNew();

                long ticksPerCycle = Stopwatch.Frequency / hz;
                long nextCycleTime = ticksPerCycle;
                Console.WriteLine("Ticks per cycle: " + ticksPerCycle);
                while (!exeCts.IsCancellationRequested)
                {
                    if (sw.ElapsedTicks < nextCycleTime)
                        continue;

                    // Continue to wait and accumulate cycles until we have
                    // permission to modify state.
                    // Also don't wanna block incase Emu.Stop() is called
                    // so that we can service that in a timely manner.
                    if (!stateAccessSem.Wait(0)) continue;
                    ExeCycle(hz);
                    stateAccessSem.Release();
                    nextCycleTime += ticksPerCycle;
                }

                ReleaseExePerms();
                OnStop();
            });
        }

        public void Continue(int hz, Func<bool> until)
        {
            ExpectExePerms();

            exeCts = new CancellationTokenSource(); // TODO: I'm not even using this class correctly... (exeCts.Token!)
            Task.Run(() =>
            {
                Stopwatch sw = Stopwatch.StartNew();

                long ticksPerCycle = Stopwatch.Frequency / hz;
                long nextCycleTime = ticksPerCycle;
                Console.WriteLine("Ticks per cycle: " + ticksPerCycle);
                while (!exeCts.IsCancellationRequested)
                {
                    if (sw.ElapsedTicks < nextCycleTime)
                        continue;

                    // Continue to wait and accumulate cycles until we have
                    // permission to modify state.
                    // Also don't wanna block incase Emu.Stop() is called
                    // so that we can service that in a timely manner.
                    if (!stateAccessSem.Wait(0)) continue;
                    ExeCycle(hz);
                    stateAccessSem.Release();
                    nextCycleTime += ticksPerCycle;
                    if (until())
                        break;
                }

                ReleaseExePerms();
                OnStop();
            });
        }

        public void Stop(bool wait)
        {
            exeCts?.Cancel();
            if (wait)
                // Wait for the execution thread to release the exe semaphore.
                Wait();
        }

        public void Wait()
        {
            exeSem.Wait();
            exeSem.Release();
        }

        /// <summary>
        /// This should be called before accessing the emulation's state to
        /// ensure the state is not changed while being accessed. After access
        /// using PauseState() is complete, ResumeState() must be called.
        /// </summary>
        public void PauseState()
        {
            stateAccessSem.Wait();
        }

        public void ResumeState()
        {
            stateAccessSem.Release();
        }

        /// <summary>
        /// Executes a single cycle.
        /// </summary>
        private void ExeCycle(int hz)
        {
            OnCycle?.Invoke(hz);
            CycleCount++;
        }

        /// <summary>
        /// Trys to take exe permissions and if it fails to aquire them, throws
        /// a ParallelExecutionException.
        /// </summary>
        /// <exception cref="ParallelExecutionException"/>
        private void ExpectExePerms()
        {
            if (!exeSem.Wait(0))
                throw new ParallelExecutionException();
        }

        private void ReleaseExePerms()
        {
            exeSem.Release();
        }
    }
}

// TODO: NOTES
// I have considered a few different ways of accessing system state.

// 1. Allow unfettered access to state information at any time.
// This option is what I want to avoid because it may cause 2 values to be
// displayed as if they are from the same point in time when they may not be.

// 2. Copy the entire state and proceed with execution.
// This seems pretty unreasonable because we would have to copy the entire
// state of every device in the emulation (yes even the kilobytes of RAM).

// 3. Require the emulation to be stopped while accessing state.
// This would cause the emulation to be off significantly over long periods of
// time. Meaning that if you run the emulation for 20 minutes it may have only
// accumulated 19 and some minutes of execution cycles. This would be an even
// bigger problem with more frequent and longer state accesses. This could be
// a reasonable solution if we do not need to show state outside the emulation
// while running such as only updating displays when the user pauses the
// emulation.
// 
// 4. Partially copy the state and then copy more state as the CPU changes state
// while in state access mode. This would be quite involved, especially for RAM.
//
// ***5. Pause execution but keep track of missed cycles. This is the option that
// I have decided to go with. This will keep the cycle count closer to a true
// system's over long periods of time. Also accesses will likely not be long
// enough that many cycles will be missed anyways. The biggest problem would
// be that it would be possible for events outside the emulation to be missed
// because we were in a pause state and the event was not detected by the
// system (which could theoretically already happen without this feature in
// place anyways).
// 
// 6. Possibly do a 'fast' state access where the state is accessed inbetween
// execution of cycles while we wait until time for the next cycle to execute.
// It sounds kind of like #5 but the emulation has priority over the outside
// accessor ensuring no cycles are missed whereas #5 will allow cycles to be
// missed and just catch up after. This would keep the cycles happening on time.
// This could be done by having the accessor wait until a cycle is finished
// and then allow them to continue accessing state but then cutting them off
// when the next cycle happens. This would be awkward, especially with the fact
// that we are already dealing with Windows scheduler and the likelyhood that
// the process is not running when it is time for the next cycle to execute.
// Though it may be feasable due to the 6502's slow clock (usually 1MHz).
