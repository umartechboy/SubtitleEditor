using System.Drawing.Drawing2D;
using System.Drawing;
using SkiaSharp;
using System.Collections;

namespace SubtitleEditor.SectionDef
{
    public class Clip : IDisposable
    {
        public string? Source { get; set; }
        public string? Label { get; set; }
        protected int minZoom = 10;
        public bool selected = false;
        public delegate void OnDebugHandler(object sender, debugEventArgs e);
        public event OnDebugHandler OnDebug;
		public System.Drawing.Color Color { get; protected set; }
		protected void DEBUG(string str)
        {
            if (OnDebug != null)
                OnDebug(this, new debugEventArgs(str));
        }

        public int ZoomBarHeight { get => SectionBar.zsw; }
        public int sbh { get => SectionBar.sbh; }
        public int HeldComp = -3;
        // 0 is center hover, -1, left edge hover, +1 is right edge hover, -2 is left section out, 2 is right section out, 3 is out of region
        public int hoverOver = -3;
        protected Point curLoc = new Point();
        protected Point mDownOn = new Point();
        protected sectionSmall mDownSec = new sectionSmall();
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
        public virtual void OnPaintBefore(int layerIndex, int layersCount, double min, double max, int Width, int Height, Graphics g, double bMin, double bMax)
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
        public virtual Cursor MouseMove(int layerIndex, int layersCount, Point e, double max, double min, int Width, int Height, Cursor c, InvalidateRef method, double bMin, double bigM)
        {
            // Handled in child classes
            throw new NotImplementedException();
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
    public class sectionSmall
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
}
