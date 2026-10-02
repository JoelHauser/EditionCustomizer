using System.Globalization;
using System.Windows;
using System.Windows.Media;
using ChangeIcons.Shared;

namespace ChangeIcons.Editor;

/// <summary>
/// How a nickname is colored and moves. The math is the shared NamePaint, the same code the plugin
/// runs, so the preview is what the game draws.
/// </summary>
public record NameLook(IReadOnlyList<Color> Colors, NameMode Mode, double Speed, string? Motion = null, bool Reverse = false)
{
    public static NameLook Solid(Color color) => new([color], NameMode.Solid, 0);

    public bool Moving => ToPaint().Moving;

    /// <summary>One color on a solid name, all of them otherwise.</summary>
    public NamePaint ToPaint()
    {
        var colors = Mode == NameMode.Solid && Colors.Count > 0 ? [Colors[0]] : Colors;
        var rgb = colors.Select(c => new[] { c.R / 255.0, c.G / 255.0, c.B / 255.0 }).ToArray();
        return new NamePaint(rgb, Mode == NameMode.Letters, Motion, Speed, Reverse);
    }
}

/// <summary>
/// A nickname drawn in its colors: one color, a gradient across the name or one per letter, still
/// or moving.
/// </summary>
public class StyledName : FrameworkElement
{
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text), typeof(string), typeof(StyledName), new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty LookProperty = DependencyProperty.Register(
        nameof(Look), typeof(NameLook), typeof(StyledName), new FrameworkPropertyMetadata(NameLook.Solid(Colors.White), FrameworkPropertyMetadataOptions.AffectsRender, (d, _) => ((StyledName)d).LookChanged()));

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
    private NamePaint _paint = NameLook.Solid(Colors.White).ToPaint();
    private bool _animating;

    // Where each visible letter sits, so a frame doesn't measure them again
    private string? _layoutKey;
    private readonly List<(int Index, double Left, double Right)> _letters = [];
    private double _minX, _width;

    public StyledName()
    {
        Loaded += (_, _) => UpdateAnimation();
        Unloaded += (_, _) => StopAnimation();
        IsVisibleChanged += (_, _) => UpdateAnimation();
    }

    private void LookChanged()
    {
        _paint = (Look ?? NameLook.Solid(Colors.White)).ToPaint();
        UpdateAnimation();
    }

    private void UpdateAnimation()
    {
        if (_paint.Moving && IsLoaded && IsVisible)
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
        var paint = _paint;
        var rgb = new double[3];

        // One still color: no per-letter work
        if (paint.Count == 1 && !paint.Moving)
        {
            var c = paint.Color(0);
            dc.DrawText(Format(new SolidColorBrush(ToColor(c))), new Point(0, 0));
            return;
        }

        var text = Format(Brushes.White);
        MeasureLetters(text);
        var time = (DateTime.UtcNow - Start).TotalSeconds;

        // Each letter gets the colors at its left and right edges, as the plugin colors each
        // letter's corners in game
        var letter = 0;
        foreach (var (index, left, right) in _letters)
        {
            paint.At((left - _minX) / _width, letter, time, rgb);
            var c0 = ToColor(rgb);
            paint.At((right - _minX) / _width, letter, time, rgb);
            var c1 = ToColor(rgb);

            Brush brush = c0 == c1
                ? new SolidColorBrush(c0)
                : new LinearGradientBrush(c0, c1, new Point(left, 0), new Point(right, 0)) { MappingMode = BrushMappingMode.Absolute };
            brush.Freeze();
            text.SetForegroundBrush(brush, index, 1);
            letter++;
        }

        dc.DrawText(text, new Point(0, 0));
    }

    private void MeasureLetters(FormattedText text)
    {
        var key = $"{Text}|{FontSize}|{FontFamily}|{FontWeight}|{VisualTreeHelper.GetDpi(this).PixelsPerDip}";
        if (key == _layoutKey)
        {
            return;
        }

        _layoutKey = key;
        _letters.Clear();
        var value = Text ?? "";
        for (var i = 0; i < value.Length; i++)
        {
            if (char.IsWhiteSpace(value[i]))
            {
                continue;
            }

            var bounds = text.BuildHighlightGeometry(new Point(0, 0), i, 1)?.Bounds;
            if (bounds is { IsEmpty: false } b)
            {
                _letters.Add((i, b.Left, b.Right));
            }
        }

        _minX = _letters.Count > 0 ? _letters.Min(l => l.Left) : 0;
        var maxX = _letters.Count > 0 ? _letters.Max(l => l.Right) : 1;
        _width = Math.Max(maxX - _minX, 0.001);
    }

    private static Color ToColor(double[] rgb) =>
        Color.FromRgb(Byte(rgb[0]), Byte(rgb[1]), Byte(rgb[2]));

    private static byte Byte(double v) => (byte)Math.Round(Math.Clamp(v, 0, 1) * 255);
}
