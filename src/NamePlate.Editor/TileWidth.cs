using System.Globalization;
using System.Windows.Data;

namespace NamePlate.Editor;

/// <summary>
/// The width of one column in an icon gallery: as many tiles as fit, then each column a little
/// wider, so the tiles spread across the whole row instead of leaving a gap at the right. Bound
/// from the gallery's WrapPanel.ItemWidth to the ListBox's ActualWidth.
/// </summary>
public class TileWidth : IValueConverter
{
    // A tile is 56 px with 3 px around it
    private const double Tile = 62;

    // The gallery's padding (6 each side), border (1 each side) and vertical scroll bar (10)
    private const double Chrome = 24;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var width = value is double w ? w - Chrome : 0;
        if (width < Tile)
        {
            return Tile;
        }

        // Whole pixels, rounded down, so layout rounding never pushes the last tile onto a new row
        return Math.Floor(width / Math.Floor(width / Tile));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
