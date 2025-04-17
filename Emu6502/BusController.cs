// Define if the bus controller should throw an exception if no device responds to a read.
//#define THROW_ON_HIZ_READ
// Define if the bus controller should throw an exception if mupltiple devices try to write to the bus at the same time.
//#define THROW_ON_BUS_CONTENTION

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Emu6502
{
    /// <summary>
    /// Handles read/write accesses between the CPU and other devices.
    /// </summary>
    public class BusController : IDeviceInterface
    {
        /// <summary>
        /// True when reading, false when writing.
        /// </summary>
        public bool Rwb { get; set; }
        /// <summary>
        /// True when an opcode for an instruction is being read.
        /// </summary>
        public bool Sync { get; set; }
        /// <summary>
        /// Vector Pull is true when a byte to an interrupt routine's address
        /// is being fetched.
        /// </summary>
        public bool Vbp { get; set; }
        public ushort Address { get; set; }
        public byte Data
        {
            get
            {
                #if THROW_ON_HIZ_READ
                if (dataHiZ)
                    throw new BusException("Data bus is HiZ!");
                else
                #endif
                    return data;
            }
            set
            {
                #if THROW_ON_BUS_CONTENTION
                if (!dataHiZ)
                    throw new BusException("Data bus conflict!");
                else
                #endif
                    data = value;
            }
        }

        public InterruptLine Irq { get; }
        public InterruptLine Nmi { get; }
        public InterruptLine Rst { get; }

        /// <summary>
        /// Called on the rising edge of the clock, after the cpu has prepared
        /// the control lines such as rwb, address, and possibly data.
        /// </summary>
        public event Action<IDeviceInterface>? OnCycle;

        private byte data;
        private bool dataHiZ;

        public BusController(InterruptLine irq, InterruptLine nmi,
            InterruptLine rst)
        {
            Rwb = false;
            Sync = false;
            Vbp = false;
            Address = 0;

            Irq = irq;
            Nmi = nmi;
            Rst = rst;

            data = 0;
            dataHiZ = false;
        }

        public void Cycle()
        {
            OnCycle?.Invoke(this);
        }

        public void SetDataHiZ()
        {
            dataHiZ = true;
        }

        public void PrepareCycle(bool rwb, bool sync, bool vbp, ushort address)
        {
            Rwb = rwb;
            Sync = sync;
            Vbp = vbp;
            Address = address;
            dataHiZ = true;
        }

        public void PrepareCycle(bool rwb, bool sync, bool vbp, ushort address, byte data)
        {
            Rwb = rwb;
            Sync = sync;
            Vbp = vbp;
            Address = address;
            dataHiZ = false;
            this.data = data;
        }

        /// <summary>
        /// Throws BusException if data is hiZ after the cycle, else returns the value of the data bus.
        /// </summary>
        /// <exception cref="BusException"/>
        public byte ReadCycle(ushort address)
        {
            Rwb = true;
            Sync = false;
            Vbp = false;
            Address = address;
            dataHiZ = true;
            Cycle();
            return Data;
        }

        /// <summary>
        /// Performs a sync cycle which is a read with sync set.
        /// </summary>
        /// <exception cref="BusException"/>
        public byte SyncCycle(ushort address)
        {
            Rwb = true;
            Sync = true;
            Vbp = false;
            Address = address;
            dataHiZ = true;
            Cycle();
            return Data;
        }

        /// <summary>
        /// Performs a vector pull cycle which is a read with vpb set.
        /// For when an interrupt vector is being fetched.
        /// </summary>
        /// <exception cref="BusException"/>
        public byte VecCycle(ushort address)
        {
            Rwb = true;
            Sync = false;
            Vbp = true;
            Address = address;
            dataHiZ = true;
            Cycle();
            return Data;
        }

        /// <summary>
        /// Throws BusException if another device attempts to drive the data bus.
        /// </summary>
        /// <exception cref="BusException"/>
        public void WriteCycle(ushort address, byte data)
        {
            Rwb = false;
            Sync = false;
            Vbp = false;
            Address = address;
            dataHiZ = false;
            this.data = data;
            Cycle();
        }
    }

    public interface IDeviceInterface
    {
        public bool Rwb { get;  }
        public bool Sync { get; }
        public bool Vbp { get; }
        public ushort Address { get; }
        public byte Data { get; set; }

        public InterruptLine Irq { get; }
        public InterruptLine Nmi { get; }
        public InterruptLine Rst { get; }

        public event Action<IDeviceInterface>? OnCycle;
    }


    public class BusException : Exception
    {
        public BusException() { }
        public BusException(string message) : base(message) { }
    }
}
