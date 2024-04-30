using FFmpegBlazor;
using Microsoft.AspNetCore.Authorization;
using MudBlazor;
using SkiaSharp;
using System.Drawing;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Color = System.Drawing.Color;

namespace SubtitleEditor.SectionDef
{
    public class SeekBarClip : Clip
    {
        public SeekBarClip(double start):base(start, start)
        {

        }
		public override void OnPaintBefore(int layerIndex, int layersCount, double min, double max, int Width, int Height, Graphics g, SectionBarPart secType, double bMin, double bMax)
		{
            try
            {
                float bX = (float)(Start - bMin) / (float)(bMax - bMin) * Width;
                float x = (float)(Start - min) / (float)(max - min) * Width;
                double h1 = ZoomBarHeight * 2, h2 = Height - sbh, h3 = Height;
                Color c = Color.FromArgb(120, 0, 0, 0);
                Color cp = c;
                Color cf = Color.FromArgb(80, 30, 160, 40);
                if (hoverOver == 0)
                {
                    cf = Color.FromArgb(140, 30, 160, 40);
                    c = Color.FromArgb(230, 0, 0, 0);
                    cp = c;
                }
                if (HeldComp == 0)
                {
                    cf = Color.FromArgb(160, 9, 159, 232);
                    c = Color.FromArgb(120, 255, 255, 255);
                    cp = Color.FromArgb(255, 0, 0, 0);
                }
                g.FillPolygon(cp, new PointF[]{
                    new PointF(x, Height - sbh),
                    new PointF(x - sbh/2, Height),
                    new PointF (x+sbh/2, Height)});

                // The seekbar
                g.DrawRectangle(cp, 1, x - 10, Height - sbh, 20, sbh, 5);
                g.FillRectangle(cf, x - 10, Height - sbh, 20, sbh, 5);
                g.DrawLine(cf, 4, x, ZoomBarHeight * 2, x, Height - ZoomBarHeight);
                g.DrawLine(cp, 2, x, ZoomBarHeight * 2, x, Height - ZoomBarHeight);


                g.DrawPolygon(cp, 1F, new PointF[]{
                    new PointF(bX,2*ZoomBarHeight),
                    new PointF(bX - ZoomBarHeight/2, ZoomBarHeight),
                    new PointF (bX+ZoomBarHeight/2, ZoomBarHeight)});

            }
            catch { }
		}
	}
	public class ZoomBarClip : Clip
	{
		public ZoomBarClip(double start, double end) : base(start, end)
		{

		}
        //some items need to be painted the grid is painted.
        public override void OnPaintBefore(int layerIndex, int layersCount, double min, double max, int Width, int Height, Graphics g, SectionBarPart secType, double bMin, double bMax)
        {
            try
            {
                //calculate the rectangle.
                // for zoom section, min is always 0. max is equal to the length of the trimBar
                // Width is not same as max. Width is the graphical length.
                zsRec = new Rectangle(
                      (int)Math.Round(((double)Start - min) / (max - min) * Width),
                      0,
                      (int)Math.Round(((double)End - Start) / (max - min) * Width),
                      ZoomBarHeight);
                Color c1 = Color.FromArgb(180, 20, 20, 20);
                Color c2 = Color.FromArgb(255, 120, 120, 120);
                Color c3 = c1;
                int edgeWidth = ZoomBarHeight / 2;
                if (hoverOver == -1)
                    c1 = Color.FromArgb(180, 80, 80, 80);
                if (hoverOver == 0)
                    c2 = Color.FromArgb(255, 160, 160, 160);
                if (hoverOver == 1)
                    c3 = Color.FromArgb(180, 60, 60, 60);
                if (HeldComp == -1)
                    c1 = Color.FromArgb(180, 200, 200, 200);
                if (HeldComp == 0)
                    c2 = Color.FromArgb(255, 200, 200, 200);
                if (HeldComp == 1)
                    c3 = Color.FromArgb(180, 200, 200, 200);
                g.FillRectangle(Color.FromArgb(240, 240, 240), 0, 0, Width, zsRec.Height);
                g.FillEllipse(
                    c1,
                    zsRec.X, zsRec.Y, 2 * edgeWidth, zsRec.Height
                    );
                g.FillEllipse(
                    c3,
                    zsRec.X + zsRec.Width - edgeWidth * 2, zsRec.Y, edgeWidth * 2, zsRec.Height
                    );
                g.FillRectangle(
                    c2,
                    zsRec.X + edgeWidth, zsRec.Y, zsRec.Width - edgeWidth * 2, zsRec.Height
                    );
                zsRec = new Rectangle();
            }
            catch { }
            //g.FillRectangle((Brush)new SolidBrush(Color.FromArgb(30, 0, 255, 0)), zsRec);
        }
	}
    public class LayerClip : Clip
    {
        protected LayerClip(double start, double end, string source = ""):base(start, end, source)
        {

        }
        public override void OnPaintBefore(int layerIndex, int layersCount, double min, double max, int Width, int Height, Graphics g, SectionBarPart secType, double bMin, double bMax)
        {
            //this rect will be used for overview section
            Rectangle zsRec2 = new Rectangle();
            try
            {
                //calculate the rectangle.
                // for zoom section, min/max is got from iterating all other sections. 
                // End of first section on the left is min. if there is no section, min of Start of zoom Bar is min
                // Start of first section on the right is min. if there is no section, max of Start of zoom Bar is max
                // Width is not same as max. Width is the graphical length.
                // Stays the same for all layers
                float layerHeight = (Height - ZoomBarHeight * 2) / (float)layersCount;
                zsRec = new RectangleF(
                    (int)Math.Round(((double)Start - min) / (max - min) * Width),
                    ZoomBarHeight * 2 + layerHeight * layerIndex,
                    (int)Math.Round(((double)End - Start) / (max - min) * Width),
                    layerHeight - 1);
                //
                zsRec2 = new Rectangle(
                    (int)Math.Round(((double)Start - bMin) / (bMax - bMin) * Width),
                    ZoomBarHeight,
                    (int)Math.Round(((double)End - Start) / (bMax - bMin) * Width),
                    ZoomBarHeight);

                Color cNormal = Color.FromArgb(50, this.Color);
                Color cHover = Color.FromArgb(80, this.Color);
                Color cSelect = Color.FromArgb(160, this.Color);
                Color cHeld = Color.FromArgb(120, this.Color);


                Color c1 = cNormal;
                Color c2 = c1;
                Color c3 = c1;

                //hover colors
                if (hoverOver == 1)
                    c3 = cHover;
                else if (hoverOver == -1)
                    c1 = cHover;
                else if (hoverOver == 0)
                {
                    c1 = cHover;
                    c2 = cHover;
                    c3 = cHover;
                }


                //held Colors
                if (HeldComp == 0 && !selected)
                {
                    c1 = cHeld;
                    c2 = c1;
                    c3 = c1;
                }
                else if (HeldComp == -1)
                    c1 = cHeld;
                else if (HeldComp == 1)
                    c3 = cHeld;

                //select Colors
                if (selected)
                {
                    c2 = cSelect;
                    if (HeldComp < -1 || HeldComp > 1)
                    {
                        c1 = cSelect;
                        c3 = c1;
                    }

                }
                float frac = (float)Math.Max(30 / zsRec.Width, 0.02);
                var positions = new[] { 0, frac, 1F - frac, 1f };
                var colors = new Color[] { c1, c2, c2, c3 };
                g.FillRectangle(colors, positions, zsRec.X, zsRec.Y, zsRec.Width, zsRec.Height);
                //
                g.DrawRectangle(
                    c2, 1,
                    zsRec2.X, zsRec2.Y, Math.Max(zsRec2.Width, 1), ZoomBarHeight);
                g.FillRectangle(selected ? cSelect : cNormal, zsRec2.X, zsRec2.Y, zsRec2.Width, ZoomBarHeight);

            }
            catch { }
            //g.FillRectangle((Brush)new SolidBrush(Color.FromArgb(30, 0, 255, 0)), zsRec);
        }
	}
	public class AudioClip : LayerClip
    {
        public byte[] Data { get; private set; }
        public AudioClip(double start, double end, byte[] data, string fname) : base(start, end, fname)
        {
            Data = data;
			this.Color = System.Drawing.Color.FromArgb(30, 168, 150);
		}

	}
    public class VideoClip : LayerClip
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
    public class PhotoClip : LayerClip
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
    public class SubtitleClip : LayerClip
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
