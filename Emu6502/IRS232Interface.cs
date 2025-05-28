using System;

namespace Emu6502
{
    public interface IRS232Interface
    {
        bool Available();
        bool ClearToSend();
        byte Read();
        void Write(byte value);
    }
}
