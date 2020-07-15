using System.Collections.Generic;
using ClosedXML.Excel;

namespace Console.Utilities
{
    /// <summary> Common utilities for manipulating Xlsx files </summary>
    public static class XlsxUtil
    {
        /// <summary> Add objects as cell values for a specific row in a worksheet </summary>
        public static void AddRow(this IXLWorksheet sheet, int index, List<object> values)
        {
            for (var i = 1; i < values.Count + 1; i++) sheet.Cell(index, i).Value = values[i - 1];
        }

        /// <summary> Add a table with overall update statistics to a sheet, with the top-left cell located at firstRow, firstColumn </summary>
        public static void AddWorldData(this IXLWorksheet sheet, int firstRow, int firstColumn, int nationCount,
            int majorTime, int minorTime)
        {
            var nextColumn = firstColumn + 1;

            sheet.Cell(firstRow + 0, firstColumn).Value = "Nations: ";
            sheet.Cell(firstRow + 0, nextColumn).Value = nationCount;
            sheet.Cell(firstRow + 1, firstColumn).Value = "Last Major: ";
            sheet.Cell(firstRow + 1, nextColumn).Value = $"{majorTime}s";
            sheet.Cell(firstRow + 2, firstColumn).Value = "Secs/Nation: ";
            sheet.Cell(firstRow + 2, nextColumn).Value = $"{majorTime / (nationCount + 0.0):0.00000}s";
            sheet.Cell(firstRow + 3, firstColumn).Value = "Nations/Sec: ";
            sheet.Cell(firstRow + 3, nextColumn).Value = $"{nationCount / (majorTime + 0.0):0.00000}s";
            sheet.Cell(firstRow + 4, firstColumn).Value = "Last Minor: ";
            sheet.Cell(firstRow + 4, nextColumn).Value = $"{minorTime}s";
            sheet.Cell(firstRow + 5, firstColumn).Value = "Secs/Nation: ";
            sheet.Cell(firstRow + 5, nextColumn).Value = $"{minorTime / (nationCount + 0.0):0.00000}s";
            sheet.Cell(firstRow + 6, firstColumn).Value = "Nations/Sec: ";
            sheet.Cell(firstRow + 6, nextColumn).Value = $"{nationCount / (minorTime + 0.0):0.00000}s";
        }
    }
}