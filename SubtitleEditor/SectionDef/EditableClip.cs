using Microsoft.AspNetCore.Authorization;
using MudBlazor;
using SkiaSharp;
using System.Drawing;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SubtitleEditor.SectionDef
{
    public class AudioClip : Clip
    {
        public AudioClip(double start, double end, string source) : base(start, end, source)
        {
        }
    }
    public class VideoClip : Clip
    {
        public VideoClip(double start, double end, string source) : base(start, end, source)
        {
        }
    }
    public class PhotoClip : Clip
    {
        public PhotoClip(double start, double end, string source) : base(start, end, source)
        {
        }
    }
    public class SubtitleClip : Clip
    {
        public SubtitleClip(double start, double end, string source) : base(start, end, source)
        {
        }
        void DrawWrapLines(float x, float y, string longLine, float lineLengthLimit, SKCanvas canvas, SKPaint defPaint)
        {
            var wrappedStrings = new List<string>();
            var lineLength = 0f;
            var line = "";
            foreach (var word in longLine.Split(' ', '\n'))
            {
                var wordWithSpace = word + " ";
                var wordWithSpaceLength = defPaint.MeasureText(wordWithSpace);
                if (lineLength + wordWithSpaceLength > lineLengthLimit)
                {
                    wrappedStrings.Add(line.Trim());
                    line = "" + wordWithSpace;
                    lineLength = wordWithSpaceLength;
                }
                else
                {
                    line += wordWithSpace;
                    lineLength += wordWithSpaceLength;
                }
            }
            if (line.Length > 0)
                wrappedStrings.Add(line.Trim());

            // Now lets calculate the bounding rect of this line.

            var wrappedLines = new Dictionary<SKRect, string>();
            float dy = 0;
            foreach (var wrappedLine in wrappedStrings)
            {
                var skrect = new SKRect();
                defPaint.MeasureText(wrappedLine, ref skrect);
                skrect.Top += dy;
                skrect.Bottom += dy;
                dy += defPaint.FontSpacing;
                wrappedLines.Add(skrect, wrappedLine);
            }

            var totalHeight = defPaint.FontSpacing * wrappedStrings.Count;


            // Find the bounding rect Ys.
            var top = wrappedLines.Min(r => r.Key.Top);
            var bottom = wrappedLines.Max(r => r.Key.Bottom);
            var middle = (bottom + top) / 2;

            // The original rects want to get drawn from top to bottom.
            // To force the middle to go to 0, we need to add an offset
            var yOffset = - middle;
            // If we draw with this offset, bounding rect will be placed at 0
            // to force it to go to y, we just need to add y
            yOffset += y;
            // Now, we just need to draw at y = yoffset + d(line spacing) * i

            // lets draw now

            dy = 0;

            foreach (var wrappedLine in wrappedLines)
            {
                // Lets now calculate xOffset for centered placement at x

                var rc = wrappedLine.Key;
                canvas.DrawText(wrappedLine.Value, x - rc.Width / 2, yOffset + dy, defPaint);
                yOffset += defPaint.FontSpacing;
            }
        }
        public override void Render(double position, SKCanvas canvas, RenderConfig config)
        {
            if (Source.Trim().Length > 0 && position >= Start - config.SubtitleOverlap && position <= End + config.SubtitleOverlap)
            {
                DrawWrapLines(config.SubtitleLocation.X, config.SubtitleLocation.Y, Source, 90, canvas,
                new SKPaint(config.SubTitlesFont)
                {
                    Color = config.SubtitleColor,
                    IsAntialias = true
                });
            }
        }
    }
    public class RenderConfig
    {
        public SKSize TargetSize { get; set; }
        public double AspectRatio { get; set; }
        public SKFont SubTitlesFont { get; set; }
        public SKColor SubtitleColor { get; set; }

        public double SubtitleOverlap { get; set; }
        public SKPoint SubtitleLocation { get; set; }
    }
}
