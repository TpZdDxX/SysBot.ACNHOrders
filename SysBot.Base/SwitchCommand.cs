using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SysBot.Base;

public static class SwitchCommand
{
    private static readonly Encoding Encoder = Encoding.ASCII;

    private static byte[] Encode(string command, bool crlf = true)
    {
        if (crlf)
        {
            command += "\r\n";
        }
        return Encoder.GetBytes(command);
    }

    public static byte[] DetachController(bool crlf = true)
    {
        return Encode("detachController", crlf);
    }

    public static byte[] Configure(SwitchConfigureParameter p, int ms, bool crlf = true)
    {
        return Encode($"configure {p} {ms}", crlf);
    }

    public static byte[] Click(SwitchButton button, bool crlf = true)
    {
        return Encode($"click {button}", crlf);
    }

    public static byte[] Hold(SwitchButton button, bool crlf = true)
    {
        return Encode($"press {button}", crlf);
    }

    public static byte[] Release(SwitchButton button, bool crlf = true)
    {
        return Encode($"release {button}", crlf);
    }

    public static byte[] SetStick(SwitchStick stick, short x, short y, bool crlf = true)
    {
        return Encode($"setStick {stick} {x} {y}", crlf);
    }

    public static byte[] ResetStick(SwitchStick stick, bool crlf = true)
    {
        return SetStick(stick, 0, 0, crlf);
    }

    public static byte[] TypeKey(HidKeyboardKey key, bool crlf = true)
    {
        return Encode($"key {(int)key}", crlf);
    }

    public static byte[] TypeMultipleKeys(IEnumerable<HidKeyboardKey> keys, bool crlf = true)
    {
        return Encode(string.Concat("key", string.Concat(keys.Select((HidKeyboardKey z) => $" {(int)z}"))), crlf);
    }

    public static byte[] Peek(uint offset, int count, bool crlf = true)
    {
        return Encode($"peek 0x{offset:X8} {count}", crlf);
    }

    public static byte[] PeekMulti(IReadOnlyDictionary<ulong, int> offsetSizeDictionary, bool crlf = true)
    {
        return Encode(string.Concat("peekMulti", string.Concat(offsetSizeDictionary.Select((KeyValuePair<ulong, int> z) => $" 0x{z.Key:X16} {z.Value}"))), crlf);
    }

    public static byte[] Poke(uint offset, byte[] data, bool crlf = true)
    {
        return Encode($"poke 0x{offset:X8} 0x{string.Concat(data.Select((byte z) => $"{z:X2}"))}", crlf);
    }

    public static byte[] PeekAbsolute(ulong offset, int count, bool crlf = true)
    {
        return Encode($"peekAbsolute 0x{offset:X16} {count}", crlf);
    }

    public static byte[] PeekAbsoluteMulti(IReadOnlyDictionary<ulong, int> offsetSizeDictionary, bool crlf = true)
    {
        return Encode(string.Concat("peekAbsoluteMulti", string.Concat(offsetSizeDictionary.Select((KeyValuePair<ulong, int> z) => $" 0x{z.Key:X16} {z.Value}"))), crlf);
    }

    public static byte[] PokeAbsolute(ulong offset, byte[] data, bool crlf = true)
    {
        return Encode($"pokeAbsolute 0x{offset:X16} 0x{string.Concat(data.Select((byte z) => $"{z:X2}"))}", crlf);
    }

    public static byte[] PeekMain(ulong offset, int count, bool crlf = true)
    {
        return Encode($"peekMain 0x{offset:X16} {count}", crlf);
    }

    public static byte[] PeekMainMulti(IReadOnlyDictionary<ulong, int> offsetSizeDictionary, bool crlf = true)
    {
        return Encode(string.Concat("peekMainMulti", string.Concat(offsetSizeDictionary.Select((KeyValuePair<ulong, int> z) => $" 0x{z.Key:X16} {z.Value}"))), crlf);
    }

    public static byte[] PokeMain(ulong offset, byte[] data, bool crlf = true)
    {
        return Encode($"pokeMain 0x{offset:X16} 0x{string.Concat(data.Select((byte z) => $"{z:X2}"))}", crlf);
    }

    public static byte[] PointerPeek(IEnumerable<long> jumps, int count, bool crlf = true)
    {
        return Encode($"pointerPeek {count}{string.Concat(jumps.Select((long z) => $" {z}"))}", crlf);
    }

    public static byte[] PointerPoke(IEnumerable<long> jumps, byte[] data, bool crlf = true)
    {
        return Encode(string.Concat("pointerPoke 0x", string.Concat(data.Select((byte z) => $"{z:X2}")), string.Concat(jumps.Select((long z) => $" {z}"))), crlf);
    }

    public static byte[] PointerAll(IEnumerable<long> jumps, bool crlf = true)
    {
        return Encode(string.Concat("pointerAll", string.Concat(jumps.Select((long z) => $" {z}"))), crlf);
    }

    public static byte[] PointerRelative(IEnumerable<long> jumps, bool crlf = true)
    {
        return Encode(string.Concat("pointerRelative", string.Concat(jumps.Select((long z) => $" {z}"))), crlf);
    }

    public static byte[] GetMainNsoBase(bool crlf = true)
    {
        return Encode("getMainNsoBase", crlf);
    }

    public static byte[] GetHeapBase(bool crlf = true)
    {
        return Encode("getHeapBase", crlf);
    }

    public static byte[] GetTitleID(bool crlf = true)
    {
        return Encode("getTitleID", crlf);
    }

    public static byte[] GetBuildID(bool crlf = true)
    {
        return Encode("getBuildID", crlf);
    }

    public static byte[] SetScreen(ScreenState state, bool crlf = true)
    {
        return Encode("screen" + ((state == ScreenState.On) ? "On" : "Off"), crlf);
    }

    public static byte[] PixelPeek(bool crlf = true)
    {
        return Encode("pixelPeek", crlf);
    }
}
