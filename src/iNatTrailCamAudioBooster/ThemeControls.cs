using System.Drawing.Drawing2D;

namespace INatTrailCamAudioBooster;

internal static class Theme
{
    public static readonly Color Background = Color.FromArgb(244, 242, 234);
    public static readonly Color Panel = Color.White;
    public static readonly Color Ink = Color.FromArgb(29, 42, 36);
    public static readonly Color Muted = Color.FromArgb(101, 117, 108);
    public static readonly Color Line = Color.FromArgb(215, 222, 213);
    public static readonly Color Green = Color.FromArgb(49, 92, 61);
    public static readonly Color GreenStrong = Color.FromArgb(35, 68, 45);
    public static readonly Color GreenSoft = Color.FromArgb(230, 238, 228);
    public static readonly Color Cream = Color.FromArgb(251, 248, 239);
    public static readonly Color Danger = Color.FromArgb(166, 67, 43);
    public static readonly Color Warning = Color.FromArgb(118, 82, 43);
}

internal sealed class GradientHeader : Panel
{
    public GradientHeader()
    {
        DoubleBuffered = true;
        Height = 112;
        Dock = DockStyle.Top;
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        using var brush = new LinearGradientBrush(
            ClientRectangle,
            Color.FromArgb(31, 61, 43),
            Color.FromArgb(64, 111, 74),
            15f);

        e.Graphics.FillRectangle(brush, ClientRectangle);
    }
}

internal sealed class RoundedPanel : Panel
{
    public int Radius { get; set; } = 18;
    public Color BorderColor { get; set; } = Theme.Line;

    public RoundedPanel()
    {
        DoubleBuffered = true;
        BackColor = Theme.Panel;
        Padding = new Padding(16);
    }

    protected override void OnResize(EventArgs eventargs)
    {
        base.OnResize(eventargs);
        using var path = RoundedRectangle(ClientRectangle, Radius);
        Region = new Region(path);
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = ClientRectangle;
        rect.Width -= 1;
        rect.Height -= 1;

        using var path = RoundedRectangle(rect, Radius);
        using var pen = new Pen(BorderColor);
        e.Graphics.DrawPath(pen, path);
        base.OnPaint(e);
    }

    internal static GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
    {
        var diameter = Math.Max(2, radius * 2);
        var path = new GraphicsPath();

        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();

        return path;
    }
}

internal sealed class ModernButton : Button
{
    private int _radius = 18;

    public int Radius
    {
        get => _radius;
        set { _radius = value; Invalidate(); }
    }

    public ModernButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        Cursor = Cursors.Hand;
        Height = 38;
        Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        BackColor = Theme.Green;
        ForeColor = Color.White;
        Padding = new Padding(10, 0, 10, 0);
        DoubleBuffered = true;
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        using var path = RoundedPanel.RoundedRectangle(ClientRectangle, Radius);
        Region = new Region(path);
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        if (Enabled && BackColor == Theme.Green)
            BackColor = Theme.GreenStrong;
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        if (Enabled && ForeColor == Color.White)
            BackColor = Theme.Green;
    }
}

internal sealed class ModernProgressBar : Control
{
    private int _value;

    public int Value
    {
        get => _value;
        set
        {
            _value = Math.Clamp(value, 0, 100);
            Invalidate();
        }
    }

    public ModernProgressBar()
    {
        DoubleBuffered = true;
        Height = 12;
        BackColor = Theme.GreenSoft;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var full = ClientRectangle;
        using var bgPath = RoundedPanel.RoundedRectangle(full, 6);
        using var bgBrush = new SolidBrush(Theme.GreenSoft);
        e.Graphics.FillPath(bgBrush, bgPath);

        if (Value <= 0) return;

        var width = Math.Max(12, (int)Math.Round(full.Width * Value / 100d));
        var progressRect = new Rectangle(full.X, full.Y, Math.Min(width, full.Width), full.Height);
        using var progressPath = RoundedPanel.RoundedRectangle(progressRect, 6);
        using var progressBrush = new SolidBrush(Theme.Green);
        e.Graphics.FillPath(progressBrush, progressPath);
    }
}
