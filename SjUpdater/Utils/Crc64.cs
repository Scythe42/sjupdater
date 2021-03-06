﻿using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace SjUpdater.Utils
{
    /// <summary>
    /// Implements a 64-bit CRC hash algorithm for a given polynomial.
    /// </summary>
    /// <remarks>
    /// For ISO 3309 compliant 64-bit CRC's use Crc64Iso.
    /// </remarks>
    public class Crc64 : HashAlgorithm
    {
        public const ulong DefaultSeed = 0x0;

        private readonly ulong[] _table;

        private readonly ulong _seed;
        private ulong _hash;

        public Crc64(ulong polynomial) : this(polynomial, DefaultSeed)
        {
        }

        public Crc64(ulong polynomial, ulong seed)
        {
            _table = InitializeTable(polynomial);
            _seed = _hash = seed;
        }

        public override void Initialize()
        {
            _hash = _seed;
        }

        protected override void HashCore(byte[] buffer, int start, int length)
        {
            _hash = CalculateHash(_hash, _table, buffer, start, length);
        }

        protected override byte[] HashFinal()
        {
            var hashBuffer = UInt64ToBigEndianBytes(_hash);
            HashValue = hashBuffer;
            return hashBuffer;
        }

        public override int HashSize => 64;

        protected static ulong CalculateHash(ulong seed, ulong[] table, IList<byte> buffer, int start, int size)
        {
            var crc = seed;

            for (var i = start; i < size; i++)
                unchecked
                {
                    crc = (crc >> 8) ^ table[(buffer[i] ^ crc) & 0xff];
                }

            return crc;
        }

        private static byte[] UInt64ToBigEndianBytes(ulong value)
        {
            var result = BitConverter.GetBytes(value);

            if (BitConverter.IsLittleEndian)
                Array.Reverse(result);

            return result;
        }

        private static ulong[] InitializeTable(ulong polynomial)
        {
            if (polynomial == Crc64Iso.Iso3309Polynomial && Crc64Iso.Table != null)
                return Crc64Iso.Table;

            var createTable = CreateTable(polynomial);

            if (polynomial == Crc64Iso.Iso3309Polynomial)
                Crc64Iso.Table = createTable;

            return createTable;
        }

        protected static ulong[] CreateTable(ulong polynomial)
        {
            var createTable = new ulong[256];
            for (var i = 0; i < 256; ++i)
            {
                var entry = (ulong)i;
                for (var j = 0; j < 8; ++j)
                    if ((entry & 1) == 1)
                        entry = (entry >> 1) ^ polynomial;
                    else
                        entry = entry >> 1;
                createTable[i] = entry;
            }
            return createTable;
        }
    }

    public class Crc64Iso : Crc64
    {
        internal static ulong[] Table;

        public const ulong Iso3309Polynomial = 0xD800000000000000;

        public Crc64Iso() : base(Iso3309Polynomial)
        {
        }

        public Crc64Iso(ulong seed) : base(Iso3309Polynomial, seed)
        {
        }

        public static ulong Compute(byte[] buffer)
        {
            return Compute(DefaultSeed, buffer);
        }

        public static ulong Compute(ulong seed, byte[] buffer)
        {
            if (Table == null)
                Table = CreateTable(Iso3309Polynomial);

            return CalculateHash(seed, Table, buffer, 0, buffer.Length);
        }
    }
}
