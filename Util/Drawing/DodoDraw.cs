using System.IO;
using SkiaSharp;

namespace SysBot.ACNHOrders
{
    public class DodoDraw
    {
        private string ImagePathTemplate { get; set; } = "dodo.png";
        private string FontPath { get; set; } = "dodo.ttf";
        private string ImagePathOutput => "current" + ImagePathTemplate;

        private readonly SKBitmap BaseImage;
        private readonly SKFont DodoFont;

        public DodoDraw(float fontPercentage = 100)
        {
            BaseImage = SKBitmap.Decode(ImagePathTemplate) ?? throw new IOException($"Unable to load {ImagePathTemplate}");
            var typeface = SKTypeface.FromFile(FontPath) ?? throw new IOException($"Unable to load {FontPath}");
            DodoFont = new SKFont(typeface, BaseImage.Height * 0.4f * (fontPercentage/100f));
        }

        public string Draw(string dodo)
        {
            using var img = BaseImage.Copy();
            using (var canvas = new SKCanvas(img))
            using (var paint = new SKPaint { Color = SKColors.White, IsAntialias = true })
            {
                // Centre the line vertically on the point, not just the glyphs
                var baseline = BaseImage.Height * 0.38f - (DodoFont.Metrics.Ascent + DodoFont.Metrics.Descent) / 2;
                canvas.DrawText(dodo, BaseImage.Width * 0.5f, baseline, SKTextAlign.Center, DodoFont, paint);
            }

            using (var data = img.Encode(SKEncodedImageFormat.Png, 100))
            using (var file = File.Create(ImagePathOutput))
            {
                data.SaveTo(file);
            }

            return ImagePathOutput;
        }

        public string? GetProcessedDodoImagePath()
        {
            if (File.Exists(ImagePathOutput))
                return ImagePathOutput;

            return null;
        }
    }
}
