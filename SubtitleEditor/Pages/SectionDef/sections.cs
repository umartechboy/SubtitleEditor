using System.Drawing.Drawing2D;
using System.Drawing;
using SkiaSharp;

namespace SubtitleEditor.Pages.SectionDef
{
	public class RegionCollection
	{
		public int FindPosition(double position)
		{
			for (int i = 0; i < Count; i++)
				if (position >= secs[i].Start && position < secs[i].End)
					return i;
			return -1;
		}

		public int NearestForward(double position)
		{
			double dist = 100000;
			int retInd = -1;
			int thisInd = FindPosition(position);
			for (int i = 0; i < Count; i++)
			{
				if (secs[i].Start >= position && i != thisInd)
				{
					if (secs[i].Start - position < dist)
					{
						dist = secs[i].Start - position;
						retInd = i;
					}
				}
			}
			return retInd;
		}

		public int NearestBackwards(double position)
		{
			double dist = 100000;
			int retInd = -1;
			int thisInd = FindPosition(position);
			for (int i = 0; i < Count; i++)
			{
				if (secs[i].End <= position && i != thisInd)
				{
					if (position - secs[i].End < dist)
					{
						dist = position - secs[i].End;
						retInd = i;
					}
				}
			}
			return retInd;
		}
		public List<section> secs = new List<section>();
		public void InsertCollection(RegionCollection collection)
		{
			for (int i = 0; i < collection.Count; i++)
				this.Insert(collection[i]);
			if (method != null) method();
		}
		public void Insert(Region region)
		{
			secs.Add(new section((double)region.Start, (double)region.End, region.ID, region.Motor1Speed, region.Motor2Speed, region.Motor3Speed, region.Motor4Speed));
			if (method != null) method();
		}
		public void Clear()
		{
			secs.Clear();
			if (method != null) method();
		}
		public void Insert(int start, int duration, int id, float motor1Speed_, float motor2Speed_, float motor3Speed_, float motor4Speed_)
		{
			secs.Add(new section((double)start, (double)start + duration, id, motor1Speed_, motor2Speed_, motor3Speed_, motor4Speed_));
			if (method != null) method();
		}
		public int Count { get { return secs.Count; } }
		public void RemoveAt(int index)
		{
			secs.RemoveAt(index);
			if (method != null) method();
		}
		public void RemoveRange(params int[] indices)
		{
			List<Region> regs = new List<Region>();
			foreach (var i in indices)
			{
				regs.Add(new Region((int)Math.Round(secs[i].Start), (int)Math.Round(secs[i].End), secs[i].ID, secs[i].Motor1Speed, secs[i].Motor2Speed, secs[i].Motor3Speed, secs[i].Motor4Speed));
			}
			foreach (var r in regs)
			{
				this.Remove(r);
			}
		}

		public void Remove(Region region)
		{
			secs.RemoveAt(IndexOf(region));
			if (method != null) method();
		}
		public int IndexOf(Region region, int searchStart = 0, bool tolerateID = false)
		{
			for (int i = searchStart; i < secs.Count; i++)
			{
				if (Math.Round(secs[i].Start) == region.Start &&
					Math.Round(secs[i].End) == region.End &&
					(secs[i].ID == region.ID || tolerateID))
				{
					return i;
				}
			}
			return -1;
		}
		public int IndexOf(string stringRegion)
		{
			return IndexOf(new Region(stringRegion), 0, true);
		}
		public Region this[int i]
		{
			get { return new Region((int)Math.Round(secs[i].Start), (int)Math.Round(secs[i].End), secs[i].ID, secs[i].Motor1Speed, secs[i].Motor2Speed, secs[i].Motor3Speed, secs[i].Motor4Speed); }
		}
		Action method;
		public RegionCollection(Action invalidate = null)
		{
			method = invalidate;
		}

		public RegionCollection Clone()
		{
			var bkpRegs = new RegionCollection();
			bkpRegs = new RegionCollection();
			for (int i = 0; i < Count; i++)
			{
				bkpRegs.Insert((int)Math.Round(secs[i].Start), (int)Math.Round(secs[i].End - secs[i].Start), secs[i].ID, secs[i].Motor1Speed, secs[i].Motor2Speed, secs[i].Motor3Speed, secs[i].Motor4Speed);
			}
			return bkpRegs;
		}
		public bool IsSame(RegionCollection collection, bool tolerateID = false)
		{
			for (int i = 0; i < collection.Count; i++)
			{
				if (!this.Contains(collection[i], tolerateID))
					return false;
			}
			if (this.Count != collection.Count)
				return false;
			return true;
		}
		public bool Contains(Region region, bool tolerateID = false)
		{
			return IndexOf(region, 0, tolerateID) >= 0;
		}
	}
	public class Region
	{
		double st = 0, en = 0;
		public int id;
		public float m1s = 0, m2s = 0, m3s = 0, m4s = 0;
		public float Motor1Speed
		{
			get
			{
				return m1s;
			}
			set
			{
				m1s = value;
			}
		}
		public float Motor2Speed { get { return m2s; } set { m2s = value; } }
		public float Motor3Speed { get { return m3s; } set { m3s = value; } }
		public float Motor4Speed { get { return m4s; } set { m4s = value; } }
		public double Start { get { return st; } set { if (value > en) value = en; st = value; } }
		public double Length { get { return End - Start; } }
		public double End { get { return en; } set { if (value < st) value = st; en = value; } }
		public int ID { get { return id; } set { id = value; } }
		public Region(double start, double end, int id_, float m1s_, float m2s_, float m3s_, float m4s_)
		{
			st = start; en = end; id = id_; m1s = m1s_; m2s = m2s_; m3s = m3s_; m4s = m4s_;
		}
		public Region(string str)
		{
			str = str.ToLower();
			var prts = str.Split(new char[] { ' ', ',', '[', ']' }, StringSplitOptions.RemoveEmptyEntries);

			st = common.stringToSeconds(prts[4]);
			en = common.stringToSeconds(prts[5]);
			Motor1Speed = (float)Convert.ToDouble(prts[0].Substring(1));
			Motor2Speed = (float)Convert.ToDouble(prts[1].Substring(1));
			Motor3Speed = (float)Convert.ToDouble(prts[2].Substring(1));
			Motor4Speed = (float)Convert.ToDouble(prts[3].Substring(1));
		}
		public override string ToString()
		{
			return String.Format("M{0} N{1} O{2} P{3} [{4}, {5}]",
				Motor1Speed,
				Motor2Speed,
				Motor3Speed,
				Motor4Speed,
				common.timeToString((int)Math.Round(Start)),
				common.timeToString((int)Math.Round(End)));
		}
	}
	public class section
	{
		float m1s = 0, m2s = 0, m3s = 0, m4s = 0;
		int minZoom = 10;
		public bool selected = false;
		public delegate void OnDebugHandler(object sender, debugEventArgs e);
		public event OnDebugHandler OnDebug;
		void DEBUG(string str)
		{
			if (OnDebug != null)
				OnDebug(this, new debugEventArgs(str));
		}

		int _id = 0;
		public int zbw { get => SectionBar.zsw; }
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
			mDownSec = new sectionSmall(this.Start, this.End, false);
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
		Rectangle zsRec = new Rectangle();

		//some items need to be painted the grid is painted.
		public void OnPaintBefore(double min, double max, int Width, int Height, Graphics g, SectionBarPart secType, double bMin, double bMax)
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
					zsRec = new Rectangle(
						(int)Math.Round(((double)this.Start - min) / (max - min) * Width),
						zbw * 2 + 1,
						(int)Math.Round(((double)this.End - this.Start) / (max - min) * Width),
						Height - zbw * 2 - 1);
					//
					zsRec2 = new Rectangle(
						(int)Math.Round(((double)this.Start - bMin) / (bMax - bMin) * Width),
						zbw,
						(int)Math.Round(((double)this.End - this.Start) / (bMax - bMin) * Width),
						zbw);

					LinearGradientBrush b1 = new LinearGradientBrush(
						new Rectangle(zsRec.X - 1, zsRec.Y, zsRec.Width + 2, zsRec.Height),
						Color.Black, Color.Black, 0, false);
					ColorBlend cb = new ColorBlend();

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
					float frac = (float)Math.Max((float)10 / zsRec.Width, 0.02);
					var positions = new[] { 0, frac, 1F - frac, 1f };
					var colors = new Color[] { c1, c2, c2, c3 };
					b1.InterpolationColors = cb;
					g.FillRectangle(colors, positions, zsRec.X, zsRec.Y, zsRec.Width, zsRec.Height);
					//
					g.DrawRectangle(
						c2, 1,
						zsRec2.X, zsRec2.Y, Math.Max(zsRec2.Width, 1), zbw);
					g.FillRectangle(selected ? cSelect[ID] : cNormal[ID], zsRec2.X, zsRec2.Y, zsRec2.Width, zbw);
				}
				else if (secType == SectionBarPart.ZoomBar)
				{
					//calculate the rectangle.
					// for zoom section, min is always 0. max is equal to the length of the trimBar
					// Width is not same as max. Width is the graphical length.
					zsRec = new Rectangle(
						  (int)Math.Round(((double)this.Start - min) / (max - min) * Width),
						  0,
						  (int)Math.Round(((double)this.End - this.Start) / (max - min) * Width),
						  zbw);
					Color c1 = Color.FromArgb(180, 20, 20, 20);
					Color c2 = Color.FromArgb(255, 120, 120, 120);
					Color c3 = c1;
					int edgeWidth = zbw / 2;
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
						zsRec.X + edgeWidth, zsRec.Y, zsRec.Width - (edgeWidth) * 2, zsRec.Height
						);
					zsRec = new Rectangle();
				}
				else // seek bar
				{
					float bX = (float)(Start - bMin) / (float)(bMax - bMin) * (float)Width;
					float x = (float)(Start - min) / (float)(max - min) * (float)Width;
					double h1 = zbw * 2, h2 = Height - sbh, h3 = Height;
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
					g.DrawLine(cf, 4, x, zbw * 2, x, Height - zbw);
					g.DrawLine(cp, 2, x, zbw * 2, x, Height - zbw);


					g.DrawPolygon(cp, 1F, new PointF[]{
					new PointF(bX,2*zbw),
					new PointF(bX - zbw/2, zbw),
					new PointF (bX+zbw/2, zbw)});

				}
			}
			catch { }
			//g.FillRectangle((Brush)new SolidBrush(Color.FromArgb(30, 0, 255, 0)), zsRec);
		}
		public void OnPaintAfter(Graphics g)
		{
			g.DrawRectangle(Color.FromArgb(44, 197, 70, 0), 2, zsRec);

		}
		public Cursor MouseMove(Point e, double max, double min, int Width, int Height, Cursor c, InvalidateRef method, SectionBarPart secType, double bMin, double bigM)
		{
			int eToSs = (int)Math.Round((double)e.X * (max - min) / Width + min);
			int sToE = (int)Math.Round(((double)this.Start - min) / (max - min) * Width);
			int eToE = (int)Math.Round(((double)this.End - min) / (max - min) * Width);

			//determine where we are hovering
			// 0 is center hover, -1, left edge hover, +1 is right edge hover, -2 is left section out, 2 is right section out, 3 is out of region
			// seek bar cannot be -1 or 1
			if (HeldComp < -1 || HeldComp > 1)
			{
				int inTol = 1, outTol = 4;

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
					// determine a hover section. dont consider y position yet
					if (e.X >= sToE - outTol && e.X <= sToE + inTol)
					{
						c = Cursors.VSplit; hoverOver = -1;
						if (e.X >= eToE - inTol && e.X <= eToE + outTol) // also +1
						{ c = Cursors.NoMoveHoriz; hoverOver = 0; }
					}
					else if (e.X >= eToE - inTol && e.X <= eToE + outTol)
					{ c = Cursors.VSplit; hoverOver = 1; }
					else if (eToSs >= this.Start && eToSs <= this.End)
					{ c = Cursors.NoMoveHoriz; hoverOver = 0; }
					else
					{
						if (eToSs < this.Start)
							hoverOver = -2;
						else
							hoverOver = 2;
						c = Cursors.Default;
					}
				}
				// y position correction
				if (secType == SectionBarPart.Sections)
				{
					if (e.Y <= zbw * 2 || e.Y >= Height - sbh)
					{
						c = Cursors.Default;
						hoverOver = 3;
					}
				}
				else if (secType == SectionBarPart.ZoomBar)
				{
					if (e.Y > zbw)
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

				double minT = 0.25;
				int movE = e.X - mDownOn.X;

				double mov = Math.Round((double)movE * ((double)max - min) / Width, 1);
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

						this.Start = mDownSec.Start + mov;
						this.End = mDownSec.End + mov;
					}
					else
					{
						if (mDownSec.Start + mov < bMin) // too left
							mov = bMin - mDownSec.Start;
						else if (mDownSec.Start + mov > bigM) // too right
							mov = bigM - mDownSec.Start;

						this.Start = mDownSec.Start + mov;
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
							int nEndToE = (int)Math.Round((double)this.End / (max - min) * Width);
							if (nEndToE - nSToE < minZoom)
							{
								mov = Math.Round((double)(nEndToE - minZoom) * ((double)max - min) / Width, 1) - mDownSec.Start;
							}
						}
						else //start at 0.100s
						{
							if (mDownSec.End - mDownSec.Start - mov < minT) // less than X sec
								mov = mDownSec.End - minT - mDownSec.Start;
							DEBUG((mDownSec.End - mDownSec.Start - mov).ToString());
						}
					}
					this.Start = mDownSec.Start + mov;
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
							int nSToE = (int)Math.Round(((double)Start) / (max - min) * Width);
							int nEndToE = (int)Math.Round((double)(mDownSec.End + mov) / (max - min) * Width);

							if (nEndToE - nSToE < minZoom)
							{
								mov = Math.Round((double)(minZoom) * ((double)max - min) / Width, 1) + Start - mDownSec.End;
							}
						}
						else //start at 0.100s
						{
							if (mDownSec.End - mDownSec.Start + mov < minT) // less than X sec
								mov = -mDownSec.End + minT + mDownSec.Start;
						}
					}
					this.End = mDownSec.End + mov;
				}

			}

			method();
			curLoc = e;
			return c;
		}

		public int ID
		{ get { return _id; } set { _id = value; } }
		public double Start
		{ get { return _v1; } set { _v1 = value; } }
		public double End { get { return _v2; } set { _v2 = value; } }
		public float Motor1Speed
		{
			get
			{
				return m1s;
			}
			set
			{
				m1s = value;
			}
		}
		public float Motor2Speed { get { return m2s; } set { m2s = value; } }
		public float Motor3Speed { get { return m3s; } set { m3s = value; } }
		public float Motor4Speed { get { return m4s; } set { m4s = value; } }
		double _v1 = 0, _v2 = 0;
		public section()
		{
			_v1 = 0;
			_v2 = 0;
			_id = 0;
		}
		public section(double v1, double v2, int id, float motor1Speed_, float motor2Speed_, float motor3Speed_, float motor4Speed_)
		{
			_v1 = v1;
			_v2 = v2;
			_id = id;
			Motor1Speed = motor1Speed_;
			Motor2Speed = motor2Speed_;
			Motor3Speed = motor3Speed_;
			Motor4Speed = motor4Speed_;
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
