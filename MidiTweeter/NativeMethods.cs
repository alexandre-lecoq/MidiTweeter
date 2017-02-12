namespace MidiTweeter
{
    using System.Runtime.InteropServices;

    public static class NativeMethods
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool Beep(uint frequency, uint duration);

        [DllImport("kernel32.dll")]
        internal static extern void Sleep(uint milliseconds);
    }
}
