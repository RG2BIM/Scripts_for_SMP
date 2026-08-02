using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Renga;
using Color = System.Drawing.Color;
using FontStyle = System.Drawing.FontStyle;
using Font = System.Drawing.Font;

namespace ScriptManagerPlugin
{
    public class ViewCubeCommand
    {
        public void Execute(IApplication rengaApp, Action<string> print)
        {
            if (WheelForm.Instance != null && !WheelForm.Instance.IsDisposed)
            {
                WheelForm.Instance.Close();
                WheelForm.Instance = null;
                return;
            }
            WheelForm.Instance = new WheelForm(rengaApp);
            WheelForm.Instance.Show();
        }
    }

    public class WheelForm : Form
    {
        public static WheelForm Instance;
        private IApplication _app;
        private bool _dragging;
        private Point _dragStart;
        
        private int _hoverVx = -2, _hoverVy = -2, _hoverVz = -2;
        private int _activeVx = -2, _activeVy = -2, _activeVz = -2;
        private int _nearVx = -2, _nearVy = -2, _nearVz = -2;
        
        private Timer _syncTimer;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        public WheelForm(IApplication app)
        {
            _app = app;
            this.Text = "Видовой штурвал";
            this.Size = new Size(200, 220);
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.TopMost = true;
            this.BackColor = Color.FromArgb(250, 250, 250);
            this.Opacity = 0.95;
            this.DoubleBuffered = true;

            try
            {
                IntPtr hWnd = GetForegroundWindow();
                if (hWnd != IntPtr.Zero)
                {
                    RECT rect;
                    if (GetWindowRect(hWnd, out rect))
                    {
                        // Пользователь просил открывать в ЛЕВОМ ВЕРХНЕМ углу программы.
                        // Делаем отступ сверху (50px) примерно равным левому (20px) визуально.
                        int x = rect.Left + 20; 
                        int y = rect.Top + 20; 
                        this.Location = new Point(x, y);
                    }
                    else
                    {
                        this.Location = new Point(20, 20);
                    }
                }
                else
                {
                    this.Location = new Point(20, 20);
                }
            }
            catch
            {
                this.Location = new Point(20, 20);
            }

            this.MouseDown += OnMouseDown;
            this.MouseMove += OnMouseMove;
            this.MouseUp += (s, e) => _dragging = false;
            this.MouseLeave += (s, e) => { _hoverVx = -2; Invalidate(); };

            var contextMenu = new ContextMenuStrip();
            contextMenu.ShowImageMargin = false;
            contextMenu.DropShadowEnabled = false;
            contextMenu.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            
            var styleMenu = new ToolStripMenuItem("Визуальный стиль");
            styleMenu.DropDownItems.Add("Каркас", null, (s, e) => ApplyStyle(VisualStyle.VisualStyle_Wireframe));
            styleMenu.DropDownItems.Add("Монохромный", null, (s, e) => ApplyStyle(VisualStyle.VisualStyle_Monochrome));
            styleMenu.DropDownItems.Add("Цветной", null, (s, e) => ApplyStyle(VisualStyle.VisualStyle_Color));
            styleMenu.DropDownItems.Add("Текстурированный", null, (s, e) => ApplyStyle(VisualStyle.VisualStyle_Textured));
            contextMenu.Items.Add(styleMenu);

            var viewsMenu = new ToolStripMenuItem("Вид");
            
            viewsMenu.DropDownItems.Add("Сверху", null, (s, e) => ApplyViewDir(0, 0, 1));
            viewsMenu.DropDownItems.Add("Снизу", null, (s, e) => ApplyViewDir(0, 0, -1));
            viewsMenu.DropDownItems.Add("Спереди", null, (s, e) => ApplyViewDir(0, -1, 0));
            viewsMenu.DropDownItems.Add("Сзади", null, (s, e) => ApplyViewDir(0, 1, 0));
            viewsMenu.DropDownItems.Add("Слева", null, (s, e) => ApplyViewDir(-1, 0, 0));
            viewsMenu.DropDownItems.Add("Справа", null, (s, e) => ApplyViewDir(1, 0, 0));
            
            viewsMenu.DropDownItems.Add(new ToolStripSeparator());
            
            viewsMenu.DropDownItems.Add("Сверху спереди", null, (s, e) => ApplyViewDir(0, -1, 1));
            viewsMenu.DropDownItems.Add("Сверху сзади", null, (s, e) => ApplyViewDir(0, 1, 1));
            viewsMenu.DropDownItems.Add("Сверху слева", null, (s, e) => ApplyViewDir(-1, 0, 1));
            viewsMenu.DropDownItems.Add("Сверху справа", null, (s, e) => ApplyViewDir(1, 0, 1));
            viewsMenu.DropDownItems.Add("Снизу спереди", null, (s, e) => ApplyViewDir(0, -1, -1));
            viewsMenu.DropDownItems.Add("Снизу сзади", null, (s, e) => ApplyViewDir(0, 1, -1));
            viewsMenu.DropDownItems.Add("Снизу слева", null, (s, e) => ApplyViewDir(-1, 0, -1));
            viewsMenu.DropDownItems.Add("Снизу справа", null, (s, e) => ApplyViewDir(1, 0, -1));
            viewsMenu.DropDownItems.Add("Спереди слева", null, (s, e) => ApplyViewDir(-1, -1, 0));
            viewsMenu.DropDownItems.Add("Спереди справа", null, (s, e) => ApplyViewDir(1, -1, 0));
            viewsMenu.DropDownItems.Add("Сзади слева", null, (s, e) => ApplyViewDir(-1, 1, 0));
            viewsMenu.DropDownItems.Add("Сзади справа", null, (s, e) => ApplyViewDir(1, 1, 0));
            
            viewsMenu.DropDownItems.Add(new ToolStripSeparator());
            
            viewsMenu.DropDownItems.Add("Сверху спереди слева", null, (s, e) => ApplyViewDir(-1, -1, 1));
            viewsMenu.DropDownItems.Add("Сверху спереди справа", null, (s, e) => ApplyViewDir(1, -1, 1));
            viewsMenu.DropDownItems.Add("Сверху сзади слева", null, (s, e) => ApplyViewDir(-1, 1, 1));
            viewsMenu.DropDownItems.Add("Сверху сзади справа", null, (s, e) => ApplyViewDir(1, 1, 1));
            viewsMenu.DropDownItems.Add("Снизу спереди слева", null, (s, e) => ApplyViewDir(-1, -1, -1));
            viewsMenu.DropDownItems.Add("Снизу спереди справа", null, (s, e) => ApplyViewDir(1, -1, -1));
            viewsMenu.DropDownItems.Add("Снизу сзади слева", null, (s, e) => ApplyViewDir(-1, 1, -1));
            viewsMenu.DropDownItems.Add("Снизу сзади справа", null, (s, e) => ApplyViewDir(1, 1, -1));

            contextMenu.Items.Add(viewsMenu);
            
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("Закрыть видовой куб", null, (s, e) => this.Close());

            this.ContextMenuStrip = contextMenu;

            _syncTimer = new Timer();
            _syncTimer.Interval = 100;
            _syncTimer.Tick += OnSyncTick;
            _syncTimer.Start();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _syncTimer != null)
            {
                _syncTimer.Stop();
                _syncTimer.Dispose();
            }
            base.Dispose(disposing);
        }

        private void OnSyncTick(object sender, EventArgs e)
        {
            try
            {
                var view = _app.ActiveView as Renga.IModelView;
                if (view == null) return;
                var view3DParams = view.GetInterfaceByName("IView3DParams") as Renga.IView3DParams;
                if (view3DParams == null || view3DParams.Camera == null) return;

                var cam = view3DParams.Camera;
                var F = cam.FocusPoint;
                var P = cam.Position;
                double dx = P.X - F.X, dy = P.Y - F.Y, dz = P.Z - F.Z;
                double len = Math.Sqrt(dx*dx + dy*dy + dz*dz);
                if (len < 0.001) return;
                dx /= len; dy /= len; dz /= len;

                int bestVx = -2, bestVy = -2, bestVz = -2;
                double maxDot = -1;

                for (int vx = -1; vx <= 1; vx++)
                for (int vy = -1; vy <= 1; vy++)
                for (int vz = -1; vz <= 1; vz++)
                {
                    if (vx == 0 && vy == 0 && vz == 0) continue;
                    double vLen = Math.Sqrt(vx*vx + vy*vy + vz*vz);
                    double nx = vx / vLen;
                    double ny = vy / vLen;
                    double nz = vz / vLen;
                    double dot = dx * nx + dy * ny + dz * nz;
                    if (dot > maxDot)
                    {
                        maxDot = dot;
                        bestVx = vx; bestVy = vy; bestVz = vz;
                    }
                }

                if (maxDot > 0.999) 
                {
                    if (_activeVx != bestVx || _activeVy != bestVy || _activeVz != bestVz || _nearVx != -2)
                    {
                        _activeVx = bestVx; _activeVy = bestVy; _activeVz = bestVz;
                        _nearVx = -2; _nearVy = -2; _nearVz = -2;
                        Invalidate();
                    }
                }
                else if (maxDot > 0.90) 
                {
                    if (_nearVx != bestVx || _nearVy != bestVy || _nearVz != bestVz || _activeVx != -2)
                    {
                        _nearVx = bestVx; _nearVy = bestVy; _nearVz = bestVz;
                        _activeVx = -2; _activeVy = -2; _activeVz = -2;
                        Invalidate();
                    }
                }
                else
                {
                    if (_activeVx != -2 || _nearVx != -2)
                    {
                        _activeVx = -2; _nearVx = -2;
                        Invalidate();
                    }
                }
            }
            catch { }
        }

        private void ApplyStyle(VisualStyle style)
        {
            try 
            {
                var view = _app.ActiveView as Renga.IModelView;
                if (view == null) return;
                view.VisualStyle = style;
            } 
            catch { }
        }

        private void ApplyViewDir(double vx, double vy, double vz)
        {
            try
            {
                var view = _app.ActiveView as Renga.IModelView;
                if (view == null) return;
                var view3DParams = view.GetInterfaceByName("IView3DParams") as Renga.IView3DParams;
                if (view3DParams == null || view3DParams.Camera == null) return;

                var cam = view3DParams.Camera;
                var F = cam.FocusPoint;
                var P = cam.Position;
                double dx = P.X - F.X, dy = P.Y - F.Y, dz = P.Z - F.Z;
                double D = Math.Sqrt(dx*dx + dy*dy + dz*dz);
                if (D < 100) D = 5000;

                double ux = 0, uy = 0, uz = 1;
                if (Math.Abs(vx) < 0.001 && Math.Abs(vy) < 0.001) { ux = 0; uy = -1; uz = 0; }
                
                double vLen = Math.Sqrt(vx*vx + vy*vy + vz*vz);
                if (vLen > 0) { vx /= vLen; vy /= vLen; vz /= vLen; }

                var newPos = new Renga.FloatPoint3D();
                newPos.X = (float)(F.X + vx * D);
                newPos.Y = (float)(F.Y + vy * D);
                newPos.Z = (float)(F.Z + vz * D);

                var upVec = new Renga.FloatVector3D();
                upVec.X = (float)ux; upVec.Y = (float)uy; upVec.Z = (float)uz;

                cam.LookAt(F, newPos, upVec);
            }
            catch { }
        }

        private (int vx, int vy, int vz, bool valid) GetVectorFromPoint(Point p)
        {
            int cx = 100, cy = 100;
            int dx = p.X - cx;
            int dy = p.Y - cy;
            
            if (dy > 95) return (0, 0, 0, false); 
            if (Math.Abs(dx) > 95 || dy < -95) return (0, 0, 0, false);
            
            if (Math.Abs(dx) < 14 && Math.Abs(dy) < 14) return (0, 0, 1, true);
            if (Math.Abs(dx) < 14 && Math.Abs(dy + 40) < 14) return (0, 1, 0, true);
            if (Math.Abs(dx) < 14 && Math.Abs(dy - 40) < 14) return (0, -1, 0, true);
            if (Math.Abs(dx + 40) < 14 && Math.Abs(dy) < 14) return (-1, 0, 0, true);
            if (Math.Abs(dx - 40) < 14 && Math.Abs(dy) < 14) return (1, 0, 0, true);
            
            int adx = Math.Abs(dx);
            int ady = Math.Abs(dy);
            int maxD = Math.Max(adx, ady);
            
            int vx = 0, vy = 0;
            if (dx > 40) vx = 1; else if (dx < -40) vx = -1;
            if (dy > 40) vy = -1; else if (dy < -40) vy = 1; 
            
            if (maxD <= 40)
            {
                vx = (dx > 0) ? 1 : -1;
                vy = (dy > 0) ? -1 : 1; 
            }
            
            int vz = 0;
            if (maxD <= 40) vz = 1; 
            else if (maxD <= 60) 
            {
                if (vx == 0 || vy == 0) vz = 1; 
                else vz = 0; 
            }
            else if (maxD <= 80) vz = -1; 
            else return (0, 0, -1, true); 
            
            return (vx, vy, vz, true);
        }

        private string GetViewName(int vx, int vy, int vz)
        {
            if (vz == 1)
            {
                if (vx == 0 && vy == 0) return "Сверху";
                if (vx == 0 && vy == -1) return "Сверху спереди";
                if (vx == 0 && vy == 1) return "Сверху сзади";
                if (vx == -1 && vy == 0) return "Сверху слева";
                if (vx == 1 && vy == 0) return "Сверху справа";
                if (vx == -1 && vy == -1) return "Сверху спереди слева";
                if (vx == 1 && vy == -1) return "Сверху спереди справа";
                if (vx == -1 && vy == 1) return "Сверху сзади слева";
                if (vx == 1 && vy == 1) return "Сверху сзади справа";
            }
            else if (vz == 0)
            {
                if (vx == 0 && vy == -1) return "Спереди";
                if (vx == 0 && vy == 1) return "Сзади";
                if (vx == -1 && vy == 0) return "Слева";
                if (vx == 1 && vy == 0) return "Справа";
                if (vx == -1 && vy == -1) return "Спереди слева";
                if (vx == 1 && vy == -1) return "Спереди справа";
                if (vx == -1 && vy == 1) return "Сзади слева";
                if (vx == 1 && vy == 1) return "Сзади справа";
            }
            else if (vz == -1)
            {
                if (vx == 0 && vy == 0) return "Снизу";
                if (vx == 0 && vy == -1) return "Снизу спереди";
                if (vx == 0 && vy == 1) return "Снизу сзади";
                if (vx == -1 && vy == 0) return "Снизу слева";
                if (vx == 1 && vy == 0) return "Снизу справа";
                if (vx == -1 && vy == -1) return "Снизу спереди слева";
                if (vx == 1 && vy == -1) return "Снизу спереди справа";
                if (vx == -1 && vy == 1) return "Снизу сзади слева";
                if (vx == 1 && vy == 1) return "Снизу сзади справа";
            }
            return "";
        }

        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                if (e.Button == MouseButtons.Left)
                {
                    var res = GetVectorFromPoint(e.Location);
                    if (res.valid)
                    {
                        ApplyViewDir(res.vx, res.vy, res.vz);
                    }
                    else
                    {
                        _dragging = true; 
                        _dragStart = e.Location;
                    }
                }
            }
            catch { }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                if (_dragging) 
                { 
                    this.Location = new Point(this.Location.X + (e.X - _dragStart.X), this.Location.Y + (e.Y - _dragStart.Y)); 
                    return; 
                }
                var res = GetVectorFromPoint(e.Location);
                if (res.valid)
                {
                    if (_hoverVx != res.vx || _hoverVy != res.vy || _hoverVz != res.vz)
                    {
                        _hoverVx = res.vx; _hoverVy = res.vy; _hoverVz = res.vz;
                        Invalidate();
                    }
                }
                else
                {
                    if (_hoverVx != -2)
                    {
                        _hoverVx = -2;
                        Invalidate();
                    }
                }
            }
            catch { }
        }

        private GraphicsPath CreateRoundedRectPath(int x, int y, int w, int h, int r)
        {
            GraphicsPath path = new GraphicsPath();
            if (r <= 0) { path.AddRectangle(new Rectangle(x, y, w, h)); return path; }
            int d = r * 2;
            path.AddArc(x, y, d, d, 180, 90);
            path.AddArc(x + w - d, y, d, d, 270, 90);
            path.AddArc(x + w - d, y + h - d, d, d, 0, 90);
            path.AddArc(x, y + h - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void FillSector(Graphics g, Brush b, int vx, int vy, int vz)
        {
            int cx = 100, cy = 100;
            if (vz == 1 && vx == 0 && vy == 0) {
                using (var p = CreateRoundedRectPath(cx - 12, cy - 12, 24, 24, 8)) g.FillPath(b, p);
                return;
            }
            if (vz == 0 && Math.Abs(vx) + Math.Abs(vy) == 1) {
                int uix = vx;
                int uiy = -vy; 
                using (var p = CreateRoundedRectPath(cx + uix*40 - 12, cy + uiy*40 - 12, 24, 24, 8)) g.FillPath(b, p);
                return;
            }
            
            Region oldClip = g.Clip;
            
            GraphicsPath outer = CreateRoundedRectPath(cx - 80, cy - 80, 160, 160, 32);
            GraphicsPath inner = CreateRoundedRectPath(cx - 60, cy - 60, 120, 120, 20);
            GraphicsPath center = CreateRoundedRectPath(cx - 40, cy - 40, 80, 80, 0); 
            
            int uix2 = (vx > 0) ? 1 : (vx < 0 ? -1 : 0);
            int uiy2 = (vy > 0) ? -1 : (vy < 0 ? 1 : 0);
            
            try 
            {
                using (var p = CreateRoundedRectPath(cx - 12, cy - 12, 24, 24, 8)) g.SetClip(p, CombineMode.Exclude);
                using (var p = CreateRoundedRectPath(cx - 52, cy - 12, 24, 24, 8)) g.SetClip(p, CombineMode.Exclude);
                using (var p = CreateRoundedRectPath(cx + 28, cy - 12, 24, 24, 8)) g.SetClip(p, CombineMode.Exclude);
                using (var p = CreateRoundedRectPath(cx - 12, cy - 52, 24, 24, 8)) g.SetClip(p, CombineMode.Exclude);
                using (var p = CreateRoundedRectPath(cx - 12, cy + 28, 24, 24, 8)) g.SetClip(p, CombineMode.Exclude);

                if (vz == 1 && vx != 0 && vy != 0) 
                {
                    g.SetClip(center, CombineMode.Intersect);
                    g.FillRectangle(b, cx + (uix2 < 0 ? -40 : 0), cy + (uiy2 < 0 ? -40 : 0), 40, 40);
                }
                else if ((vz == 1 && (vx == 0 || vy == 0)) || (vz == 0 && vx != 0 && vy != 0)) 
                {
                    g.SetClip(inner, CombineMode.Intersect);
                    g.SetClip(center, CombineMode.Exclude);
                    g.FillRectangle(b, cx + (uix2 < 0 ? -80 : (uix2 > 0 ? 40 : -40)), cy + (uiy2 < 0 ? -80 : (uiy2 > 0 ? 40 : -40)), 
                                    uix2 == 0 ? 80 : 40, uiy2 == 0 ? 80 : 40);
                }
                else if (vz == -1) 
                {
                    if (vx == 0 && vy == 0)
                    {
                        using (var rim = CreateRoundedRectPath(cx - 90, cy - 90, 180, 180, 40))
                        {
                            g.SetClip(rim, CombineMode.Intersect);
                            g.SetClip(outer, CombineMode.Exclude);
                            g.FillPath(b, rim);
                        }
                    }
                    else
                    {
                        g.SetClip(outer, CombineMode.Intersect);
                        g.SetClip(inner, CombineMode.Exclude);
                        g.FillRectangle(b, cx + (uix2 < 0 ? -80 : (uix2 > 0 ? 40 : -40)), cy + (uiy2 < 0 ? -80 : (uiy2 > 0 ? 40 : -40)), 
                                        uix2 == 0 ? 80 : 40, uiy2 == 0 ? 80 : 40);
                    }
                }
            } 
            finally 
            {
                g.Clip = oldClip;
                outer.Dispose();
                inner.Dispose();
                center.Dispose();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            try
            {
                e.Graphics.Clear(this.BackColor);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                
                e.Graphics.DrawRectangle(Pens.Gray, 0, 0, this.Width - 1, this.Height - 1);
                
                int cx = 100, cy = 100;
                
                if (_nearVx != -2)
                {
                    using (Brush b = new SolidBrush(Color.FromArgb(80, 220, 200, 50))) FillSector(e.Graphics, b, _nearVx, _nearVy, _nearVz);
                }
                if (_hoverVx != -2)
                {
                    using (Brush b = new SolidBrush(Color.FromArgb(80, 100, 150, 255))) FillSector(e.Graphics, b, _hoverVx, _hoverVy, _hoverVz);
                }
                if (_activeVx != -2)
                {
                    using (Brush b = new SolidBrush(Color.FromArgb(80, 50, 200, 80))) FillSector(e.Graphics, b, _activeVx, _activeVy, _activeVz);
                }
                
                using (Pen pLine = new Pen(Color.FromArgb(100, 100, 100), 1.5f))
                {
                    using (Region rsRegion = new Region())
                    {
                        rsRegion.MakeEmpty();
                        using (var p = CreateRoundedRectPath(cx - 12, cy - 12, 24, 24, 8)) rsRegion.Union(p);
                        using (var p = CreateRoundedRectPath(cx - 12, cy - 52, 24, 24, 8)) rsRegion.Union(p);
                        using (var p = CreateRoundedRectPath(cx - 12, cy + 28, 24, 24, 8)) rsRegion.Union(p);
                        using (var p = CreateRoundedRectPath(cx - 52, cy - 12, 24, 24, 8)) rsRegion.Union(p);
                        using (var p = CreateRoundedRectPath(cx + 28, cy - 12, 24, 24, 8)) rsRegion.Union(p);
                        
                        e.Graphics.SetClip(rsRegion, CombineMode.Exclude);
                        
                        using (var rim = CreateRoundedRectPath(cx - 90, cy - 90, 180, 180, 40)) e.Graphics.DrawPath(pLine, rim);
                        using (var outer = CreateRoundedRectPath(cx - 80, cy - 80, 160, 160, 32)) e.Graphics.DrawPath(pLine, outer);
                        using (var inner = CreateRoundedRectPath(cx - 60, cy - 60, 120, 120, 20)) e.Graphics.DrawPath(pLine, inner);
                        using (var center = CreateRoundedRectPath(cx - 40, cy - 40, 80, 80, 0)) e.Graphics.DrawPath(pLine, center);
                        
                        e.Graphics.DrawLine(pLine, cx - 40, cy - 80, cx - 40, cy - 40);
                        e.Graphics.DrawLine(pLine, cx + 40, cy - 80, cx + 40, cy - 40);
                        e.Graphics.DrawLine(pLine, cx - 40, cy + 40, cx - 40, cy + 80);
                        e.Graphics.DrawLine(pLine, cx + 40, cy + 40, cx + 40, cy + 80);
                        
                        e.Graphics.DrawLine(pLine, cx - 80, cy - 40, cx - 40, cy - 40);
                        e.Graphics.DrawLine(pLine, cx - 80, cy + 40, cx - 40, cy + 40);
                        e.Graphics.DrawLine(pLine, cx + 40, cy - 40, cx + 80, cy - 40);
                        e.Graphics.DrawLine(pLine, cx + 40, cy + 40, cx + 80, cy + 40);
                        
                        // Внутреннее перекрестие для разделения зон верхних изометрий
                        e.Graphics.DrawLine(pLine, cx, cy - 40, cx, cy + 40);
                        e.Graphics.DrawLine(pLine, cx - 40, cy, cx + 40, cy);
                        
                        e.Graphics.ResetClip();
                        
                        using (var p = CreateRoundedRectPath(cx - 12, cy - 12, 24, 24, 8)) e.Graphics.DrawPath(pLine, p);
                        using (var p = CreateRoundedRectPath(cx - 12, cy - 52, 24, 24, 8)) e.Graphics.DrawPath(pLine, p);
                        using (var p = CreateRoundedRectPath(cx - 12, cy + 28, 24, 24, 8)) e.Graphics.DrawPath(pLine, p);
                        using (var p = CreateRoundedRectPath(cx - 52, cy - 12, 24, 24, 8)) e.Graphics.DrawPath(pLine, p);
                        using (var p = CreateRoundedRectPath(cx + 28, cy - 12, 24, 24, 8)) e.Graphics.DrawPath(pLine, p);
                    }
                }

                string text = "";
                if (_hoverVx != -2) text = GetViewName(_hoverVx, _hoverVy, _hoverVz);
                else if (_activeVx != -2) text = GetViewName(_activeVx, _activeVy, _activeVz);
                else if (_nearVx != -2) text = "~ " + GetViewName(_nearVx, _nearVy, _nearVz);
                else text = "ПКМ: Меню";

                if (!string.IsNullOrEmpty(text))
                {
                    using (Font f = new Font("Segoe UI", 8, FontStyle.Regular))
                    {
                        SizeF sz = e.Graphics.MeasureString(text, f);
                        float tx = (this.Width - sz.Width) / 2;
                        float ty = 196;
                        e.Graphics.DrawString(text, f, Brushes.Black, tx, ty);
                    }
                }
            }
            catch { }
        }
    }
}
