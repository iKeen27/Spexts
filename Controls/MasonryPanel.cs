using System.Windows;
using System.Windows.Controls;

namespace Spexts.Controls;

/// <summary>
/// Custom masonry/Pinterest-style layout panel.
/// Places each child into the column with the shortest current height,
/// eliminating vertical dead space while maintaining responsive multi-column flow.
/// Column count is determined dynamically: floor(availableWidth / DesiredColumnWidth).
/// </summary>
public class MasonryPanel : Panel
{
    /// <summary>
    /// Target width for each column. The panel calculates
    /// how many columns fit in the available width and distributes children accordingly.
    /// </summary>
    public static readonly DependencyProperty DesiredColumnWidthProperty =
        DependencyProperty.Register(
            nameof(DesiredColumnWidth),
            typeof(double),
            typeof(MasonryPanel),
            new FrameworkPropertyMetadata(392.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public double DesiredColumnWidth
    {
        get => (double)GetValue(DesiredColumnWidthProperty);
        set => SetValue(DesiredColumnWidthProperty, value);
    }

    /// <summary>
    /// Horizontal spacing between columns.
    /// </summary>
    public static readonly DependencyProperty ColumnSpacingProperty =
        DependencyProperty.Register(
            nameof(ColumnSpacing),
            typeof(double),
            typeof(MasonryPanel),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public double ColumnSpacing
    {
        get => (double)GetValue(ColumnSpacingProperty);
        set => SetValue(ColumnSpacingProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        double availableWidth = double.IsInfinity(availableSize.Width)
            ? 1200.0 // Fallback for infinite width
            : availableSize.Width;

        int columnCount = Math.Max(1, (int)Math.Floor(availableWidth / DesiredColumnWidth));
        double totalSpacing = (columnCount - 1) * ColumnSpacing;
        double columnWidth = (availableWidth - totalSpacing) / columnCount;

        // Measure each child with the computed column width
        var childConstraint = new Size(columnWidth, double.PositiveInfinity);
        foreach (UIElement child in InternalChildren)
        {
            child.Measure(childConstraint);
        }

        // Track column heights to compute total panel height
        double[] columnHeights = new double[columnCount];

        foreach (UIElement child in InternalChildren)
        {
            int shortestCol = GetShortestColumn(columnHeights);
            columnHeights[shortestCol] += child.DesiredSize.Height;
        }

        double maxHeight = 0;
        for (int i = 0; i < columnCount; i++)
        {
            if (columnHeights[i] > maxHeight)
                maxHeight = columnHeights[i];
        }

        return new Size(availableWidth, maxHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        int columnCount = Math.Max(1, (int)Math.Floor(finalSize.Width / DesiredColumnWidth));
        double totalSpacing = (columnCount - 1) * ColumnSpacing;
        double columnWidth = (finalSize.Width - totalSpacing) / columnCount;

        double[] columnHeights = new double[columnCount];

        // Center the columns in the available space
        double totalColumnsWidth = columnCount * columnWidth + totalSpacing;
        double leftOffset = Math.Max(0, (finalSize.Width - totalColumnsWidth) / 2.0);

        foreach (UIElement child in InternalChildren)
        {
            int shortestCol = GetShortestColumn(columnHeights);

            double x = leftOffset + shortestCol * (columnWidth + ColumnSpacing);
            double y = columnHeights[shortestCol];

            child.Arrange(new Rect(x, y, columnWidth, child.DesiredSize.Height));

            columnHeights[shortestCol] += child.DesiredSize.Height;
        }

        double maxHeight = 0;
        for (int i = 0; i < columnCount; i++)
        {
            if (columnHeights[i] > maxHeight)
                maxHeight = columnHeights[i];
        }

        return new Size(finalSize.Width, maxHeight);
    }

    private static int GetShortestColumn(double[] columnHeights)
    {
        int shortest = 0;
        for (int i = 1; i < columnHeights.Length; i++)
        {
            if (columnHeights[i] < columnHeights[shortest])
                shortest = i;
        }
        return shortest;
    }
}
