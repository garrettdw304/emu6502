using System.Diagnostics;

namespace Emu6502
{
    /// <summary>
    /// Triggers <see cref="IMachine.Cycle(int)"/> at regular intervals based on a provided clock rate in Hz.
    /// <br/>NOTE: This class is not thread safe. An Emulation object should only ever be utilized by a single thread.
    /// </summary>
    public class Emulation
    {
        public bool IsRunning => exeSem.CurrentCount == 0;

        public long CycleCount { get; private set; }

        public event Action? OnStop;

        /// <summary>
        /// A single resource which gives a thread permission to execute cycles.
        /// </summary>
        private readonly SemaphoreSlim exeSem = new SemaphoreSlim(1, 1);
        /// <summary>
        /// A single resource which gives a thread permission to access the state of the emulated world.
        /// </summary>
        private readonly SemaphoreSlim accessSem = new SemaphoreSlim(1, 1);
        /// <summary>
        /// The <see cref="Task"/> and <see cref="CancellationTokenSource"/> of the most recent execution task.
        /// </summary>
        private ExeInfo? exeInfo;
        private event Action<int>? CycleEvent;

        public Emulation()
        {

        }

        /// <summary>
        /// Synchronously executes a cycle. OnStop is not executed.
        /// </summary>
        public void Cycle(int hz)
        {
            ExpectExeSem();

            accessSem.Wait();
            ExeCycle(hz);
            accessSem.Release();

            ReleaseExeSem();
        }

        public void Continue(int hz)
        {
            ExpectExeSem();

            CancellationTokenSource exeCts = new CancellationTokenSource();
            Task exeTask = new Task(() =>
            {
                CancellationToken token = exeCts.Token;

                Stopwatch sw = Stopwatch.StartNew();
                long ticksPerCycle = Stopwatch.Frequency / hz;
                long nextCycleTime = ticksPerCycle;

                while (!token.IsCancellationRequested)
                {
                    if (sw.ElapsedTicks < nextCycleTime)
                        continue;

                    if (!accessSem.Wait(0)) continue;
                    ExeCycle(hz);
                    accessSem.Release();
                    nextCycleTime += ticksPerCycle;
                }

                ReleaseExeSem();
                OnStop?.Invoke();
            });
            exeInfo = new ExeInfo(exeTask, exeCts);
            exeTask.Start();
        }

        public void Continue(int hz, Func<bool> until)
        {
            ExpectExeSem();

            CancellationTokenSource exeCts = new CancellationTokenSource();
            Task exeTask = new Task(() =>
            {
                CancellationToken token = exeCts.Token;

                Stopwatch sw = Stopwatch.StartNew();
                long ticksPerCycle = Stopwatch.Frequency / hz;
                long nextCycleTime = ticksPerCycle;

                while (!token.IsCancellationRequested)
                {
                    if (sw.ElapsedTicks < nextCycleTime)
                        continue;

                    if (!accessSem.Wait(0)) continue;
                    ExeCycle(hz);
                    accessSem.Release();
                    nextCycleTime += ticksPerCycle;
                    if (until())
                        break;
                }

                ReleaseExeSem();
                OnStop?.Invoke();
            });
            exeInfo = new ExeInfo(exeTask, exeCts);
            exeTask.Start();
        }

        /// <summary>
        /// Stops the execution thread and optionally waits until the thread has terminated.
        /// </summary>
        /// <param name="wait"></param>
        public void Stop(bool wait)
        {
            ExeInfo? info = exeInfo;

            if (info == null)
                return;

            info.cts.Cancel();

            if (wait)
            {
                if (Task.CurrentId == info.task.Id)
                    return;
                info.task.Wait();
            }
        }

        public void Wait()
        {
            ExeInfo? info = exeInfo;

            if (info == null)
                return;

            if (Task.CurrentId == info.task.Id)
                return;

            info.task.Wait();
        }

        public void AddMachine(IMachine machine)
        {
            CycleEvent += machine.Cycle;
        }

        public void RemoveMachine(IMachine machine)
        {
            CycleEvent -= machine.Cycle;
        }

        public void PauseState()
        {
            accessSem.Wait();
        }

        public void ResumeState()
        {
            accessSem.Release();
        }

        /// <summary>
        /// Executes a single cycle via a call to <see cref="CycleEvent"/> and increments <see cref="CycleCount"/>.
        /// </summary>
        /// <param name="hz">The Hz of the clock signal during this cycle.</param>
        private void ExeCycle(int hz)
        {
            CycleEvent?.Invoke(hz);
            CycleCount++;
        }

        /// <summary>
        /// Claims the <see cref="exeSem"/> resource or throws if it is not available.
        /// </summary>
        /// <exception cref="ParallelExecutionException"></exception>
        private void ExpectExeSem()
        {
            if (!exeSem.Wait(0))
                throw new ParallelExecutionException();
        }

        /// <summary>
        /// Releases the <see cref="exeSem"/> resource.
        /// </summary>
        /// <exception cref="SemaphoreFullException"/>
        private void ReleaseExeSem()
        {
            exeSem.Release();
        }

        /// <summary>
        /// Pairs a <see cref="Task"/> and a <see cref="CancellationTokenSource"/> together.
        /// </summary>
        private class ExeInfo
        {
            public readonly Task task;
            public readonly CancellationTokenSource cts;

            public ExeInfo(Task exeTask, CancellationTokenSource exeCts)
            {
                task = exeTask;
                cts = exeCts;
            }
        }
    }
}
