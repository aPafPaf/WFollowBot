using System;
using System.Reflection;

namespace WFollowBot.Memory
{
    internal static class MemoryReader
    {
        private static readonly object ProcessHandle;
        private static readonly object ProcessObject;
        private static readonly MethodInfo ReadMemoryMethod;
        private static readonly MethodInfo ReadMemoryArrayMethod;
        private static readonly MethodInfo ReadStringMethod;
        private static readonly MethodInfo ReadUnicodeStringMethod;

        static MemoryReader()
        {
            ProcessObject = typeof(GameHelper.Core)
                .GetProperty("Process", BindingFlags.Public | BindingFlags.Static)!
                .GetValue(null)!;

            ProcessHandle = ProcessObject!
                .GetType()
                .GetProperty("Handle", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(ProcessObject)!;

            var handleType = ProcessHandle.GetType();
            ReadMemoryMethod = handleType.GetMethod("ReadMemory", BindingFlags.Instance | BindingFlags.NonPublic)!;
            ReadMemoryArrayMethod = handleType.GetMethod("ReadMemoryArray", BindingFlags.Instance | BindingFlags.NonPublic)!;
            ReadStringMethod = handleType.GetMethod("ReadString", BindingFlags.Instance | BindingFlags.NonPublic)!;
            ReadUnicodeStringMethod = handleType.GetMethod("ReadUnicodeString", BindingFlags.Instance | BindingFlags.NonPublic)!;
        }

        public static T ReadMemory<T>(IntPtr address) where T : unmanaged
        {
            try
            {
                var result = ReadMemoryMethod
                    .MakeGenericMethod(typeof(T))
                    .Invoke(ProcessHandle, new object[] { address });
                return (T)result!;
            }
            catch
            {
                return default;
            }
        }

        public static IntPtr ReadIntPtr(IntPtr address) => ReadMemory<IntPtr>(address);
        public static int ReadInt32(IntPtr address) => ReadMemory<int>(address);
        public static uint ReadUInt32(IntPtr address) => ReadMemory<uint>(address);
        public static long ReadInt64(IntPtr address) => ReadMemory<long>(address);
        public static ulong ReadUInt64(IntPtr address) => ReadMemory<ulong>(address);
        public static short ReadInt16(IntPtr address) => ReadMemory<short>(address);
        public static ushort ReadUInt16(IntPtr address) => ReadMemory<ushort>(address);
        public static byte ReadByte(IntPtr address) => ReadMemory<byte>(address);
        public static float ReadFloat(IntPtr address) => ReadMemory<float>(address);
        public static double ReadDouble(IntPtr address) => ReadMemory<double>(address);
        public static bool ReadBool(IntPtr address) => ReadMemory<bool>(address);

        public static byte[] ReadBytes(IntPtr address, int count)
        {
            try
            {
                var result = ReadMemoryArrayMethod
                    .MakeGenericMethod(typeof(byte))
                    .Invoke(ProcessHandle, new object[] { address, count });
                return (byte[])result!;
            }
            catch
            {
                return Array.Empty<byte>();
            }
        }

        public static string ReadString(IntPtr address)
        {
            try
            {
                var result = ReadStringMethod.Invoke(ProcessHandle, new object[] { address });
                return (string)result!;
            }
            catch
            {
                return string.Empty;
            }
        }

        public static string ReadUnicodeString(IntPtr address)
        {
            try
            {
                var result = ReadUnicodeStringMethod.Invoke(ProcessHandle, new object[] { address });
                return (string)result!;
            }
            catch
            {
                return string.Empty;
            }
        }

        public static IntPtr GetModuleBase()
        {
            try
            {
                var processType = ProcessObject.GetType();
                var addressProp = processType.GetProperty("Address", BindingFlags.Instance | BindingFlags.NonPublic);
                if (addressProp == null)
                    addressProp = processType.GetProperty("Address", BindingFlags.Instance | BindingFlags.Public);
                return (IntPtr)addressProp!.GetValue(ProcessObject)!;
            }
            catch
            {
                return IntPtr.Zero;
            }
        }

        public static int GetModuleSize()
        {
            try
            {
                var infoProp = ProcessObject.GetType()
                    .GetProperty("Information", BindingFlags.Instance | BindingFlags.NonPublic);
                if (infoProp == null) return 0;
                var info = (System.Diagnostics.Process)infoProp.GetValue(ProcessObject)!;
                return info.MainModule?.ModuleMemorySize ?? 0;
            }
            catch
            {
                return 0;
            }
        }
    }
}
