using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Drawing;
using OpenTK.Graphics.OpenGL;
using SkiaSharp.Views.Blazor;
using SkiaSharp;
using Microsoft.JSInterop;

namespace SubtitleEditor.SectionDef
{
    public class debugEventArgs : EventArgs
    {
        string s = "";
        public string Message { get { return s; } }
        public debugEventArgs(string m)
        { s = m; }
    }
    public class SeekBarEventArgs : EventArgs
    {
        double d = 0;
        public double Value { get { return d; } }
        public SeekBarEventArgs(double v)
        { d = v; }
    }
    public class SectionsArgs : EventArgs
    {
        List<int> i = new List<int>();
        public List<int> Indices { get { return i; } }
        public SectionsArgs(List<int> inds)
        { i = inds; }
        public SectionsArgs(int[] inds)
        { foreach (var ind in inds) i.Add(ind); }
    }
    public class SectionArgs : EventArgs
    {
        int hovInd_ = -1;
        List<int> i = new List<int>();
        public int CurrentIndex { get { return hovInd_; } }
        public List<int> Indices { get { return i; } }
        public SectionArgs(List<int> inds, int hovInd)
        { i = inds; hovInd_ = hovInd; }
    }
    public class ClipArgs : EventArgs
    {
        public Clip Clip { get; set; }
    }
    public enum SectionBarPart
    {
        Sections,
        ZoomBar,
        OverviewBar,
        SeekBar,
        None
    }

    public class trimBarClickEventArgs : EventArgs
    {
        MouseButtons b;
        SectionBarPart tp_;
        Point p;

        int index = 0, d, c;
        public int Index { get { return index; } }
        public MouseButtons Button { get { return b; } }
        public SectionBarPart Part { get { return tp_; } }
        public int Delta { get { return d; } }
        public int Clicks { get { return c; } }
        public Point Location { get { return p; } }
        public int X { get { return p.X; } }
        public int Y { get { return p.Y; } }
        public trimBarClickEventArgs(int i, int c_, int d_, Point p_, MouseButtons b_, SectionBarPart tp)
        { index = i; c = c_; d = d_; b = b_; p = p_; tp_ = tp; }
    }

    public class SectionBar
    {
        bool skipUpdate = true, multiSel = false;
        int minZoomE = 10;
        //global zoom section and overview section height
        public static int zsw = 15;
        //global seekbar height
        public static int sbh = 15;
        // current trimBarPart under hover. Save value to be used in click events
        SectionBarPart tp;
        SectionBarPart tpInProcess = SectionBarPart.None;
        public delegate void TimeSliceChangeHandler(object sender, EventArgs e);
        public delegate void ClipSelectionHandler(object sender, ClipArgs e);
        public delegate void SectionModifyHandler(object sender, SectionsArgs e);
        public delegate void OnDebugHandler(object sender, debugEventArgs e);
        public delegate void TrimBarClickHandler(object sender, trimBarClickEventArgs e);
        public delegate void SectionsRemovedHandler(object sender, SectionsArgs e);
        public delegate void SectionsAddedHandler(object sender, SectionsArgs e);
        public delegate void SeekBarHandler(object sender, SeekBarEventArgs e);
        public event TimeSliceChangeHandler TimeSliceChanged;
        public event TrimBarClickHandler OnClick;
        public event ClipSelectionHandler OnClipEditRequest;
        public event SectionModifyHandler SectionModifyRequest;
        public event OnDebugHandler OnDebug;
        public event SectionsRemovedHandler SectionsRemoved;
        public event SectionsAddedHandler SectionsAdded;
        public event SeekBarHandler SeekPointChanged;
        public event EventHandler OnRequestToRenderPreview;
        public void ResetBounds()
        {
            MouseMove(this, new MouseEventArgs()
            {
                Location = new Point(
                LabelsSectionWidth + 10, zsw * 2 + 1)
            });
        }

		public List<List<Clip>> GetClipsToRender(double position)
        {
            return Layers;
        }
        public void ResetMaxTime()
        {
            foreach (var layer in Layers)
                foreach (var clip in layer)
                    if (clip.End > Maximum)
                    {
                        Maximum = clip.End;
                        ZoomEnd = clip.End;
                        Invalidate();
                    }
            
        }

		public async Task RenderFrameAsync(double position, SKCanvas canvas, RenderConfig config)
        {
            var ll = new List<List<Clip>>();
            ll.AddRange(Layers);
            ll.Reverse();
            foreach (var layer in ll)
                foreach (var clip in layer)
                    await clip.RenderAsync(position, canvas, config);
        }
        bool ddess = false, ddss = false, ddsgs = false;
        // save the current hover section in hovSec. Update it on any mouse movements
        int hovSec = -1;
        public double ShowMin { get; set; } = 0;
        public double ShowMax { get; set; } = 120 * 60;
        public double Maximum { get; set; } = 120;
        public double Minimum { get; set; } = 0;
        Clip zoomSection;
        double lastPos = 0;
        Clip seekBar;
        public List<List<Clip>> Layers;
        public event MouseEventHandler MouseEnter;
        public event MouseEventHandler MouseMove;
        public event MouseEventHandler MouseDown;
        public event MouseEventHandler MouseUp;
        public event MouseEventHandler MouseLeave;
        public event MouseEventHandler MouseClick;

        public void NotifyMouseEnter(MouseEventArgs e)
        {
            MouseEnter?.Invoke(this, e);
        }
        public void NotifyMouseMove(MouseEventArgs e)
        {
            MouseMove?.Invoke(this, e);
        }
        public void NotifyMouseDown(MouseEventArgs e)
        {
            MouseDown?.Invoke(this, e);
        }
        public void NotifyMouseUp(MouseEventArgs e)
        {
            MouseUp?.Invoke(this, e);
        }
        public void NotifyMouseLeave(MouseEventArgs e)
        {
            MouseLeave?.Invoke(this, e);
        }
        public void NotifyMouseClick(MouseEventArgs e)
        {
            MouseClick?.Invoke(this, e);
        }
        public void Invalidate()
        {
            // invoke skview invalidate
            _invalidate?.Invoke();
        }
        Action _invalidate;
        public int LabelsSectionWidth { get; set; } = 200;
        public int LayersSectionWidth
        {
            get => Width - 200;
            set => Width = value + 200;
        }
        public int Width { get; set; } = 400;
        public int Height { get; set; }
        public Cursor Cursor;
        // the default constructor.
        public SectionBar(Action invalidate, int layers)
        {
            _invalidate = invalidate;
                                   // override mouse click events and intercept others.
            MouseMove += OnMouseMove;
            MouseEnter += OnMouseEnter;
            MouseLeave += OnMouseLeave;
            MouseDown += OnMouseDown;
            //this.DoubleClick += trimBar_DoubleClick;
            MouseUp += OnMouseUp;
            MouseClick += OnMouseClick;
            // default values for min and max
            Minimum = 0;
            Maximum = 120;
            //default zoom section
            zoomSection = new ZoomBarClip(Minimum, Maximum);
            seekBar = new SeekBarClip(0);
            //init sections
            Layers = new List<List<Clip>>();
            for (int i = 0; i < layers; i++)
                Layers.Add(new List<Clip>());
            //debug
            zoomSection.OnDebug += zoomSection_OnDebug;

        }

        [DefaultValue("")]
        public bool DiableDefaultEmptySectionStrip { get { return ddess; } set { ddess = value; } }

        [DefaultValue("")]
        public bool DiableDefaultSectionStrip { get { return ddss; } set { ddss = value; } }

        [DefaultValue("")]
        public bool DiableDefaultSectionGroupStrip { get { return ddsgs; } set { ddsgs = value; } }


        [Description("The Value of seekbar")]
        [RefreshProperties(RefreshProperties.All)]
        public double SeekPosition { get { return seekBar.Start; } set { seekBar.Start = value; Invalidate(); } }

        [Description("Determine if multi-selection is enabled")]
        [RefreshProperties(RefreshProperties.All)]
        public bool MultiSelection { get { return multiSel; } set { multiSel = value; } }


        public double ZoomStart
        {
            get { return zoomSection.Start; }
            set
            {
                var reqWid = (double)(ZoomEnd - value) / (double)(Maximum - Minimum) * LayersSectionWidth;
                var minWid = minZoomE * (double)(Maximum - Minimum) / LayersSectionWidth;

                if (reqWid < minZoomE)
                    value = Math.Round(ZoomEnd - minWid, 2);

                if (value < Minimum)
                    value = Minimum;

                zoomSection.Start = value;

                if (!skipUpdate)
                {
                    ProcessLayersMouseMove(new Point(65000, 65000));
                    Invalidate();
                }
            }
        }

        [Description("ZoomBar end point")]
        [RefreshProperties(RefreshProperties.All)]
        public double ZoomEnd
        {
            get { return zoomSection.End; }
            set
            {

                var reqWid = (double)(value - ZoomStart) / (double)(Maximum - Minimum) * LayersSectionWidth;
                var minWid = minZoomE * (double)(Maximum - Minimum) / LayersSectionWidth;

                if (reqWid < minZoomE)
                    value = Math.Round(ZoomStart + minWid, 2);

                if (value > Maximum)
                    value = Maximum;

                zoomSection.End = value;
                if (!skipUpdate)
                {
                    ProcessLayersMouseMove(new Point(65000, 65000));
                    Invalidate();
                }
            }
        }
        List<int> GetSelectedSectionIndices(int layerIndex)
        {
            var ans = new List<int>();
            for (int i = 0; i < Layers[layerIndex].Count; i++)
                if (Layers[layerIndex][i].selected)
                    ans.Add(i);
            return ans;
        }
        void ClearSelectedSections(int layerIndex)
        {
            for (int i = 0; i < Layers[layerIndex].Count; i++)
                Layers[layerIndex][i].selected = false;
        }
        //set selection. both in selSec and the real section
        public void SetSelection(int layerIndex, params int[] inds)
        {
            if (!multiSel && inds.Length > 1)
                throw new Exception("Multi selection is disabled. Kindly enable it first.");
            // selSecClear();
            //aply changes
            for (int i = 0; i < Layers[layerIndex].Count; i++)
            {
                Layers[layerIndex][i].selected = ((IList<int>)inds).Contains(i);
            }
            Invalidate();
        }
        double lastSeekNotificationOn = -1;
        void NotifySeekChange()
        {
            if (lastSeekNotificationOn == SeekPosition)
                return;
            lastSeekNotificationOn = SeekPosition;
            if (SeekPointChanged != null)
                SeekPointChanged(this, new SeekBarEventArgs(seekBar.Start));
        }
        void DEBUG(string str)
        {
            if (OnDebug != null)
                OnDebug(this, new debugEventArgs(str));
        }
        bool LastClickWasOutSide = true;
        void ProcessLayersMouseClick(MouseEventArgs e)
        {
            //change selection if necessary

            float layerHeight = (Height - 2 * zsw - sbh) / (float)Layers.Count;
            var cursorLayerInd = (int)((e.Y - zsw * 2) / layerHeight);
            if (tp == SectionBarPart.Sections)
            {
				bool atLeastOneSelected = false;
				for (int layerIndex = 0; layerIndex < Layers.Count; layerIndex++)
                {
                    int[] _ar = new int[GetSelectedSectionIndices(layerIndex).Count];
                    GetSelectedSectionIndices(layerIndex).CopyTo(_ar, 0);
                    List<int> selSecBkp = new List<int>(); selSecBkp.AddRange(_ar);
                    if (hovSec >= 0)
                    {
                        if (!multiSel && (e.Button != MouseButtons.Right || GetSelectedSectionIndices(layerIndex).Count == 1))
                        {
                            for (int i = 0; i < Layers[layerIndex].Count; i++)
                            {
                                Layers[layerIndex][i].selected = false;
                            }
                        }
                    }
                    else  // click on empty place in section bar
                    {
                        LastClickWasOutSide = true;
                        //bool hadSel = selSec.Count > 0;
                        for (int i = 0; i < Layers[layerIndex].Count; i++)
                            Layers[layerIndex][i].selected = false;
                        //if (hadSel && SelectionChanged != null)
                        //    SelectionChanged(this, new SectionsArgs(new List<int>()));
                        if (OnClick != null) OnClick(this, new trimBarClickEventArgs(-1, e.Clicks, e.Delta, e.Location, e.Button, SectionBarPart.Sections));
                    }
                    // just update selection before calling the OnClick event.
                    // set zoom selection to current hover section only. don't remove selection if multiple are selected already
                    
                    for (int i = 0; i < Layers[layerIndex].Count; i++)
                    {
                        if (GetSelectedSectionIndices(layerIndex).Count < 1)
                        {
                            Layers[layerIndex][i].selected = i == hovSec && layerIndex == cursorLayerInd;
                            if (Layers[layerIndex][i].selected)
                            {
                                OnClipEditRequest?.Invoke(this, new ClipArgs() { Clip = Layers[layerIndex][i] });
                                atLeastOneSelected = true;

							}
                        }
                        else
                        {
                            Layers[layerIndex][i].selected = (Layers[layerIndex][i].selected || i == hovSec) && layerIndex == cursorLayerInd;
                            if (Layers[layerIndex][i].selected)
                            {
                                OnClipEditRequest?.Invoke(this, new ClipArgs() { Clip = Layers[layerIndex][i] });
                                atLeastOneSelected = true;

							}
                        }
                    }
                    if (OnClick != null)
                        OnClick(this, new trimBarClickEventArgs(hovSec, e.Clicks, e.Delta, e.Location, e.Button, tp));
				}
				if (!atLeastOneSelected)
				{
					OnClipEditRequest?.Invoke(this, new ClipArgs() { Clip = null });
				}
				return;
            }
            if (tp == SectionBarPart.SeekBar && seekBar.hoverOver != 0)
            {
                float reqVal = e.X / (float)LayersSectionWidth * (float)(ShowMax - ShowMin) + (float)ShowMin;
                seekBar.Start = reqVal;
                Invalidate();
                NotifySeekChange();
                if (OnClick != null)
                    OnClick(this, new trimBarClickEventArgs(hovSec, 1, e.Delta, e.Location, e.Button, tp));
            }
        }
        //debug
        void zoomSection_OnDebug(object sender, debugEventArgs e)
        {
            DEBUG(e.Message);
        }

        Point lastlocE = new Point();
        Point quickLast = new Point();
        // reroute mouse click to OnClick
        void OnMouseClick(object sender, MouseEventArgs e)
        {
            // Lets first process layer labels
            if (e.Location.X <= LabelsSectionWidth)
            {
                // Process Layers labels section
            }
            else // Process
            {
                // Offset the point
                e.Location = new Point(e.X - LabelsSectionWidth, e.Y);
                ProcessLayersMouseClick(e);
            }
        }
        void OnMouseUp(object sender, MouseEventArgs e)
        {
            // Lets first process layer labels


            // Offset the point
            e.Location = new Point(e.X - LabelsSectionWidth, e.Y);
            //Process Layer Data
            tpInProcess = SectionBarPart.None;
            if (tp == SectionBarPart.SeekBar)
            {
                NotifySeekChange();
                seekBar.MouseUp();
                return;
            }
            if (e.Button == MouseButtons.Right)
                ProcessLayersMouseClick(e);

            zoomSection.MouseUp();
            seekBar.MouseUp();
            for (int layerIndex = 0; layerIndex < Layers.Count; layerIndex++)
                for (int i = 0; i < Layers[layerIndex].Count; i++)
                {

                    if (Layers[layerIndex][i].hoverOver == -1 || Layers[layerIndex][i].hoverOver == 1)
                    {
                        if(Layers[layerIndex][i].selected)
                            OnClipEditRequest(this, new ClipArgs() { Clip = Layers[layerIndex][i] });
                        OnRequestToRenderPreview?.Invoke(this, EventArgs.Empty);
					}
					Layers[layerIndex][i].MouseUp();
                }
            Invalidate();
        }

        void OnMouseDown(object sender, MouseEventArgs e)
        {
            // Lets first process layer labels
            if (e.Location.X <= LabelsSectionWidth)
            {
                // Process Layers labels section
            }
            else // Process
            {
                // Offset the point
                e.Location = new Point(e.X - LabelsSectionWidth, e.Y);
                //Process Layer Data
                zoomSection.MouseDown(Invalidate);
                seekBar.MouseDown(Invalidate);
                for (int layerIndex = 0; layerIndex < Layers.Count; layerIndex++)
                    for (int i = 0; i < Layers[layerIndex].Count; i++)
                        Layers[layerIndex][i].MouseDown(Invalidate);
                if (tp != SectionBarPart.None)
                    tpInProcess = tp;
                ProcessLayersMouseMove(e.Location);
            }
        }

        void OnMouseLeave(object sender, EventArgs e)
        {
            tpInProcess = SectionBarPart.None;
            zoomSection.MouseLeave(Invalidate);
            seekBar.MouseLeave(Invalidate);
            for (int layerIndex = 0; layerIndex < Layers.Count; layerIndex++)
                for (int i = 0; i < Layers[layerIndex].Count; i++)
                    Layers[layerIndex][i].MouseLeave(Invalidate);
            hovSec = -1;
            tp = SectionBarPart.None;
            //DEBUG(hovSec.ToString() + ", " + GetSelectedSectionIndices.ToString() + ", " + tp.ToString());
        }

        void OnMouseEnter(object sender, EventArgs e)
        {
        }
        void OnMouseMove(object sender, MouseEventArgs e)
        {
            // Lets first process layer labels
            if (e.Location.X <= LabelsSectionWidth)
            {
                // Process Layers labels section
            }
            else // Process
            {
                // Offset the point
                e.Location = new Point(e.X - LabelsSectionWidth, e.Y);
                if (e.X != quickLast.X || e.Y != quickLast.Y)
                {
                    ProcessLayersMouseMove(e.Location);
                    quickLast = e.Location;
                }
            }
        }
        void ProcessLayersMouseMove(Point p)
        {
            hovSec = -1;
            if (p.Y < zsw)
                tp = SectionBarPart.ZoomBar;
            else if (p.Y < 2 * zsw)
                tp = SectionBarPart.OverviewBar;
            else if (p.Y < Height - sbh)
                tp = SectionBarPart.Sections;
            else
                tp = SectionBarPart.SeekBar;
            if (tpInProcess != SectionBarPart.None)
                tp = tpInProcess;
            Cursor c = Cursor;
            int over = 0;
            if (zoomSection != null)
            {
                c = zoomSection.MouseMove(-1, Layers.Count, p, Maximum, Minimum, LayersSectionWidth, Height, Cursor, Invalidate, Minimum, Maximum);
                ShowMin = ZoomStart;
                ShowMax = ZoomEnd;
                if (c != Cursors.Default)
                {
                    Cursor = c;
                    over++;
                }
                if (zoomSection.hoverOver == 0 && tp == SectionBarPart.ZoomBar)
                    hovSec = 0;
                if (zoomSection.HeldComp == 0 && tp == SectionBarPart.ZoomBar)
                    for (int layerIndex = 0; layerIndex < Layers.Count; layerIndex++)
                        ClearSelectedSections(layerIndex);
            }
            Cursor c2 = seekBar.MouseMove(-1, Layers.Count, p, ShowMax, ShowMin, LayersSectionWidth, Height, Cursor, Invalidate, Minimum, Maximum);
            if (tp == SectionBarPart.SeekBar)
                c = c2;

            // find local limits
            //if (tp == SectionBarPart.Sections && selSec.Count == 1) // already multi selection has started
            //    selSecClear();
            for (int layerIndex = 0; layerIndex < Layers.Count; layerIndex++)
                for (int i = 0; i < Layers[layerIndex].Count; i++)
                {
                    double secMinTemp = 0, secMaxTemp = Maximum;
                    for (int j = 0; j < Layers[layerIndex].Count; j++)
                    {
                        if (i == j) continue;
                        if (Layers[layerIndex][j].End <= Layers[layerIndex][i].Start) // is on the left
                        {
                            if (secMinTemp < Layers[layerIndex][j].End) //is more near to the corner
                                secMinTemp = Layers[layerIndex][j].End;
                        }
                        if (Layers[layerIndex][j].Start >= Layers[layerIndex][i].End) // is on the right
                        {
                            if (secMaxTemp > Layers[layerIndex][j].Start) //is more near to the corner
                                secMaxTemp = Layers[layerIndex][j].Start;
                        }
                    }
                    if (Layers[layerIndex][i].hoverOver == 0 && tp == SectionBarPart.Sections) // has centerHover
                        hovSec = i;
                    if ((Layers[layerIndex][i].HeldComp == 0 || Layers[layerIndex][i].selected) && tp == SectionBarPart.Sections)
                        Layers[layerIndex][i].selected = true;
                    c =
                        Layers[layerIndex][i].MouseMove(layerIndex, Layers.Count, p, ShowMax, ShowMin, LayersSectionWidth, Height, Cursor, Invalidate,
                        secMinTemp,
                        secMaxTemp);

                    if (c != Cursors.Default)
                    {
                        over++;
                        if (over == 1)
                            Cursor = c;
                        else
                            Cursor = Cursors.IBeam;
                    }

                    if (hovSec == -1 && tp == SectionBarPart.OverviewBar) // check if the mouse is in  overview bar
                    {
                        double eToX = p.X / (double)LayersSectionWidth * Maximum;
                        if (eToX >= Layers[layerIndex][i].Start && eToX <= Layers[layerIndex][i].End)
                            hovSec = i;
                    }
                }
            if (over == 0)
                Cursor = Cursors.Default;
            lastlocE = new Point(p.X, p.Y);
            string str = "";
            for (int layerIndex = 0; layerIndex < Layers.Count; layerIndex++)
                foreach (var i in GetSelectedSectionIndices(layerIndex))
                    str += i.ToString() + " ";

            NotifySeekChange();
            //Console.WriteLine("tp = " + tp + ", hovSec = " + hovSec);
        }
        public void OnPaint(SKPaintGLSurfaceEventArgs e)
        {
            var g = Graphics.FromCanvas(e.Surface.Canvas);
            float layerHeight = (Height - zsw * 2 - sbh) / (float)Layers.Count;
            // Draw layer labels section
            g.FillRectangle(Color.DarkGray, 0, zsw * 2, LabelsSectionWidth, Height - zsw * 2 - sbh);

            for (int layersIndex = 0; layersIndex <= Layers.Count; layersIndex++)
            {
                // draw layer separators
                g.DrawLine(Color.FromArgb(130, Color.White), 1, 0, zsw * 2 - 1 + layerHeight * layersIndex, LabelsSectionWidth, zsw * 2 + layerHeight * layersIndex);
            }

            // Draw Layers now
            e.Surface.Canvas.Translate(LabelsSectionWidth, 0);
            try
            {
                //fill the sectionbar background
                g.FillRectangle(Color.FromArgb(139, 199, 175), 0, zsw * 2, LayersSectionWidth, Height - zsw * 2 - sbh);
                g.FillRectangle(Color.FromArgb(200, 200, 200), 0, Height - sbh, LayersSectionWidth, sbh);

                for (int layersIndex = 0; layersIndex <= Layers.Count; layersIndex++)
                {
                    // draw layer separators
                    g.DrawLine(Color.FromArgb(130, Color.White), 1, 0, zsw * 2 - 1 + layerHeight * layersIndex, LayersSectionWidth, zsw * 2 + layerHeight * layersIndex);
                }

                //calculate the grid
                int sResB = 1, sResS = 1;
                List<int> stops = new List<int>(new int[] { 1, 10, 30, 60, 120, 300, 600 });
                while ((ShowMax - ShowMin) / sResB * 25 > LayersSectionWidth)
                    sResB = stops[stops.IndexOf(sResB) + 1];

                while ((ShowMax - ShowMin) / sResS * 5 > LayersSectionWidth)
                    sResS = stops[stops.IndexOf(sResS) + 1];
                DEBUG(sResB.ToString() + ", " + sResS.ToString());
                g.FillRectangle(
                    Color.FromArgb(200, 200, 200),
                    0, zsw, LayersSectionWidth, zsw);
                // all sections Background
                if (zoomSection != null)
                    zoomSection.OnPaintBefore(-1, Layers.Count, Minimum, Maximum, LayersSectionWidth, Height - sbh, g, Minimum, Maximum);

                for (int layerIndex = 0; layerIndex < Layers.Count; layerIndex++)
                    for (int i = 0; i < Layers[layerIndex].Count; i++)
                        Layers[layerIndex][i].OnPaintBefore(layerIndex, Layers.Count, ShowMin, ShowMax, LayersSectionWidth, Height - sbh, g, Minimum, Maximum);

                //draw the grid
                for (int i = (int)Math.Round(ShowMin); i < ShowMax; i++)
                {
                    if (i % sResB == 0)
                    {
                        int x = (int)Math.Round((double)(i - ShowMin) / (ShowMax - ShowMin) * LayersSectionWidth); string s = ((int)Math.Round((double)i / 60)).ToString();
                        var bigL = Color.Black;
                        if (i % 60 != 0)
                        {
                            int j = i / 60;
                            s = "A";
                            s = Math.Round((double)j).ToString() + ":" + Math.Round((double)i % 60).ToString();
                            bigL = Color.FromArgb(30, 30, 30);
                        }

                        g.DrawLine(
                            bigL, 1,
                            x, zsw * 2, x, (int)(Height * 0.2) + zsw * 2
                            );
                        g.DrawLine(
                            bigL, 1,
                            x, (int)(Height * 0.8) - sbh, x, Height - sbh
                            );

                        float sw = g.MeasureString(s, "Consolas", 7).Width;
                        g.DrawString(s, "Consolas", 7, bigL, new PointF(x - sw / 2, (float)(Height * 0.2) + zsw * 2));
                    }
                    if (sResB != sResS && i % sResS == 0 && i % sResB != 0)
                    {
                        int x = (int)Math.Round((double)(i - ShowMin) / (ShowMax - ShowMin) * LayersSectionWidth);

                        g.DrawLine(
                            Color.FromArgb(50, 30, 30, 30), 1,
                            x, zsw * 2, x, (int)(Height * 0.1) + zsw * 2
                            );
                        g.DrawLine(
                            Color.FromArgb(50, 30, 30, 30), 1,
                            x, (int)(Height * 0.9) - sbh, x, Height - sbh
                            );
                    }
                }

                // all sections Foreground
                if (zoomSection != null)
                    zoomSection.OnPaintAfter(g);

                if (seekBar != null)
                    seekBar.OnPaintBefore(-1, Layers.Count, ShowMin, ShowMax, LayersSectionWidth, Height, g, Minimum, Maximum); ;

                for (int layerIndex = 0; layerIndex < Layers.Count; layerIndex++)
                    for (int i = 0; i < Layers[layerIndex].Count; i++)
                        Layers[layerIndex][i].OnPaintAfter(g);
            }
            catch
            { }

            e.Surface.Canvas.Translate(-LabelsSectionWidth, 0);
        }

    }
}