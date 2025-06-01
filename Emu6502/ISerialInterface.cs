using System;

namespace Emu6502
{
    public interface ISerialInterface
    {
        bool Available();
        bool ClearToSend();
        byte Read();
        void Write(byte value);
    }
}
