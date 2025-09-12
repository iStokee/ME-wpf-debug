using System;
using System.Runtime.InteropServices;

namespace MESharp.Native
{
    internal static class NativeUI
    {
        [DllImport("kernel32.dll")]
        internal static extern uint GetCurrentThreadId();

        // MemoryError native exports (same process)
        [DllImport("MemoryError", CallingConvention = CallingConvention.Cdecl, EntryPoint = "UI_RegisterWpfThreadId")]
        internal static extern void UI_RegisterWpfThreadId(uint tid);

        [DllImport("MemoryError", CallingConvention = CallingConvention.Cdecl, EntryPoint = "UI_RegisterWpfHwnd")]
        internal static extern void UI_RegisterWpfHwnd(IntPtr hwnd);

        [DllImport("MemoryError", CallingConvention = CallingConvention.Cdecl, EntryPoint = "UI_ActivateWpfWindow")]
        internal static extern void UI_ActivateWpfWindow();
    }
}
