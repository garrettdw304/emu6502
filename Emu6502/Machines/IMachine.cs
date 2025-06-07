using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emu6502
{
    public interface IMachine
    {
        /// <summary>
        /// Indicates that an amount of time has passed equal to 1 second / hz.
        /// </summary>
        public void Cycle(int hz);
    }
}
