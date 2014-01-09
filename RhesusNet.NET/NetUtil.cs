using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RhesusNet.NET
{
    public class NetUtil
    {
        public static UInt32 PackFloat(float f)
        {
            // 32 bits; 1 bit sign, 8 bit exponent, 23 bit mantissa
            if (f == 0)
                return 0;

            int sign = Math.Sign(f);

            if (sign < 0) { f *= -1; sign = 0; }

            byte exponent = 0;

            while (f >= 2.0f) { f /= 2.0f; exponent++; }
            while (f < 1.0f) { f *= 2.0f; exponent--; }

            uint mantissa = (uint)(f * Math.Pow(2, 22) + 0.5f);

            mantissa &= (uint)(~(uint)0) >> 9;

            return ((uint)sign << (32 - 1)) | ((uint)exponent << (32 - 1 - 8)) | mantissa;
        }

        public static float UnpackFloat(UInt32 packed)
        {
            uint mantissa = (packed & (uint)(~(uint)0) >> 9);

            int sign = (packed & ((uint)1 << 31)) == 0 ? -1 : 1;

            float mantissaF = mantissa / (float)(Math.Pow(2, 22)) * sign;

            int exponent = (sbyte)((packed >> 23) & (~(uint)0 >> 24));

            return mantissaF * (float)Math.Pow(2, exponent);
        }

        public static UInt64 PackDouble(double d)
        {
            // 64 bits; 1 bit sign, 11 bit exponent, 52 bit mantissa
            if (d == 0)
                return 0;

            long sign = Math.Sign(d);

            if (sign < 0) { d *= -1; sign = 0; }

            short exponent = 0;

            while (d >= 2.0f) { d /= 2.0f; exponent++; }
            while (d < 1.0f) { d *= 2.0f; exponent--; }

            int expSign = Math.Sign(exponent);

            if (expSign < 0)
                exponent *= -1;

            uint uExp = (uint)exponent;

            uExp &= (uint)(~(uint)0) >> 5;

            exponent = (short)((int)uExp * expSign);

            ulong mantissa = (ulong)(d * Math.Pow(2, 22) + 0.5f);

            mantissa &= (ulong)(~(ulong)0) >> 12;

            return ((ulong)sign << (64 - 1)) | ((ulong)exponent << (64 - 1 - 11)) | mantissa;
        }

        public static double UnpackDouble(UInt64 packed)
        {
            ulong mantissa = (packed & ((ulong)(~(ulong)0) >> 12));

            int sign = (packed & ((ulong)1 << 63)) == 0 ? -1 : 1;

            double mantissaD = mantissa / (Math.Pow(2, 22)) * sign;

            int exponent = (short)((packed >> 52) & (~(ulong)0 >> 53));

            return mantissaD * Math.Pow(2, exponent);
        }
    }
}
