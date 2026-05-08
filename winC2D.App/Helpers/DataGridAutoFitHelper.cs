using System.Windows;
using System.Windows.Controls;

namespace winC2D.App.Helpers;

/// <summary>
/// Provides a helper method that auto-fits DataGrid column widths based on
/// header and cell content, similar to Windows Explorer's "Size Columns to Fit".
/// Call once after the DataGrid has been populated with new data.
/// </summary>
public static class DataGridAutoFitHelper
{
    /// <summary>
    /// Auto-fit all columns of the given DataGrid to their content.
    /// Columns with both MinWidth == MaxWidth (i.e. fixed, non-resizable) are skipped.
    /// </summary>
    /// <param name="grid">The DataGrid whose columns should be auto-fitted.</param>
    /// <param name="minWidth">Minimum column width (default 40).</param>
    /// <param name="maxWidth">Maximum column width (default 600).</param>
    /// <param name="padding">Extra horizontal padding added to the measured width (default 20).</param>
    public static void AutoFitColumns(
        DataGrid grid,
        double minWidth = 40,
        double maxWidth = 600,
        double padding = 20)
    {
        if (grid is null || grid.Columns.Count == 0)
            return;

        // Force measure/arrange so that column headers are rendered
        grid.UpdateLayout();

        foreach (var column in grid.Columns)
        {
            // Skip columns that are intentionally fixed-size (checkbox etc.)
            if (column.MinWidth > 0
                && column.MaxWidth > 0
                && Math.Abs(column.MinWidth - column.MaxWidth) < 1)
            {
                continue;
            }

            // Temporarily set to Auto to let WPF compute the desired width.
            // We store the originals to restore behaviour after measurement.
            var originalWidth = column.Width;
            var originalMinWidth = column.MinWidth;
            var originalMaxWidth = column.MaxWidth;

            column.MinWidth = 0;
            column.MaxWidth = double.PositiveInfinity;
            column.Width = DataGridLength.Auto;

            // Force a layout pass so ActualWidth reflects the auto-sized value
            grid.UpdateLayout();

            double measuredWidth = column.ActualWidth;

            if (measuredWidth > 0)
            {
                // Clamp and apply padding
                double target = measuredWidth + padding;
                target = Math.Max(target, minWidth);
                target = Math.Min(target, maxWidth);

                column.Width = new DataGridLength(target);
            }
            else
            {
                // Fallback: restore original if measurement failed
                column.Width = originalWidth;
            }

            column.MinWidth = originalMinWidth;
            column.MaxWidth = originalMaxWidth;
        }
    }
}
