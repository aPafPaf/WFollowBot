using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace WFollowBot.Memory
{
    internal static class MemoryScanner
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern int VirtualQueryEx(
            IntPtr hProcess, IntPtr lpAddress,
            out MEMORY_BASIC_INFORMATION lpBuffer, int dwLength);

        [StructLayout(LayoutKind.Sequential)]
        private struct MEMORY_BASIC_INFORMATION
        {
            public IntPtr BaseAddress;
            public IntPtr AllocationBase;
            public uint AllocationProtect;
            public IntPtr RegionSize;
            public uint State;
            public uint Protect;
            public uint Type;
        }

        private const uint MEM_COMMIT = 0x1000;
        private const uint PAGE_READWRITE = 0x04;
        private const uint PAGE_READONLY = 0x02;
        private const uint PAGE_EXECUTE_READWRITE = 0x40;
        private const uint PAGE_EXECUTE_READ = 0x20;
        private const uint PAGE_WRITECOPY = 0x08;
        private const uint PAGE_EXECUTE_WRITECOPY = 0x80;

        internal static List<IntPtr> FindAllQwordValues(byte[] value, bool scanWritableOnly = true)
        {
            var result = new List<IntPtr>();
            var hProcess = GetProcessHandle();
            if (hProcess == IntPtr.Zero)
                return result;

            var addr = new IntPtr(0x10000);
            const long maxAddr = 0x7FFFFFFF0000;
            const int chunkSize = 0x10000;

            while (addr.ToInt64() < maxAddr)
            {
                int ret = VirtualQueryEx(hProcess, addr, out var mbi, Marshal.SizeOf<MEMORY_BASIC_INFORMATION>());
                if (ret == 0)
                    break;

                if ((mbi.State & MEM_COMMIT) != 0)
                {
                    bool readable = scanWritableOnly
                        ? (mbi.Protect & 0xFF) is PAGE_READWRITE or PAGE_EXECUTE_READWRITE or PAGE_WRITECOPY or PAGE_EXECUTE_WRITECOPY
                        : (mbi.Protect & 0xFF) is PAGE_READONLY or PAGE_READWRITE or PAGE_EXECUTE_READ or PAGE_EXECUTE_READWRITE or PAGE_WRITECOPY or PAGE_EXECUTE_WRITECOPY;

                    if (readable)
                    {
                        var regionSize = mbi.RegionSize.ToInt64();
                        for (long offset = 0; offset < regionSize; offset += chunkSize)
                        {
                            var chunkAddr = mbi.BaseAddress + (int)Math.Min(offset, int.MaxValue);
                            var size = (int)Math.Min(regionSize - offset, chunkSize);
                            var data = MemoryReader.ReadBytes(chunkAddr, size);
                            if (data.Length == 0)
                                continue;

                            for (int i = 0; i <= data.Length - value.Length; i++)
                            {
                                bool match = true;
                                for (int b = 0; b < value.Length && match; b++)
                                    if (data[i + b] != value[b])
                                        match = false;

                                if (match)
                                    result.Add(chunkAddr + i);
                            }
                        }
                    }
                }

                var nextAddr = mbi.BaseAddress.ToInt64() + mbi.RegionSize.ToInt64();
                addr = new IntPtr(nextAddr);
            }

            return result;
        }

        private static IntPtr GetProcessHandle()
        {
            try
            {
                var processObj = typeof(GameHelper.Core)
                    .GetProperty("Process", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)!
                    .GetValue(null)!;

                var handle = processObj.GetType()
                    .GetProperty("Handle", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                    .GetValue(processObj)!;

                var dangerousGetHandle = handle.GetType()
                    .GetMethod("DangerousGetHandle", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)!;

                return (IntPtr)dangerousGetHandle.Invoke(handle, null)!;
            }
            catch
            {
                return IntPtr.Zero;
            }
        }
    }
}
