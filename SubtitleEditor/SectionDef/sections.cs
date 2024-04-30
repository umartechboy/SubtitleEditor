using System.Drawing.Drawing2D;
using System.Drawing;
using SkiaSharp;
using System.Collections;

namespace SubtitleEditor.SectionDef
{
    public class Clip : IDisposable
    {
        public string? Source { get; set; }
        int minZoom = 10;
        public bool selected = false;
        public delegate void OnDebugHandler(object sender, debugEventArgs e);
        public event OnDebugHandler OnDebug;
        void DEBUG(string str)
        {
            if (OnDebug != null)
                OnDebug(this, new debugEventArgs(str));
        }

        public int ZoomBarHeight { get => SectionBar.zsw; }
        public int sbh { get => SectionBar.sbh; }
        public int HeldComp = -3;
        // 0 is center hover, -1, left edge hover, +1 is right edge hover, -2 is left section out, 2 is right section out, 3 is out of region
        public int hoverOver = -3;
        Point curLoc = new Point();
        Point mDownOn = new Point();
        sectionSmall mDownSec = new sectionSmall();
        public delegate void InvalidateRef();
        public void MouseDown(InvalidateRef method)
        {
            HeldComp = hoverOver;
            mDownOn = new Point(curLoc.X, curLoc.Y);
            mDownSec = new sectionSmall(Start, End, false);
            method();
        }
        public void MouseUp()
        {
            HeldComp = -3;
        }
        public void MouseLeave(InvalidateRef method)
        {
            HeldComp = -3;
            hoverOver = -3;
            method();
        }
        RectangleF zsRec = new RectangleF();

        //some items need to be painted the grid is painted.
        public void OnPaintBefore(int layerIndex, int layersCount, double min, double max, int Width, int Height, Graphics g, SectionBarPart secType, double bMin, double bMax)
        {
            //this rect will be used for overview section
            Rectangle zsRec2 = new Rectangle();
            try
            {
                // drawing sections
                if (secType == SectionBarPart.Sections)
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

                    Color[] cNormal = new Color[] { Color.FromArgb(40, 233, 91, 50), Color.FromArgb(30, 81, 48, 235) };
                    Color[] cHover = new Color[] { Color.FromArgb(70, 233, 91, 50), Color.FromArgb(60, 81, 48, 235) };
                    Color[] cSelect = new Color[] { Color.FromArgb(220, 233, 91, 49), Color.FromArgb(180, 219, 45, 238) };
                    Color[] cHeld = new Color[] { Color.FromArgb(110, 233, 91, 49), Color.FromArgb(100, 219, 45, 238) };

                    Color c1 = cNormal[ID];
                    Color c2 = c1;
                    Color c3 = c1;

                    //hover colors
                    if (hoverOver == 1)
                        c3 = cHover[ID];
                    else if (hoverOver == -1)
                        c1 = cHover[ID];
                    else if (hoverOver == 0)
                    {
                        c1 = cHover[ID];
                        c2 = cHover[ID];
                        c3 = cHover[ID];
                    }


                    //held Colors
                    if (HeldComp == 0 && !selected)
                    {
                        c1 = cHeld[ID];
                        c2 = c1;
                        c3 = c1;
                    }
                    else if (HeldComp == -1)
                        c1 = cHeld[ID];
                    else if (HeldComp == 1)
                        c3 = cHeld[ID];

                    //select Colors
                    if (selected)
                    {
                        c2 = cSelect[ID];
                        if (HeldComp < -1 || HeldComp > 1)
                        {
                            c1 = cSelect[ID];
                            c3 = c1;
                        }

                    }
                    float frac = (float)Math.Max(10 / zsRec.Width, 0.02);
                    var positions = new[] { 0, frac, 1F - frac, 1f };
                    var colors = new Color[] { c1, c2, c2, c3 };
                    g.FillRectangle(colors, positions, zsRec.X, zsRec.Y, zsRec.Width, zsRec.Height);
                    //
                    g.DrawRectangle(
                        c2, 1,
                        zsRec2.X, zsRec2.Y, Math.Max(zsRec2.Width, 1), ZoomBarHeight);
                    g.FillRectangle(selected ? cSelect[ID] : cNormal[ID], zsRec2.X, zsRec2.Y, zsRec2.Width, ZoomBarHeight);
                }
                else if (secType == SectionBarPart.ZoomBar)
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
                else // seek bar
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
            }
            catch { }
            //g.FillRectangle((Brush)new SolidBrush(Color.FromArgb(30, 0, 255, 0)), zsRec);
        }
        public virtual async Task RenderAsync(double position, SKCanvas canvas, RenderConfig config)
        {

        }

        public void OnPaintAfter(Graphics g)
        {
            g.DrawRectangle(Color.FromArgb(44, 197, 70, 0), 2, zsRec);

        }
        public Cursor MouseMove(int layerIndex, int layersCount, Point e, double max, double min, int Width, int Height, Cursor c, InvalidateRef method, SectionBarPart secType, double bMin, double bigM)
        {
            int eToSs = (int)Math.Round(e.X * (max - min) / Width + min);
            int sToE = (int)Math.Round(((double)Start - min) / (max - min) * Width);
            int eToE = (int)Math.Round(((double)End - min) / (max - min) * Width);

            //determine where we are hovering
            // 0 is center hover, -1, left edge hover, +1 is right edge hover, -2 is left section out, 2 is right section out, 3 is out of region
            // seek bar cannot be -1 or 1
            if (HeldComp < -1 || HeldComp > 1)
            {
                int inTol = 4, outTol = 4;

                if (secType == SectionBarPart.SeekBar)
                {
                    // determine a hover section. dont consider y position yet
                    if (e.X >= sToE - 13 && e.X <= sToE + 13)
                    { c = Cursors.NoMoveHoriz; hoverOver = 0; }
                    else
                    {
                        if (e.X < sToE - 10)
                            hoverOver = -2;
                        else if (e.X > sToE + 10)
                            hoverOver = 2;
                        c = Cursors.Default;
                    }
                }
                else
                {
                    if (secType == SectionBarPart.ZoomBar)
                        inTol = ZoomBarHeight / 2;
                    // determine a hover section. dont consider y position yet
                    if (e.X >= sToE - outTol && e.X <= sToE + inTol)
                    {
                        c = Cursors.VSplit; hoverOver = -1;
                    }
                    else if (e.X >= eToE - inTol && e.X <= eToE + outTol)
                    { c = Cursors.VSplit; hoverOver = 1; }
                    else if (e.X >= sToE && e.X <= eToE)
                    { c = Cursors.NoMoveHoriz; hoverOver = 0; }
                    else
                    {
                        if (eToSs < Start)
                            hoverOver = -2;
                        else
                            hoverOver = 2;
                        c = Cursors.Default;
                    }
                }
                // y position correction
                if (secType == SectionBarPart.Sections)
                {
                    float layerHeight = (Height - ZoomBarHeight * 2) / (float)layersCount;
                    if (e.Y <= ZoomBarHeight * 2 + layerHeight * layerIndex ||
                        e.Y >= ZoomBarHeight * 2 + layerHeight * (layerIndex + 1))
                    {
                        c = Cursors.Default;
                        hoverOver = 3;
                    }
                }
                else if (secType == SectionBarPart.ZoomBar)
                {
                    if (e.Y > ZoomBarHeight)
                    {
                        c = Cursors.Default;
                        hoverOver = 3;
                    }
                }
                else
                {
                    if (e.Y < Height - sbh)
                    {
                        c = Cursors.Default;
                        hoverOver = 3;
                    }
                }

            }

            // already held
            if (HeldComp >= -1 && HeldComp <= 1)
            {
                // for length changing bars

                double minT = 1/30.0; // 30fps
                int movE = e.X - mDownOn.X;

                double mov = Math.Round(movE * ((double)max - min) / Width, 2);
                //seek bar cannot be -1 or 1, its 0, -2, 2, or 3
                if (HeldComp == 0) // draging
                {
                    // normalize mov
                    if (secType != SectionBarPart.SeekBar)
                    {
                        if (mDownSec.Start + mov < bMin) // too left
                            mov = bMin - mDownSec.Start;
                        else if (mDownSec.End + mov > bigM) // too right
                            mov = bigM - mDownSec.End;

                        Start = mDownSec.Start + mov;
                        End = mDownSec.End + mov;
                    }
                    else
                    {
                        if (mDownSec.Start + mov < bMin) // too left
                            mov = bMin - mDownSec.Start;
                        else if (mDownSec.Start + mov > bigM) // too right
                            mov = bigM - mDownSec.Start;

                        Start = mDownSec.Start + mov;
                    }
                }
                else if (HeldComp == -1) // not the case with seekbar
                {
                    if (mov < 0)//  increasing length to left
                    {
                        if (mDownSec.Start + mov < bMin) // too left
                            mov = bMin - mDownSec.Start;
                    }
                    else
                    {
                        if (secType == SectionBarPart.ZoomBar) // stop at a minimum length
                        {
                            int nSToE = (int)Math.Round(((double)mDownSec.Start + mov) / (max - min) * Width);
                            int nEndToE = (int)Math.Round((double)End / (max - min) * Width);
                            if (nEndToE - nSToE < minZoom)
                            {
                                mov = Math.Round((nEndToE - minZoom) * ((double)max - min) / Width, 1) - mDownSec.Start;
                            }
                        }
                        else //start at 0.100s
                        {
                            if (mDownSec.End - mDownSec.Start - mov < minT) // less than X sec
                                mov = mDownSec.End - minT - mDownSec.Start;
                            DEBUG((mDownSec.End - mDownSec.Start - mov).ToString());
                        }
                    }
                    Start = mDownSec.Start + mov;
                }
                else if (HeldComp == 1)
                {
                    if (mov > 0)//  increasing length to right
                    {
                        if (mDownSec.End + mov > bigM) // too right
                            mov = bigM - mDownSec.End;
                    }
                    else
                    {
                        if (secType == SectionBarPart.ZoomBar) // stop at a minimum length
                        {
                            int nSToE = (int)Math.Round((double)Start / (max - min) * Width);
                            int nEndToE = (int)Math.Round((double)(mDownSec.End + mov) / (max - min) * Width);

                            if (nEndToE - nSToE < minZoom)
                            {
                                mov = Math.Round(minZoom * ((double)max - min) / Width, 1) + Start - mDownSec.End;
                            }
                        }
                        else //start at 0.100s
                        {
                            if (mDownSec.End - mDownSec.Start + mov < minT) // less than X sec
                                mov = -mDownSec.End + minT + mDownSec.Start;
                        }
                    }
                    End = mDownSec.End + mov;
                }

            }

            method();
            curLoc = e;
            return c;
        }

        public void Dispose()
        {

        }

        public int ID { get; set; } = 0;
        public double Start { get; set; } = 0;
        public double End { get; set; } = 0;
        public Clip(double start, double end, string source = "", int id = 0)
        {
            Source = source;
            Start = start;
            End = end;
            ID = id;
        }
    }
    class sectionSmall
    {
        public double Start
        { get { return _v1; } set { _v1 = value; } }
        public double End { get { return _v2; } set { _v2 = value; } }
        public bool IsTrimSection { get { return _isTrimSection; } set { _isTrimSection = value; } }
        double _v1 = 0, _v2 = 0;
        bool _isTrimSection = false;
        public sectionSmall()
        {
            _v1 = 0;
            _v2 = 0;
            _isTrimSection = false;
        }
        public sectionSmall(double v1, double v2, bool isTrimSection)
        {
            _v1 = v1;
            _v2 = v2;
            _isTrimSection = isTrimSection;
        }
    }
    class Zone
    {
        public double Start
        { get { return _v1; } set { _v1 = value; } }
        public double End { get { return _v2; } set { _v2 = value; } }
        double _v1 = 0, _v2 = 0;
        public Zone()
        {
            _v1 = 0;
            _v2 = 0;
        }
        public Zone(double v1, double v2)
        {
            _v1 = v1;
            _v2 = v2;
        }
    }
}
