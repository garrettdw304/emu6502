using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emu6502
{
    public class ParallelExecutionException : Exception
    {
        public ParallelExecutionException() { }
        public ParallelExecutionException(string msg) : base(msg) { }
    }
}
