namespace Emu6502
{
    public interface IDevice
    {
        public void OnCycle(IDeviceInterface bc);
    }
}
