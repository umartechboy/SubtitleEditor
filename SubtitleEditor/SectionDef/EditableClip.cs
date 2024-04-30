using FFmpegBlazor;
using Microsoft.AspNetCore.Authorization;
using MudBlazor;
using SkiaSharp;
using System.Drawing;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SubtitleEditor.SectionDef
{
    public class AudioClip : Clip
    {
        public byte[] Data { get; private set; }
        public AudioClip(double start, double end, byte[] data, string fname) : base(start, end, fname)
        {
            Data = data;
			this.Color = System.Drawing.Color.FromArgb(30, 168, 150);
		}

	}
    public class VideoClip : Clip
	{
		public VideoClip(double start, double end) : base(start, end, "")
		{
			this.Color = System.Drawing.Color.FromArgb(255, 113, 91);
		}
		public HybridSKBitmap[]? Data { get; set; }
		public float Size { get; set; } = 100;
		public float X { get; set; } = 50;
		public float Y { get; set; } = 50 * 9 / 16.0F;
		public float fps { get; set; } = 30;
		public override async Task RenderAsync(double position, SKCanvas canvas, RenderConfig config)
		{
			if (Data != null && position >= Start && position <= End)
			{
				var fractionTimeToRender = (position - this.Start) / (End - Start);
				var indexToRender = (int)Math.Round((Data.Length - 1) * fractionTimeToRender);

                var bmp = await Data[indexToRender].GetSKBimap();

                float aspect = bmp.Width / (float)bmp.Height;
				
				var wid = /* We are gonna force fit */ 100 * (/* User Scale */Size / 100);
				var hei = wid / aspect;
				var x = X - wid / 2;
				var y = Y - hei / 2;
                RectangleF r = new RectangleF(x, y, wid, hei);
				canvas.DrawBitmap(bmp, new SKRect(r.Left, r.Top, r.Right, r.Bottom));
			}
		}
	}
    public class PhotoClip : Clip
    {
        public SKBitmap Data { get; set; }
        public float Size { get; set; } = 100;
        public float X { get; set; } = 50;
        public float Y { get; set; } = 50 * 9 / 16.0F;
        public PhotoClip(double start, double end) : base(start, end, "")
        {
			this.Color = System.Drawing.Color.FromArgb(120, 0, 0);
        }
        public override async Task RenderAsync(double position, SKCanvas canvas, RenderConfig config)
        {
            if (Data != null && position >= Start && position <= End)
            {
                float aspect = Data.Width / (float)Data.Height;
                var wid = /* We are gonna force fit */ 100 * (/* User Scale */Size / 100);
                var hei = wid / aspect;
                var x = X - wid / 2;
                var y = Y - hei / 2;
                RectangleF r = new RectangleF(x, y, wid, hei);
                canvas.DrawBitmap(Data, new SKRect(r.Left, r.Top, r.Right, r.Bottom));
            }
        }
    }
    public class SubtitleClip : Clip
    {
        public SubtitleClip(double start, double end, string source) : base(start, end, source)
        {
			this.Color = System.Drawing.Color.FromArgb(255, 190, 11);
        }
        void DrawWrapLines(float x, float y, string longLine, float lineLengthLimit, SKCanvas canvas, SKPaint defPaint, RenderConfig config)
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
                if (config.ShadowSize > 0)
                    defPaint.ImageFilter = SKImageFilter.CreateDropShadow(config.ShadowDistance, config.ShadowDistance, config.ShadowSize, config.ShadowSize, config.ShadowColor);
                canvas.DrawText(wrappedLine.Value, x - rc.Width / 2, yOffset + dy, defPaint);
                yOffset += defPaint.FontSpacing;
            }
        }
        public override async Task RenderAsync(double position, SKCanvas canvas, RenderConfig config)
        {
            if (Source?.Trim().Length > 0 && position >= Start - config.SubtitleOverlap && position <= End + config.SubtitleOverlap)
            {
                float opacity = 1;
                if (position <= Start)
                    opacity = (float)((position - (Start - config.SubtitleOverlap)) / config.SubtitleOverlap);
                else if (position > End)
                    opacity = (float)(((End + config.SubtitleOverlap) - position) / config.SubtitleOverlap);
                DrawWrapLines(config.SubtitleLocation.X, config.SubtitleLocation.Y, Source, 90, canvas,
                new SKPaint(config.SubTitlesFont)
                {
                    Color = config.SubtitleColor.WithAlpha((byte)(255 * opacity)),
                    IsAntialias = true
                }, config);
            }
        }
        //public class PNGFrame
        //{
        //    SKBitmap data;
        //    public SKBitmap Image
        //    {
        //        get
        //        {
        //            if (data == null)
        //                try
        //                {
        //                    SKBitmap.Decode()
        //                }
        //                catch { }
        //            return data;
        //        }
        //    }
        //    string file = "";
        //    byte[] fileData;
        //    public PNGFrame(FFmpegBlazor.FFMPEG ffmpeg, byte[] dataBuffer)
        //    {
        //        this.file = fileName;
        //        dataBuffer =
        //    }
        //}
    }
    public class RenderConfig
    {
        public SKSize TargetSize { get; set; }
        public float AspectRatio { get; set; }
        public float ShadowSize { get; set; }
        public float ShadowDistance { get; set; }
        public SKFont SubTitlesFont { get; set; }
        public SKColor SubtitleColor { get; set; }
		public SKColor ShadowColor { get; set; }

        public float SubtitleOverlap { get; set; } = 0.2F;
        public SKPoint SubtitleLocation { get; set; }
    }
    public class HybridSKBitmap 
    {
        public SKBitmap? Bitmap { get; set;}
        public string? FFMpegFile { get; set; }
        public FFMPEG? FFMpeg { get; set; }
        public HybridSKBitmap(string fName, FFMPEG fFMPEG)
        {
            FFMpegFile = fName;
            FFMpeg = fFMPEG;
        }
        public void Free()
        {
            Bitmap?.Dispose();
            Bitmap = null;
            GC.Collect();
        }
        public async Task<SKBitmap> GetSKBimap()
        {
            if (Bitmap == null)
            {
                try
                {
                    var frame = await FFMpeg.ReadFile(FFMpegFile);
                    var bmp = SKBitmap.Decode(frame);
                    Bitmap = bmp;
                }
                catch (Exception ex)
                {
                }
            }
            return Bitmap;
        }
    }
}
