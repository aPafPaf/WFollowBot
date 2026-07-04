using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WFollowBot.Memory
{
    public struct Pattern
    {
        public readonly string Name;
        public readonly byte[] Data;
        public readonly bool[] Mask;
        public readonly int BytesToSkip;

        public Pattern(string name, string arrayOfHexBytes)
        {
            this.Name = name;
            var arrayOfHexBytesList = arrayOfHexBytes.Split(
                new[] { " ", "," }, StringSplitOptions.RemoveEmptyEntries).ToList();

            this.BytesToSkip = arrayOfHexBytesList.FindIndex("^".Equals);
            if (this.BytesToSkip < 0)
            {
                throw new ArgumentException(
                    $"Pattern '{name}' must contain a '^' marker to indicate BytesToSkip");
            }

            (this.Data, this.Mask) = ParseArrayOfHexBytes(
                arrayOfHexBytesList.Where(hex => hex != "^").ToList());
        }

        public Pattern(string name, string arrayOfHexBytes, int bytesToSkip)
        {
            this.Name = name;
            this.BytesToSkip = bytesToSkip;
            (this.Data, this.Mask) = ParseArrayOfHexBytes(arrayOfHexBytes.Split(
                new[] { " ", "," }, StringSplitOptions.RemoveEmptyEntries).ToList());
        }

        private static (byte[], bool[]) ParseArrayOfHexBytes(List<string> arrayOfHexBytes)
        {
            List<bool> mask = new();
            List<byte> data = new();
            for (var i = 0; i < arrayOfHexBytes.Count; i++)
            {
                var hexByte = arrayOfHexBytes[i];
                if (hexByte.StartsWith("?"))
                {
                    data.Add(0x00);
                    mask.Add(false);
                }
                else
                {
                    data.Add(byte.Parse(hexByte, NumberStyles.HexNumber));
                    mask.Add(true);
                }
            }

            return (data.ToArray(), mask.ToArray());
        }
    }
}
