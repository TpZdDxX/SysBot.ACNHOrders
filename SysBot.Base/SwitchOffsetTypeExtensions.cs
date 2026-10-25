using System;
using System.Collections.Generic;

namespace SysBot.Base;

public static class SwitchOffsetTypeExtensions
{
    public static Func<ulong, int, byte[]> GetReadMethod(this SwitchOffsetType type, bool crlf = true)
    {
        if (1 == 0)
        {
        }
        Func<ulong, int, byte[]> result = type switch
        {
            SwitchOffsetType.Heap => (Func<ulong, int, byte[]>)((ulong o, int c) => SwitchCommand.Peek((uint)o, c, crlf)),
            SwitchOffsetType.Main => (ulong o, int c) => SwitchCommand.PeekMain(o, c, crlf),
            SwitchOffsetType.Absolute => (ulong o, int c) => SwitchCommand.PeekAbsolute(o, c, crlf),
            _ => throw new IndexOutOfRangeException("Invalid offset type."),
        };
        if (1 == 0)
        {
        }
        return result;
    }

    public static Func<IReadOnlyDictionary<ulong, int>, byte[]> GetReadMultiMethod(this SwitchOffsetType type, bool crlf = true)
    {
        if (1 == 0)
        {
        }
        Func<IReadOnlyDictionary<ulong, int>, byte[]> result = type switch
        {
            SwitchOffsetType.Heap => (Func<IReadOnlyDictionary<ulong, int>, byte[]>)((IReadOnlyDictionary<ulong, int> d) => SwitchCommand.PeekMulti(d, crlf)),
            SwitchOffsetType.Main => (IReadOnlyDictionary<ulong, int> d) => SwitchCommand.PeekMainMulti(d, crlf),
            SwitchOffsetType.Absolute => (IReadOnlyDictionary<ulong, int> d) => SwitchCommand.PeekAbsoluteMulti(d, crlf),
            _ => throw new IndexOutOfRangeException("Invalid offset type."),
        };
        if (1 == 0)
        {
        }
        return result;
    }

    public static Func<ulong, byte[], byte[]> GetWriteMethod(this SwitchOffsetType type, bool crlf = true)
    {
        if (1 == 0)
        {
        }
        Func<ulong, byte[], byte[]> result = type switch
        {
            SwitchOffsetType.Heap => (Func<ulong, byte[], byte[]>)((ulong o, byte[] b) => SwitchCommand.Poke((uint)o, b, crlf)),
            SwitchOffsetType.Main => (ulong o, byte[] b) => SwitchCommand.PokeMain(o, b, crlf),
            SwitchOffsetType.Absolute => (ulong o, byte[] b) => SwitchCommand.PokeAbsolute(o, b, crlf),
            _ => throw new IndexOutOfRangeException("Invalid offset type."),
        };
        if (1 == 0)
        {
        }
        return result;
    }
}
