using System;

namespace SysBot.Base;

public static class Decoder
{
    private static bool IsNum(char c)
    {
        return (uint)(c - 48) <= 9u;
    }

    private static bool IsHexUpper(char c)
    {
        return (uint)(c - 65) <= 5u;
    }

    private static bool IsHexLower(char c)
    {
        return (uint)(c - 97) <= 5u;
    }

    public static byte[] ConvertHexByteStringToBytes(byte[] bytes)
    {
        byte[] array = new byte[bytes.Length / 2];
        for (int i = 0; i < array.Length; i++)
        {
            int num = i * 2;
            char _ = (char)bytes[num];
            char _2 = (char)bytes[num + 1];
            array[i] = DecodeTuple(_, _2);
        }
        return array;
    }

    private static byte DecodeTuple(char _0, char _1)
    {
        byte b;
        if (IsNum(_0))
        {
            b = (byte)(_0 - 48 << 4);
        }
        else if (IsHexUpper(_0))
        {
            b = (byte)(_0 - 65 + 10 << 4);
        }
        else
        {
            if (!IsHexLower(_0))
            {
                throw new ArgumentOutOfRangeException("_0");
            }
            b = (byte)(_0 - 97 + 10 << 4);
        }
        if (IsNum(_1))
        {
            return (byte)(b | (byte)(_1 - 48));
        }
        if (IsHexUpper(_1))
        {
            return (byte)(b | (byte)(_1 - 65 + 10));
        }
        if (IsHexLower(_1))
        {
            return (byte)(b | (byte)(_1 - 97 + 10));
        }
        throw new ArgumentOutOfRangeException("_1");
    }
}
