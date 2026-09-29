using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ChangeIcons.Editor;

/// <summary>
/// Hue bar, saturation/brightness square, hex box and a row of quick colors. Changes are
/// reported live while dragging.
/// </summary>
public class ColorPicker : Border
{
    private const double SquareSize = 200;
    private const double BarWidth = 18;

    private static readonly string[] Quick =
    [
        "#FFFFFF", "#C3CDD3", "#0095E2", "#CA8A00", "#86AA7C", "#7141CA", "#55D0E6",
        "#FF3B3B", "#FF8A1E", "#FFD23F", "#4FD65A", "#2BE3FF", "#3D6BFF", "#B04CFF", "#FF4FC3", "#1A1A1A",
    ];

    private readonly Rectangle _hueLayer = new() { Width = SquareSize, Height = SquareSize };
    private readonly Ellipse _svThumb = new() { Width = 14, Height = 14, Stroke = Brushes.White, StrokeThickness = 2, IsHitTestVisible = false };
    private readonly Border _hueThumb = new() { Width = BarWidth + 6, Height = 6, BorderBrush = Brushes.White, BorderThickness = new Thickness(2), IsHitTestVisible = false };
    private readonly Canvas _square = new() { Width = SquareSize, Height = SquareSize, ClipToBounds = true, Cursor = Cursors.Cross };
    private readonly Canvas _bar = new() { Width = BarWidth, Height = SquareSize, Cursor = Cursors.Hand };
    private readonly TextBox _hex = new() { Width = 96 };
    private readonly Border _swatch = new() { Width = 36, Height = 26, CornerRadius = new CornerRadius(3) };

    private double _h, _s, _v;
    private bool _updating;

    public event Action<Color>? ColorChanged;

    public ColorPicker()
    {
        Background = (Brush)Application.Current.Resources["Panel2Brush"];
        BorderBrush = (Brush)Application.Current.Resources["BorderBrush"];
        BorderThickness = new Thickness(1);
        CornerRadius = new CornerRadius(6);
        Padding = new Thickness(12);

        // Square: pure hue, white fading in from the left, black from the bottom
        _square.Children.Add(_hueLayer);
        _square.Children.Add(new Rectangle
        {
            Width = SquareSize, Height = SquareSize,
            Fill = new LinearGradientBrush(Colors.White, Color.FromArgb(0, 255, 255, 255), 0),
        });
        _square.Children.Add(new Rectangle
        {
            Width = SquareSize, Height = SquareSize,
            Fill = new LinearGradientBrush(Color.FromArgb(0, 0, 0, 0), Colors.Black, 90),
        });
        _square.Children.Add(_svThumb);

        var rainbow = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(0, 1) };
        for (var i = 0; i <= 6; i++)
        {
            rainbow.GradientStops.Add(new GradientStop(FromHsv(i * 60 % 360, 1, 1), i / 6.0));
        }

        _bar.Children.Add(new Rectangle { Width = BarWidth, Height = SquareSize, Fill = rainbow, RadiusX = 3, RadiusY = 3 });
        _bar.Children.Add(_hueThumb);
        Canvas.SetLeft(_hueThumb, -3);

        Hook(_square, p =>
        {
            _s = Math.Clamp(p.X / SquareSize, 0, 1);
            _v = 1 - Math.Clamp(p.Y / SquareSize, 0, 1);
            Changed();
        });
        Hook(_bar, p =>
        {
            _h = Math.Clamp(p.Y / SquareSize, 0, 1) * 359.9;
            Changed();
        });

        _hex.TextChanged += (_, _) =>
        {
            if (!_updating && Images.TryParseColor(_hex.Text, out var c))
            {
                SetColor(c, notify: true, fromHex: true);
            }
        };

        var quick = new WrapPanel { Width = SquareSize + BarWidth + 10, Margin = new Thickness(0, 10, 0, 0) };
        foreach (var hex in Quick)
        {
            var color = Images.ParseOr(hex, Colors.White);
            var chip = new Border
            {
                Width = 22, Height = 22, Margin = new Thickness(0, 0, 4, 4), CornerRadius = new CornerRadius(3),
                Background = new SolidColorBrush(color), BorderBrush = (Brush)Application.Current.Resources["BorderBrush"],
                BorderThickness = new Thickness(1), Cursor = Cursors.Hand, ToolTip = hex,
            };
            chip.MouseLeftButtonUp += (_, _) => SetColor(color, notify: true);
            quick.Children.Add(chip);
        }

        var top = new StackPanel { Orientation = Orientation.Horizontal };
        top.Children.Add(_square);
        top.Children.Add(new Border { Width = 10 });
        top.Children.Add(_bar);

        var bottom = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 10, 0, 0) };
        bottom.Children.Add(_swatch);
        bottom.Children.Add(new Border { Width = 8 });
        bottom.Children.Add(_hex);

        var root = new StackPanel();
        root.Children.Add(top);
        root.Children.Add(bottom);
        root.Children.Add(quick);
        Child = root;
    }

    public Color Color => FromHsv(_h, _s, _v);

    public void SetColor(Color color, bool notify = false, bool fromHex = false)
    {
        ToHsv(color, out var h, out _s, out _v);

        // Grays have no hue; keep the bar where it was
        if (_s > 0.001 && _v > 0.001)
        {
            _h = h;
        }

        Refresh(updateHex: !fromHex);
        if (notify)
        {
            ColorChanged?.Invoke(Color);
        }
    }

    private void Changed()
    {
        Refresh(updateHex: true);
        ColorChanged?.Invoke(Color);
    }

    private void Refresh(bool updateHex)
    {
        _hueLayer.Fill = new SolidColorBrush(FromHsv(_h, 1, 1));
        Canvas.SetLeft(_svThumb, _s * SquareSize - 7);
        Canvas.SetTop(_svThumb, (1 - _v) * SquareSize - 7);
        _svThumb.Stroke = _v > 0.6 && _s < 0.4 ? Brushes.Black : Brushes.White;
        Canvas.SetTop(_hueThumb, _h / 360 * SquareSize - 3);
        _swatch.Background = new SolidColorBrush(Color);

        if (updateHex)
        {
            _updating = true;
            _hex.Text = Images.ToHex(Color);
            _updating = false;
        }
    }

    private static void Hook(Panel element, Action<Point> onPoint)
    {
        element.Background ??= Brushes.Transparent;
        element.MouseLeftButtonDown += (_, e) =>
        {
            element.CaptureMouse();
            onPoint(e.GetPosition(element));
            e.Handled = true;
        };
        element.MouseMove += (_, e) =>
        {
            if (element.IsMouseCaptured)
            {
                onPoint(e.GetPosition(element));
            }
        };
        element.MouseLeftButtonUp += (_, _) => element.ReleaseMouseCapture();
    }

    public static Color FromHsv(double h, double s, double v)
    {
        var c = v * s;
        var x = c * (1 - Math.Abs(h / 60 % 2 - 1));
        var m = v - c;
        var (r, g, b) = (h % 360) switch
        {
            < 60 => (c, x, 0.0),
            < 120 => (x, c, 0.0),
            < 180 => (0.0, c, x),
            < 240 => (0.0, x, c),
            < 300 => (x, 0.0, c),
            _ => (c, 0.0, x),
        };
        return Color.FromRgb((byte)Math.Round((r + m) * 255), (byte)Math.Round((g + m) * 255), (byte)Math.Round((b + m) * 255));
    }

    public static void ToHsv(Color color, out double h, out double s, out double v)
    {
        double r = color.R / 255.0, g = color.G / 255.0, b = color.B / 255.0;
        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        var d = max - min;
        v = max;
        s = max == 0 ? 0 : d / max;
        h = d == 0 ? 0
            : max == r ? 60 * ((g - b) / d % 6)
            : max == g ? 60 * ((b - r) / d + 2)
            : 60 * ((r - g) / d + 4);
        if (h < 0)
        {
            h += 360;
        }
    }

    /// <summary>Opens a picker under <paramref name="anchor"/>; closes when you click elsewhere.</summary>
    public static Popup Open(UIElement anchor, Color initial, Action<Color> onChange)
    {
        var picker = new ColorPicker();
        picker.SetColor(initial);
        picker.ColorChanged += onChange;
        var popup = new Popup
        {
            Child = picker, PlacementTarget = anchor, Placement = PlacementMode.Bottom, StaysOpen = false,
            AllowsTransparency = true, PopupAnimation = PopupAnimation.Fade, VerticalOffset = 6,
        };
        popup.IsOpen = true;
        picker._hex.Focus();
        return popup;
    }
}
