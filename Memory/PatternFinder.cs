using System;
using System.Collections.Generic;
using System.Linq;

namespace WFollowBot.Memory
{
    internal static class PluginPatternFinder
    {
        private const int MaxBytesObject = 84000;
        private const int ChunkOverlap = 64;

        internal static Dictionary<string, int> Find(
            Pattern[] patterns, IntPtr baseAddress, int moduleSize)
        {
            var patternMaxLength = patterns.Max(p => p.Data.Length);
            var totalReads = (moduleSize + MaxBytesObject - 1) / MaxBytesObject;

            var result = new Dictionary<string, int>();
            var found = new bool[patterns.Length];
            var offsets = new int[patterns.Length];
            var totalFound = 0;

            var lockObj = new object();

            for (int chunk = 0; chunk < totalReads; chunk++)
            {
                if (totalFound >= patterns.Length)
                    break;

                var chunkOffset = chunk * MaxBytesObject;
                var isLast = chunk == totalReads - 1;
                var readSize = isLast
                    ? moduleSize - chunkOffset
                    : Math.Min(MaxBytesObject + patternMaxLength, moduleSize - chunkOffset);

                if (readSize <= 0)
                    continue;

                var data = MemoryReader.ReadBytes(
                    new IntPtr(baseAddress.ToInt64() + chunkOffset), readSize);

                if (data.Length < patternMaxLength)
                    continue;

                for (int i = 0; i <= data.Length - patternMaxLength && totalFound < patterns.Length; i++)
                {
                    for (int p = 0; p < patterns.Length; p++)
                    {
                        if (found[p])
                            continue;

                        var pat = patterns[p];
                        if (data.Length - i < pat.Data.Length)
                            continue;

                        bool match = true;
                        for (int b = 0; b < pat.Data.Length && match; b++)
                        {
                            if (pat.Mask[b] && data[i + b] != pat.Data[b])
                                match = false;
                        }

                        if (match)
                        {
                            lock (lockObj)
                            {
                                if (!found[p])
                                {
                                    found[p] = true;
                                    offsets[p] = chunkOffset + i + pat.BytesToSkip;
                                    totalFound++;
                                }
                            }
                        }
                    }
                }
            }

            if (totalFound < patterns.Length)
                throw new Exception(
                    $"Couldn't find all patterns. Found {totalFound}/{patterns.Length}. " +
                    "Kindly fix the patterns.");

            for (int i = 0; i < patterns.Length; i++)
                result.Add(patterns[i].Name, offsets[i]);

            return result;
        }
    }
}
