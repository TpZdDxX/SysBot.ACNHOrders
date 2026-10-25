using System;

namespace SysBot.Base;

internal static class ArrayUtil
{
    public static byte[] SliceSafe(this byte[] src, int offset, int length)
    {
        int num = src.Length - offset;
        if (num < length)
        {
            length = num;
        }
        byte[] array = new byte[length];
        Buffer.BlockCopy(src, offset, array, 0, array.Length);
        return array;
    }
}
