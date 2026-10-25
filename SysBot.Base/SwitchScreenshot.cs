using System;
using SkiaSharp;

namespace SysBot.Base;

public sealed class SwitchScreenshot : IDisposable
{
    public SKBitmap SwitchImage { get; }

    public SwitchScreenshot(byte[] data)
    {
        SwitchImage = SKBitmap.Decode(data) ?? throw new ArgumentException("Screenshot data is not a supported image format.", nameof(data));
    }

    public void Dispose() => SwitchImage.Dispose();
}
