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
		public System.Drawing.Color Color { get; protected set; }
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
        protected RectangleF zsRec = new RectangleF();

        //some items need to be painted the grid is painted.
        public virtual void OnPaintBefore(int layerIndex, int layersCount, double min, double max, int Width, int Height, Graphics g, SectionBarPart secType, double bMin, double bMax)
        {
            // Handled by chlid classes
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
