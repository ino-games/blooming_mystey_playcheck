using System;
using System.Collections.Generic;
using System.Text;

namespace Service.Logic
{
    public static class Maths
    {
        // *******************************************************************************
        public const Int32 LOGICMATHS_BIT_WIDTH = 5;
        public const Int32 LOGICMATHS_BIT_HEIGHT = 3;


        // *******************************************************************************
        public static void ConvertMaskBitToXY(Int32 pos, ref Int32 x, ref Int32 y)
        {
            Int32 nx = pos % LOGICMATHS_BIT_WIDTH;
            Int32 ny = pos / LOGICMATHS_BIT_WIDTH;

            x = nx;
            y = ny;

        }

        // *******************************************************************************
        public static Int32 ConvertXYToMask(Int32 x, Int32 y)
        {
            return (y * LOGICMATHS_BIT_WIDTH) + x;
        }

        // *******************************************************************************
        public static void SetBit(ref Int32 destPattern, Int32 x, Int32 y)
        {
            Int32 n = ConvertXYToMask(x, y);
            destPattern |= (1 << n);
        }

        // *******************************************************************************
        public static void ClearBit(ref Int32 destPattern, Int32 x, Int32 y)
        {
            Int32 n = ConvertXYToMask(x, y);
            destPattern &= ~(1 << n);
        }

        // *******************************************************************************
        public static void SetSingleBit(ref Int32 destPattern, Int32 n)
        {
            destPattern |= (1 << n);
        }

        // *******************************************************************************
        public static void ClearSingleBit(ref Int32 destPattern, Int32 n)
        {
            destPattern &= ~(1 << n);
        }

        // *******************************************************************************
        public static void ClearAllBits(ref Int32 destPattern)
        {
            destPattern = 0;
        }

        // *******************************************************************************
        public static void SetAllBits(ref Int32 destPattern)
        {
            destPattern = -1;
        }

        // *******************************************************************************
        public static void ToggleBit(ref Int32 destPattern, Int32 x, Int32 y)
        {
            Int32 n = ConvertXYToMask(x, y);
            destPattern ^= (1 << n);
        }

        // *******************************************************************************
        public static bool IsBitSet(Int32 destPattern, Int32 x, Int32 y)
        {
            Int32 n = ConvertXYToMask(x, y);
            if (((destPattern) & (1 << n)) != 0)
            {
                return true;
            }

            return false;
        }

        // *******************************************************************************
        public static bool IsSingleBitSet(Int32 destPattern, Int32 n)
        {
            if (((destPattern) & (1 << n)) != 0)
            {
                return true;
            }

            return false;
        }


    }
}
