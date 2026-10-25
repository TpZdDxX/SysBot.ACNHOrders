using System;
using SkiaSharp;

namespace SysBot.Base;

public class RegionScreenshotComparer : IScreenshotComparer
{
    public readonly int X;

    public readonly int Y;

    public readonly int XSize;

    public readonly int YSize;

    public readonly SKColor ExpectedColor;

    public int DeadZone { get; set; }

    public RegionScreenshotComparer(int x, int y, int xSize, int ySize, SKColor expectedColor, int deadZone = 15)
    {
        X = x;
        Y = y;
        XSize = xSize;
        YSize = ySize;
        ExpectedColor = expectedColor;
        DeadZone = deadZone;
    }

    /// <summary>Averages the color of the region, ignoring alpha.</summary>
    public SKColor GetColorOfRect(SwitchScreenshot screen)
    {
        SKBitmap image = screen.SwitchImage;
        if (X < 0 || Y < 0 || XSize <= 0 || YSize <= 0 || X + XSize > image.Width || Y + YSize > image.Height)
            throw new ArgumentOutOfRangeException(nameof(screen), $"Region ({X}, {Y}, {XSize}x{YSize}) is outside the {image.Width}x{image.Height} screenshot.");

        int red = 0;
        int green = 0;
        int blue = 0;
        for (int x = X; x < X + XSize; x++)
        {
            for (int y = Y; y < Y + YSize; y++)
            {
                SKColor pixel = image.GetPixel(x, y);
                red += pixel.Red;
                green += pixel.Green;
                blue += pixel.Blue;
            }
        }

        int pixelCount = XSize * YSize;
        return new SKColor((byte)(red / pixelCount), (byte)(green / pixelCount), (byte)(blue / pixelCount));
    }

    public bool Compare(SwitchScreenshot screen)
    {
        SKColor color = GetColorOfRect(screen);
        return IsWithinDeadZone(ExpectedColor.Red, color.Red) && IsWithinDeadZone(ExpectedColor.Green, color.Green) && IsWithinDeadZone(ExpectedColor.Blue, color.Blue);
    }

    public int CompareRange(SwitchScreenshot screen)
    {
        SKColor color = GetColorOfRect(screen);
        return Math.Abs(ExpectedColor.Red - color.Red) + Math.Abs(ExpectedColor.Green - color.Green) + Math.Abs(ExpectedColor.Blue - color.Blue);
    }

    private bool IsWithinDeadZone(int expected, int actual) => Math.Max(expected - DeadZone, 0) <= actual && actual <= Math.Min(expected + DeadZone, 255);
}
