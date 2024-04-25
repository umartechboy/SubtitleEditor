using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Drawing;
using OpenTK.Graphics.OpenGL;
using SkiaSharp.Views.Blazor;

namespace SubtitleEditor.Pages.SectionDef
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
		public delegate void SectionModifyHandler(object sender, SectionsArgs e);
		public delegate void OnDebugHandler(object sender, debugEventArgs e);
		public delegate void TrimBarClickHandler(object sender, trimBarClickEventArgs e);
		public delegate void SectionsRemovedHandler(object sender, SectionsArgs e);
		public delegate void SectionsAddedHandler(object sender, SectionsArgs e);
		public delegate void SeekBarHandler(object sender, SeekBarEventArgs e);
		public event TimeSliceChangeHandler TimeSliceChanged;
		public event TrimBarClickHandler OnClick;
		public event SectionModifyHandler SectionModifyRequest;
		public event OnDebugHandler OnDebug;
		public event SectionsRemovedHandler SectionsRemoved;
		public event SectionsAddedHandler SectionsAdded;
		public event SeekBarHandler SeekPointChanged;
		bool ddess = false, ddss = false, ddsgs = false;
		// save the current hover section in hovSec. Update it on any mouse movements
		int hovSec = -1;
		public double ShowMin { get; set; } = 0;
		public double ShowMax { get; set; } = 120 * 60;
		public double Maximum { get; set; } = 120;
		public double Minimum { get; set; } = 0;
		section zoomSection;
		double lastPos = 0;
		section seekBar;
		public List<section> TimeSlices { get { return sections; } }
		public List<section> sections;
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
		public int Width { get; set; }
		public int Height { get; set; }
		public Cursor Cursor;
		// the default constructor.
		public SectionBar(Action invalidate)
		{
			this._invalidate = invalidate;
			Cursor = new Cursor(); // give it JS Interrop later
								   // override mouse click events and intercept others.
			this.MouseMove += trimBar_MouseMove;
			this.MouseEnter += trimBar_MouseEnter;
			this.MouseLeave += trimBar_MouseLeave;
			this.MouseDown += trimBar_MouseDown;
			//this.DoubleClick += trimBar_DoubleClick;
			this.MouseUp += trimBar_MouseUp;
			this.MouseClick += trimBar_MouseClick;
			// default values for min and max
			Minimum = 0;
			Maximum = 120;
			//default zoom section
			zoomSection = new section(Minimum, Maximum, 0);
			seekBar = new section(0, 0, 0);
			//init sections
			sections = new List<section>();
			//debug
			zoomSection.OnDebug += zoomSection_OnDebug;

		}

		public int HoverIndex { get { return hovSec; } }
		public int Count { get { return sections.Count; } }

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
				var reqWid = (double)(ZoomEnd - value) / (double)(Maximum - Minimum) * (double)Width;
				var minWid = (double)(minZoomE) * (double)(Maximum - Minimum) / (double)Width;

				if (reqWid < minZoomE)
					value = Math.Round(ZoomEnd - minWid, 2);

				if (value < Minimum)
					value = Minimum;

				zoomSection.Start = value;

				if (!skipUpdate)
				{
					MouseMove_ISR(new Point(65000, 65000));
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

				var reqWid = (double)(value - ZoomStart) / (double)(Maximum - Minimum) * (double)Width;
				var minWid = (double)(minZoomE) * (double)(Maximum - Minimum) / (double)Width;

				if (reqWid < minZoomE)
					value = Math.Round(ZoomStart + minWid, 2);

				if (value > Maximum)
					value = Maximum;

				zoomSection.End = value;
				if (!skipUpdate)
				{
					MouseMove_ISR(new Point(65000, 65000));
					Invalidate();
				}
			}
		}
		List<int> selSec
		{
			get
			{
				var ans = new List<int>();
				for (int i = 0; i < sections.Count; i++)
					if (sections[i].selected)
						ans.Add(i);
				return ans;
			}
		}
		void selSecClear()
		{
			for (int i = 0; i < sections.Count; i++)
				sections[i].selected = false;
		}
		//set selection. both in selSec and the real section
		public void SetSelection(params int[] inds)
		{
			if (!multiSel && inds.Length > 1)
				throw new Exception("Multi selection is disabled. Kindly enable it first.");
			// selSecClear();
			//aply changes
			for (int i = 0; i < sections.Count; i++)
			{
				sections[i].selected = ((IList<int>)inds).Contains(i);
			}
			Invalidate();
		}
		public List<int> SelectedIndices
		{
			get { return selSec; }
		}

		void DEBUG(string str)
		{
			if (OnDebug != null)
				OnDebug(this, new debugEventArgs(str));
		}

		//intervept the double click event and forward through OnClick
		void trimBar_DoubleClick(object sender, EventArgs e)
		{
			if (tp == SectionBarPart.SeekBar)
				return;
			// no event in case on zoom bar
			if (tp == SectionBarPart.ZoomBar && hovSec == 0)
			{
				skipUpdate = true;
				ZoomStart = Minimum;
				ZoomEnd = Maximum;
				skipUpdate = false;
				return;
			}
			else if (tp == SectionBarPart.ZoomBar) return;
			// no event in case of overview
			if ((tp == SectionBarPart.OverviewBar || tp == SectionBarPart.Sections) && hovSec >= 0)
			{
				skipUpdate = true;
				ZoomStart = sections[hovSec].Start;
				ZoomEnd = sections[hovSec].End;
				skipUpdate = false;
				return;
			}
			else if (tp == SectionBarPart.OverviewBar) return;

			//the rest of the code is executed only in case of sections
			// just update selection before calling the OnClick event.
			// set zoom selection to current hover section only. don't remove selection if multiple are selected already
			for (int i = 0; i < sections.Count; i++)
			{
				if (selSec.Count <= 1)
					sections[i].selected = i == hovSec;
				else
					sections[i].selected = sections[i].selected || i == hovSec;
			}
			Invalidate();

			if (OnClick != null)
			{
				OnClick(this, new trimBarClickEventArgs(hovSec, 1, 0, lastlocE, MouseButtons.Left, tp));
			}
		}

		// reroute mouse click to OnClick
		void trimBar_MouseClick(object sender, MouseEventArgs e)
		{
			trimBar_MouseClick_ISR(e);
		}
		bool LastClickWasOutSide = true;
		void trimBar_MouseClick_ISR(MouseEventArgs e)
		{
			//change selection if necessary

			if (tp == SectionBarPart.Sections)
			{
				int[] _ar = new int[selSec.Count];
				selSec.CopyTo(_ar, 0);
				List<int> selSecBkp = new List<int>(); selSecBkp.AddRange(_ar);
				if (hovSec >= 0)
				{
					if ((!multiSel) && (e.Button != MouseButtons.Right || selSec.Count == 1))
					{
						for (int i = 0; i < sections.Count; i++)
						{
							sections[i].selected = false;
						}
					}
				}
				else  // click on empty place in section bar
				{
					LastClickWasOutSide = true;
					//bool hadSel = selSec.Count > 0;
					for (int i = 0; i < sections.Count; i++)
					{ sections[i].selected = false; }
					//if (hadSel && SelectionChanged != null)
					//    SelectionChanged(this, new SectionsArgs(new List<int>()));
					if (OnClick != null) OnClick(this, new trimBarClickEventArgs(-1, e.Clicks, e.Delta, e.Location, e.Button, SectionBarPart.Sections));
				}
				// just update selection before calling the OnClick event.
				// set zoom selection to current hover section only. don't remove selection if multiple are selected already
				for (int i = 0; i < sections.Count; i++)
				{
					if (selSec.Count < 1)
					{
						sections[i].selected = i == hovSec;
					}
					else
					{
						sections[i].selected = sections[i].selected || i == hovSec;
					}
				}
				if (OnClick != null)
					OnClick(this, new trimBarClickEventArgs(hovSec, e.Clicks, e.Delta, e.Location, e.Button, tp));
				return;
			}
			if (tp == SectionBarPart.SeekBar && seekBar.hoverOver != 0)
			{
				float reqVal = (float)(e.X) / (float)Width * (float)(ShowMax - ShowMin) + (float)ShowMin;
				seekBar.Start = reqVal;
				Invalidate();
				if (SeekPointChanged != null)
					SeekPointChanged(this, new SeekBarEventArgs(seekBar.Start));
				if (OnClick != null)
					OnClick(this, new trimBarClickEventArgs(hovSec, 1, e.Delta, e.Location, e.Button, tp));
			}

		}
		//debug
		void zoomSection_OnDebug(object sender, debugEventArgs e)
		{
			DEBUG(e.Message);
		}

		void trimBar_MouseUp(object sender, MouseEventArgs e)
		{
			tpInProcess = SectionBarPart.None;
			if (tp == SectionBarPart.SeekBar)
			{
				SeekPointChanged?.Invoke(this, new SeekBarEventArgs(SeekPosition));
				seekBar.MouseUp();
				return;
			}
			if (e.Button == MouseButtons.Right)
				trimBar_MouseClick_ISR(e);

			zoomSection.MouseUp();
			seekBar.MouseUp();
			for (int i = 0; i < sections.Count; i++)
			{
				sections[i].MouseUp();
			}
			Invalidate();
		}

		void trimBar_MouseDown(object sender, MouseEventArgs e)
		{
			zoomSection.MouseDown(Invalidate);
			seekBar.MouseDown(Invalidate);
			for (int i = 0; i < sections.Count; i++)
			{ sections[i].MouseDown(Invalidate); }
			if (tp != SectionBarPart.None)
				tpInProcess = tp;
			MouseMove_ISR(e.Location);
		}

		void trimBar_MouseLeave(object sender, EventArgs e)
		{
			tpInProcess = SectionBarPart.None;
			zoomSection.MouseLeave(Invalidate);
			seekBar.MouseLeave(Invalidate);
			for (int i = 0; i < sections.Count; i++)
			{ sections[i].MouseLeave(Invalidate); }
			hovSec = -1;
			tp = SectionBarPart.None;
			DEBUG(hovSec.ToString() + ", " + selSec.ToString() + ", " + tp.ToString());
		}

		void trimBar_MouseEnter(object sender, EventArgs e)
		{
		}
		Point lastlocE = new Point();
		Point quickLast = new Point();
		void trimBar_MouseMove(object sender, MouseEventArgs e)
		{
			if (e.X != quickLast.X || e.Y != quickLast.Y)
			{
				MouseMove_ISR(e.Location);
				quickLast = e.Location;
			}
		}
		void MouseMove_ISR(Point e)
		{
			hovSec = -1;
			if (e.Y < zsw)
				tp = SectionBarPart.ZoomBar;
			else if (e.Y < 2 * zsw)
				tp = SectionBarPart.OverviewBar;
			else if (e.Y < Height - sbh)
				tp = SectionBarPart.Sections;
			else
				tp = SectionBarPart.SeekBar;
			if (tpInProcess != SectionBarPart.None)
				tp = tpInProcess;
			Cursor c = Cursor;
			int over = 0;
			if (zoomSection != null)
			{
				c = zoomSection.MouseMove(e, Maximum, Minimum, Width, Height, Cursor, Invalidate, SectionBarPart.ZoomBar, Minimum, Maximum);
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
					selSecClear();
			}
			Cursor c2 = this.seekBar.MouseMove(e, ShowMax, ShowMin, Width, Height, Cursor, Invalidate, SectionBarPart.SeekBar, Minimum, Maximum);
			if (tp == SectionBarPart.SeekBar)
				c = c2;

			// find local limits
			//if (tp == SectionBarPart.Sections && selSec.Count == 1) // already multi selection has started
			//    selSecClear();
			for (int i = 0; i < sections.Count; i++)
			{
				double secMinTemp = 0, secMaxTemp = Maximum;
				for (int j = 0; j < sections.Count; j++)
				{
					if (i == j) continue;
					if (sections[j].End <= sections[i].Start) // is on the left
					{
						if (secMinTemp < sections[j].End) //is more near to the corner
							secMinTemp = sections[j].End;
					}
					if (sections[j].Start >= sections[i].End) // is on the right
					{
						if (secMaxTemp > sections[j].Start) //is more near to the corner
							secMaxTemp = sections[j].Start;
					}
				}
				if (sections[i].hoverOver == 0 && tp == SectionBarPart.Sections) // has centerHover
					hovSec = i;
				if ((sections[i].HeldComp == 0 || sections[i].selected) && tp == SectionBarPart.Sections)
					sections[i].selected = true;
				c =
					sections[i].MouseMove(e, ShowMax, ShowMin, Width, Height, Cursor, Invalidate, SectionBarPart.Sections,
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
					double eToX = e.X / (double)Width * Maximum;
					if (eToX >= sections[i].Start && eToX <= sections[i].End)
						hovSec = i;
				}
			}
			if (over == 0)
				Cursor = Cursors.Default;
			lastlocE = new Point(e.X, e.Y);
			string str = "";
			foreach (var i in selSec)
				str += i.ToString() + " ";

			if (hovSec == -1)
				;
			Console.WriteLine("tp = " + tp + ", hovSec = " + hovSec);
		}
		public void OnPaint(SKPaintGLSurfaceEventArgs e)
		{
			var g = Graphics.FromCanvas(e.Surface.Canvas);

			//fill the sectionbar background with gradient
			g.FillRectangle(Color.FromArgb(139, 199, 175), 0, zsw * 2, Width, Height - zsw * 2 - sbh);
			g.FillRectangle(Color.FromArgb(200, 200, 200), 0, Height - sbh, Width, sbh);

			//calculate the grid
			int sResB = 1, sResS = 1;
			List<int> stops = new List<int>(new int[] { 1, 10, 30, 60, 120, 300, 600 });
			while ((ShowMax - ShowMin) / sResB * 25 > Width)
				sResB = stops[stops.IndexOf(sResB) + 1];

			while ((ShowMax - ShowMin) / sResS * 5 > Width)
				sResS = stops[stops.IndexOf(sResS) + 1];
			DEBUG(sResB.ToString() + ", " + sResS.ToString());
			g.FillRectangle(
				Color.FromArgb(200, 200, 200),
				0, zsw, Width, zsw);
			// all sections Background
			if (zoomSection != null)
				zoomSection.OnPaintBefore(Minimum, Maximum, Width, Height - sbh, g, SectionBarPart.ZoomBar, Minimum, Maximum);
			for (int i = 0; i < sections.Count; i++)
			{ sections[i].OnPaintBefore(ShowMin, ShowMax, Width, Height - sbh, g, SectionBarPart.Sections, Minimum, Maximum); }

			//draw the grid
			for (int i = (int)Math.Round(ShowMin); i < ShowMax; i++)
			{
				if (i % sResB == 0)
				{
					int x = (int)Math.Round((double)(i - ShowMin) / (ShowMax - ShowMin) * Width); string s = ((int)Math.Round((double)i / 60)).ToString();
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
					int x = (int)Math.Round((double)(i - ShowMin) / (ShowMax - ShowMin) * Width);

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
				seekBar.OnPaintBefore(ShowMin, ShowMax, Width, Height, g, SectionBarPart.SeekBar, Minimum, Maximum); ;
			for (int i = 0; i < sections.Count; i++)
			{ sections[i].OnPaintAfter(g); }
		}

	}
}