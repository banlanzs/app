// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Buffers.Binary;

namespace Stratum.Core.Generator
{
    public class Totp : HmacOtp, IGenerator
    {
        private readonly int _period;

        public Totp(string secret, int period, HashAlgorithm algorithm, int digits) : base(secret, algorithm, digits)
        {
            _period = period;
        }

        public string Compute(long counter)
        {
            Span<byte> counterBytes = stackalloc byte[sizeof(long)];
            BinaryPrimitives.WriteInt64BigEndian(counterBytes, counter / _period);
            var material = base.Compute(counterBytes);
            return Finalise(material);
        }

        public static byte[] GetCounterBytes(long counter, int period)
        {
            var window = counter / period;
            var bytes = new byte[sizeof(long)];
            BinaryPrimitives.WriteInt64BigEndian(bytes, window);
            return bytes;
        }

        protected virtual string Finalise(int material)
        {
            return Truncate(material);
        }
    }
}