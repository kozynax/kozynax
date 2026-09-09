using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Kozynax.UI;

namespace ZXMAK2.Hardware.WinForms.General
{
    public class DataPanel : Control
    {
        public DataPanelComponent _component;
        public DataPanel()
        {
            TabStop = true;
            this.Font = new Font("Courier", 13, System.Drawing.FontStyle.Regular, GraphicsUnit.Pixel);
            Size = new System.Drawing.Size(424, 99);
            ControlStyles styles = ControlStyles.Selectable |
                                   ControlStyles.UserPaint |
                                   ControlStyles.ResizeRedraw |
                                   ControlStyles.StandardClick |           // csClickEvents
                                   ControlStyles.UserMouse |               // csCaptureMouse
                                   ControlStyles.ContainerControl |        // csAcceptsControls?
                                   ControlStyles.StandardDoubleClick |     // csDoubleClicks
                                   0;
            base.SetStyle(styles, true);

            mouseTimer = new Timer();
            mouseTimer.Enabled = false;
            mouseTimer.Interval = 50;
            mouseTimer.Tick += OnMouseTimer;

            fLineHeight = 1;
        }

        // private...
        private Timer mouseTimer;

        private const int fGutterWidth = 30;
        private int fLineHeight;
        private Bitmap bitmap = null;
        private int wa = 0;
        private int wd = 0;
        private int wtab = 0;
        private int wsp = 0;
        private int wsymb = 0;

        public void DrawLines(Graphics g, int x, int y, int wid, int hei)
        {
            if ((Height <= 0) || (Width <= 0)) return;
            if (!Visible) return;
            if ((bitmap == null) || (bitmap.Width != wid) || (bitmap.Height != hei))
                bitmap = new Bitmap(wid, hei);

            using (Graphics gp = Graphics.FromImage(bitmap))
            {
                int wdsp = 2;
                wa = (int)gp.MeasureString("DDDD", this.Font).Width;             // "DDDD" width (addr)
                wd = (int)gp.MeasureString("DD", this.Font).Width + wdsp * 2;        // "DD" width (data)
                wsymb = (int)gp.MeasureString("D", this.Font).Width;
                wtab = 8;
                wsp = 8;

                int CurrentY = 0;
                Color ink;
                Color paper;

                gp.FillRectangle(new SolidBrush(BackColor), 0, 0, bitmap.Width, bitmap.Height);

                var lines = _component.GetLines();

                foreach (var line in lines)
                {
                    var left = fGutterWidth + wsp;
                    int pos = 0;
                    var elements = line.LineElements.OrderBy(l => l.FirstCol).ToList();
                    foreach (var element in elements)
                    {
                        // HACK: move char block to the right from hex block
                        if (pos > 0 && element.Text.Length == 1 && elements[pos - 1].Text.Length != 1)
                            left += wsp;
                        
                        // Draw blue/gray background
                        if (element.Selected)
                        {
                            if (Focused)
                            {
                                ink = Color.White;
                                paper = Color.Navy;
                            }
                            else
                            {
                                ink = Color.Silver;
                                paper = Color.Gray;
                            }

                            int rectWidth;
                            if (element.Text.Length == 4)
                                rectWidth = wa;
                            else if (element.Text.Length == 2)
                                rectWidth = wd;
                            else
                                rectWidth = wsp * element.Text.Length;
                            
                            gp.FillRectangle(new SolidBrush(paper), new Rectangle(left, CurrentY, rectWidth, fLineHeight));
                        }
                        else
                        {
                            ink = ForeColor;
                            paper = BackColor;
                        }

                        gp.DrawString(element.Text, this.Font, new SolidBrush(ink), left, CurrentY);
                        
                        if (element.Text.Length == 4)
                            left += wa + wsp;
                        else if (element.Text.Length == 2)
                            left += wd;
                        else
                            left += wsp * element.Text.Length;

                        pos++;
                    }
                    CurrentY += fLineHeight;
                }
            }
            g.DrawImageUnscaled(bitmap, x, y);// DrawImage(bitmap, x, y);
        }
        
        protected override void OnMouseDown(MouseEventArgs e)
        {
            if ((e.Button & MouseButtons.Left) != 0)
            {
                int nl = (e.Y - 1) / fLineHeight;
                if ((nl < _component.VisibleLineCount) && (nl >= 0))
                {
                    if (nl != _component.ActiveLine)
                    {
                        _component.ActiveLine = nl;
                        Invalidate();
                    }
                }
                int nc;
                if (wd >= 0)
                {
                    nc = ((e.X - 1) - (fGutterWidth + wsp + wa + wtab));
                    if (nc >= 0) nc /= wd;
                    else nc = -1;
                }
                else
                    nc = 0;
                if ((nc < _component.ColCount) && (nc >= 0))
                {
                    if (nc != _component.ActiveColumn)
                    {
                        _component.ActiveColumn = nc;
                        Invalidate();
                    }
                }
                mouseTimer.Enabled = true;
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if ((e.Button & MouseButtons.Left) != 0)
            {
                int nl = (e.Y - 1) / fLineHeight;
                if ((nl < _component.VisibleLineCount) && (nl >= 0))
                {
                    if (nl != _component.ActiveLine)
                    {
                        _component.ActiveLine = nl;
                        Invalidate();
                    }
                }
                int nc;
                if (wd >= 0)
                {
                    nc = ((e.X - 1) - (fGutterWidth + wsp + wa + wtab));
                    if (nc >= 0) nc /= wd;
                    else nc = -1;
                }
                else
                    nc = 0;
                if ((nc < _component.ColCount) && (nc >= 0))
                {
                    if (nc != _component.ActiveColumn)
                    {
                        _component.ActiveColumn = nc;
                        Invalidate();
                    }
                }
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            int delta = -e.Delta / 120;  //WHEEL_DELTA=120
            if (delta < 0)
                for (int i = 0; i < -delta; i++)
                    _component.ControlUp();
            else
                for (int i = 0; i < delta; i++)
                    _component.ControlDown();
            Invalidate();

            base.OnMouseWheel(e);
        }

        protected override void OnMouseCaptureChanged(EventArgs e)
        {
            mouseTimer.Enabled = false;
            base.OnMouseCaptureChanged(e);
        }

        private void OnMouseTimer(object sender, EventArgs e)
        {
            Point mE = PointToClient(MousePosition);
            int nl = (mE.Y - 1);
            if (nl < 0)
            {
                _component.ControlUp();
                Invalidate();
            }
            if (nl >= (_component.VisibleLineCount * fLineHeight))
            {
                _component.ControlDown();
                Invalidate();
            }

            int nc;
            if (wd >= 0)
            {
                nc = ((mE.X - 1) - (fGutterWidth + wsp + wa + wtab));
                if (nc >= 0) nc /= wd;
                else nc = -1;
            }
            else
                nc = 0;
            if ((nc < _component.ColCount) && (nc >= 0))
            {
                if (nc != _component.ActiveColumn)
                {
                    _component.ActiveColumn = nc;
                    Invalidate();
                }
            }
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            if ((e.Button & MouseButtons.Left) != 0)
            {
                int nl = e.Y / fLineHeight;
                int nc;
                if (wd >= 0)
                    nc = ((e.X - 1) - (fGutterWidth + wsp + wa + wtab)) / wd;
                else
                    nc = 0;
                if ((nl < _component.VisibleLineCount) && (nl >= 0))
                {
                    if ((nc < _component.ColCount) && (nc >= 0))
                    {
                        _component.EditValue(nl, nc);
                        Refresh();
                    }
                }
            }
            base.OnMouseDoubleClick(e);
        }


        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            Invalidate();
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            Invalidate();
        }

        protected override bool IsInputKey(Keys keyData)
        {
            Keys keys1 = keyData & Keys.KeyCode;
            switch (keys1)
            {
                case Keys.Left:
                case Keys.Up:
                case Keys.Right:
                case Keys.Down:
                    return true;
            }
            return base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Down:
                    _component.ControlDown();
                    Invalidate();
                    break;
                case Keys.Up:
                    _component.ControlUp();
                    Invalidate();
                    break;
                case Keys.Left:
                    _component.ControlLeft();
                    Invalidate();
                    break;
                case Keys.Right:
                    _component.ControlRight();
                    Invalidate();
                    break;
                case Keys.PageDown:
                    _component.ControlPageDown();
                    Invalidate();
                    break;
                case Keys.PageUp:
                    _component.ControlPageUp();
                    Invalidate();
                    break;
                case Keys.Enter:
                    if (_component.VisibleLineCount > 0)
                        _component.EditValue(_component.ActiveLine, _component.ActiveColumn);
                    Refresh();
                    break;
            }
            base.OnKeyDown(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            int lh = (int)e.Graphics.MeasureString("3D,", this.Font).Height;
            int lc = ((Height - 2) / lh);
            if (lc < 0) lc = 0;
            if ((_component.VisibleLineCount != lc) || (fLineHeight != lh))
            {
                fLineHeight = lh;
                _component.VisibleLineCount = lc;
                _component.UpdateLines();
            }
            // chk...
            if ((_component.ActiveLine >= _component.VisibleLineCount) && (_component.VisibleLineCount > 0))
            {
                _component.ActiveLine = _component.VisibleLineCount - 1;
                //            Invalidate();
            }
            else if ((_component.ActiveLine < 0) && (_component.VisibleLineCount > 0))
            {
                _component.ActiveLine = 0;
                //            Invalidate();
            }

            DrawLines(e.Graphics, 0, 0, ClientRectangle.Width, ClientRectangle.Height);// Width - 2, Height - 2);
        }

        public void Init(DataPanelComponent component)
        {
            _component = component;
            component.Redraw += (sender, args) => Invalidate(); 
        }
    }
}