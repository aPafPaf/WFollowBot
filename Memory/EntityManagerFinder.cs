using System;

namespace WFollowBot.Memory
{
    internal static class EntityManagerFinder
    {
        private static readonly Pattern[] Patterns =
        {
            new(
                "Entity Manager",
                "48 8B 05 ^ ?? ?? ?? ?? 4C 89 25 ?? ?? ?? ?? 49 8B 8E 38 02 00 00"
            )
        };

        internal const long TargetingStateVtable = 0x142abee70;
        private const int EntityManagerSize = 0x2160;

        private static IntPtr? _entityManagerPtrAddr;
        private static IntPtr? _entityManagerInstance;
        private static IntPtr? _targetingStateAddr;
        private static long _lastFrame;
        private static string _lastScanInfo = "not scanned";
        private static int _lastDisplacement;
        private static int _lastPatternOffset;

        public static IntPtr? EntityManagerPtrAddress
        {
            get
            {
                EnsureFound();
                return _entityManagerPtrAddr;
            }
        }

        public static IntPtr? EntityManagerInstance
        {
            get
            {
                EnsureFound();
                return _entityManagerInstance;
            }
        }

        public static IntPtr? TargetingStateAddress
        {
            get
            {
                EnsureFound();
                return _targetingStateAddr;
            }
        }

        public static string LastScanInfo => _lastScanInfo;
        public static int LastDisplacement => _lastDisplacement;
        public static int LastPatternOffset => _lastPatternOffset;

        private static void EnsureFound()
        {
            var now = Environment.TickCount64;
            if (_entityManagerInstance != null && now - _lastFrame < 1000)
                return;

            _lastFrame = now;
            FindEntityManager();
            FindTargetingState();
        }

        private static void FindEntityManager()
        {
            var baseAddr = MemoryReader.GetModuleBase();
            var moduleSize = MemoryReader.GetModuleSize();

            if (baseAddr == IntPtr.Zero || moduleSize <= 0)
                return;

            var results = PluginPatternFinder.Find(Patterns, baseAddr, moduleSize);
            if (!results.TryGetValue("Entity Manager", out var patternOffset))
                return;

            // patternOffset = address of ^ (start of 4-byte relative offset)
            // x64 RIP-relative: target = next_instruction_addr + displacement
            // next_instruction_addr = instruction_base + 7 = (instruction_base + BytesToSkip) + (7 - BytesToSkip)
            //                      = patternOffset + 5  (since BytesToSkip=2, instr_len=7)
            _lastPatternOffset = patternOffset;
            var displacement = MemoryReader.ReadInt32(baseAddr + patternOffset);
            _lastDisplacement = displacement;
            _entityManagerPtrAddr = baseAddr + patternOffset + displacement + 5;
            _entityManagerInstance = MemoryReader.ReadIntPtr(_entityManagerPtrAddr.Value);
        }

        private static void FindTargetingState()
        {
            if (_entityManagerInstance == null)
            {
                _targetingStateAddr = null;
                _lastScanInfo = "no entity manager";
                return;
            }

            var vtableBytes = BitConverter.GetBytes(TargetingStateVtable);

            // 0. Scan inside EntityManager (maybe embedded as a field)
            var emInstance = _entityManagerInstance.Value;
            var emData = MemoryReader.ReadBytes(emInstance, EntityManagerSize);
            if (emData.Length == EntityManagerSize)
            {
                for (int i = 0; i <= emData.Length - 8; i += 8)
                {
                    if (emData[i] == vtableBytes[0] && emData[i + 1] == vtableBytes[1] &&
                        emData[i + 2] == vtableBytes[2] && emData[i + 3] == vtableBytes[3] &&
                        emData[i + 4] == vtableBytes[4] && emData[i + 5] == vtableBytes[5] &&
                        emData[i + 6] == vtableBytes[6] && emData[i + 7] == vtableBytes[7])
                    {
                        _targetingStateAddr = emInstance + i;
                        _lastScanInfo = $"inside EntityManager +0x{i:X}";
                        return;
                    }
                }
            }

            // 1. Scan module image (.text, .rdata, .data)
            var baseAddr = MemoryReader.GetModuleBase();
            var moduleSize = MemoryReader.GetModuleSize();
            if (baseAddr != IntPtr.Zero && moduleSize > 0)
            {
                var rangeEnd = baseAddr.ToInt64() + moduleSize;
                const int chunkSize = 0x10000;
                for (var addr = baseAddr.ToInt64(); addr < rangeEnd; addr += chunkSize - 8)
                {
                    var remaining = (int)Math.Min(rangeEnd - addr, chunkSize);
                    var data = MemoryReader.ReadBytes(new IntPtr(addr), remaining);
                    for (int i = 0; i <= data.Length - 8; i++)
                    {
                        if (data[i] == vtableBytes[0] && data[i + 1] == vtableBytes[1] &&
                            data[i + 2] == vtableBytes[2] && data[i + 3] == vtableBytes[3] &&
                            data[i + 4] == vtableBytes[4] && data[i + 5] == vtableBytes[5] &&
                            data[i + 6] == vtableBytes[6] && data[i + 7] == vtableBytes[7])
                        {
                            _targetingStateAddr = new IntPtr(addr + i);
                            _lastScanInfo = "module image (.text/.rdata/.data)";
                            return;
                        }
                    }
                }
            }

            // 2. Full scan: all writable memory via VirtualQueryEx (heap instances)
            _lastScanInfo = "scanning writable memory (VirtualQueryEx)...";
            var matches = MemoryScanner.FindAllQwordValues(vtableBytes, scanWritableOnly: true);
            foreach (var match in matches)
            {
                var vtableCheck = MemoryReader.ReadIntPtr(match);
                if (vtableCheck.ToInt64() == TargetingStateVtable)
                {
                    _targetingStateAddr = match;
                    _lastScanInfo = $"writable memory (match {matches.Count} total)";
                    return;
                }
            }

            _targetingStateAddr = null;
            _lastScanInfo = $"not found (scanned module + {matches.Count} vtable refs in writable mem)";
        }

        public static void InvalidateCache()
        {
            _entityManagerPtrAddr = null;
            _entityManagerInstance = null;
            _targetingStateAddr = null;
            _lastFrame = 0;
            _lastScanInfo = "cache invalidated";
        }
    }
}
