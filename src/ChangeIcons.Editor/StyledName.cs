using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace ChangeIcons.Editor;

/// <summary>
/// How a nickname is colored. The math matches the plugin's NameStyle, so the preview is what
/// the game draws.
/// </summary>
public record NameLook(IReadOnlyList<Color> Colors, NameMode Mode, double Speed)
{
    public static NameLook Solid(Color color) => new([color], NameMode.Solid, 0);

    public bool Moving => Speed > 0 && Colors.Count > 1 && Mode != NameMode.Solid;

    public Color Letter(int index, double time)
    {
        var shift = Speed <= 0 ? 0 : (int)Math.Floor(time * Speed * 4);
        var n = Colors.Count;
        return Colors[((index + shift) % n + n) % n];
    }

    public Color Gradient(double x, double time)
    {
        var n = Colors.Count;
        if (Speed <= 0)
        {
            return Sample(Math.Clamp(x, 0, 1) * (n - 1), wrap: false);
        }

        var t = x + time * Speed;
        t -= Math.Floor(t);
        return Sample(t * n, wrap: true);
    }

    private Color Sample(double position, bool wrap)
    {
        var n = Colors.Count;
        var i = (int)Math.Floor(position);
        var f = position - i;
        var a = Colors[Math.Clamp(i, 0, n - 1)];
        var b = wrap ? Colors[(i + 1) % n] : Colors[Math.Clamp(i + 1, 0, n - 1)];
        return Color.FromArgb(255, Lerp(a.R, b.R, f), Lerp(a.G, b.G, f), Lerp(a.B, b.B, f));
    }

    private static byte Lerp(byte a, byte b, double f) => (byte)Math.Round(a + (b - a) * f);
}

/// <summary>
/// A nickname drawn in its colors: solid, gradient across the name, or per letter, moving or
/// still.
/// </summary>
public class StyledName : FrameworkElement
{
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text), typeof(string), typeof(StyledName), new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty LookProperty = DependencyProperty.Register(
        nameof(Look), typeof(NameLook), typeof(StyledName), new FrameworkPropertyMetadata(NameLook.Solid(Colors.White), FrameworkPropertyMetadataOptions.AffectsRender, (d, _) => ((StyledName)d).UpdateAnimation()));

    public static readonly DependencyProperty FontSizeProperty = DependencyProperty.Register(
        nameof(FontSize), typeof(double), typeof(StyledName), new FrameworkPropertyMetadata(14.0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty FontFamilyProperty = DependencyProperty.Register(
        nameof(FontFamily), typeof(FontFamily), typeof(StyledName), new FrameworkPropertyMetadata(new FontFamily("Bahnschrift"), FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty FontWeightProperty = DependencyProperty.Register(
        nameof(FontWeight), typeof(FontWeight), typeof(StyledName), new FrameworkPropertyMetadata(FontWeights.SemiBold, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }
    public NameLook Look { get => (NameLook)GetValue(LookProperty); set => SetValue(LookProperty, value); }
    public double FontSize { get => (double)GetValue(FontSizeProperty); set => SetValue(FontSizeProperty, value); }
    public FontFamily FontFamily { get => (FontFamily)GetValue(FontFamilyProperty); set => SetValue(FontFamilyProperty, value); }
    public FontWeight FontWeight { get => (FontWeight)GetValue(FontWeightProperty); set => SetValue(FontWeightProperty, value); }

    private static readonly DateTime Start = DateTime.UtcNow;
    private bool _animating;

    public StyledName()
    {
        Loaded += (_, _) => UpdateAnimation();
        Unloaded += (_, _) => StopAnimation();
        IsVisibleChanged += (_, _) => UpdateAnimation();
    }

    private void UpdateAnimation()
    {
        if (Look.Moving && IsLoaded && IsVisible)
        {
            if (!_animating)
            {
                CompositionTarget.Rendering += OnFrame;
                _animating = true;
            }
        }
        else
        {
            StopAnimation();
        }
    }

    private void StopAnimation()
    {
        if (_animating)
        {
            CompositionTarget.Rendering -= OnFrame;
            _animating = false;
        }
    }

    private void OnFrame(object? sender, EventArgs e) => InvalidateVisual();

    private FormattedText Format(Brush brush) =>
        new(Text ?? "", CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
            new Typeface(FontFamily, FontStyles.Normal, FontWeight, FontStretches.Normal), FontSize, brush,
            VisualTreeHelper.GetDpi(this).PixelsPerDip);

    protected override Size MeasureOverride(Size availableSize)
    {
        var text = Format(Brushes.White);
        return new Size(Math.Ceiling(text.WidthIncludingTrailingWhitespace), Math.Ceiling(text.Height));
    }

    protected override void OnRender(DrawingContext dc)
    {
        var look = Look;
        var text = Format(new SolidColorBrush(look.Colors.Count > 0 ? look.Colors[0] : Colors.White));
        var time = (DateTime.UtcNow - Start).TotalSeconds;

        if (look.Colors.Count > 1 && look.Mode == NameMode.Letters)
        {
            var letter = 0;
            for (var i = 0; i < Text.Length; i++)
            {
                if (!char.IsWhiteSpace(Text[i]))
                {
                    text.SetForegroundBrush(new SolidColorBrush(look.Letter(letter++, time)), i, 1);
                }
            }
        }
        else if (look.Colors.Count > 1 && look.Mode == NameMode.Gradient)
        {
            // Sampled finely enough to look continuous, from the same formula as the plugin
            var brush = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 0) };
            const int steps = 48;
            for (var s = 0; s <= steps; s++)
            {
                var x = (double)s / steps;
                brush.GradientStops.Add(new GradientStop(look.Gradient(x, time), x));
            }

            text.SetForegroundBrush(brush);
        }

        dc.DrawText(text, new Point(0, 0));
    }
}
