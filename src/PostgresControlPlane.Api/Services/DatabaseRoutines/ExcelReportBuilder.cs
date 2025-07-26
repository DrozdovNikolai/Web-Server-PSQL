using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using PostgresControlPlane.Api.Models.Contracts;

namespace PostgresControlPlane.Api.Services.DatabaseRoutines;

public sealed partial class ExcelReportBuilder : IExcelReportBuilder
{
    private const int MaximumExcelRows = 1_048_576;
    private const int MaximumExcelColumns = 16_384;

    public RoutineExcelFile Build(
        RoutineInvocationResponse result,
        ExcelExportSettings settings)
    {
        ValidateSettings(result, settings);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(settings.SheetName.Trim());
        var startRow = settings.ReportStartPosition.Row;
        var startColumn = settings.ReportStartPosition.Column;

        for (var columnIndex = 0; columnIndex < result.Columns.Count; columnIndex++)
        {
            var cell = worksheet.Cell(startRow, startColumn + columnIndex);
            SetText(cell, result.Columns[columnIndex]);
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.FromHtml("#075C43");
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#E8F3ED");
        }

        for (var rowIndex = 0; rowIndex < result.Rows.Count; rowIndex++)
        {
            var row = result.Rows[rowIndex];
            for (var columnIndex = 0; columnIndex < result.Columns.Count; columnIndex++)
            {
                row.TryGetValue(result.Columns[columnIndex], out var value);
                SetDatabaseValue(
                    worksheet.Cell(startRow + rowIndex + 1, startColumn + columnIndex),
                    value);
            }
        }

        ApplyRanges(worksheet, settings, startRow, startColumn);
        ApplyCells(worksheet, settings, startRow, startColumn);
        ApplyDimensions(worksheet, settings, startRow, startColumn, result.Columns.Count);
        ApplyFreezePane(worksheet, settings);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return new RoutineExcelFile(stream.ToArray(), NormalizeFileName(settings.FileName));
    }

    private static void ValidateSettings(
        RoutineInvocationResponse result,
        ExcelExportSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.SheetName) ||
            settings.SheetName.Length > 31 ||
            InvalidSheetNameRegex().IsMatch(settings.SheetName))
        {
            throw new ArgumentException("The worksheet name is not valid.", nameof(settings));
        }

        var startRow = settings.ReportStartPosition.Row;
        var startColumn = settings.ReportStartPosition.Column;
        if (startRow < 1 || startColumn < 1 ||
            (long)startRow + result.Rows.Count > MaximumExcelRows ||
            (long)startColumn + result.Columns.Count - 1 > MaximumExcelColumns)
        {
            throw new ArgumentException("The report does not fit inside an Excel worksheet.", nameof(settings));
        }

        foreach (var range in settings.Ranges)
        {
            ValidateOffset(startRow, startColumn, range.Top, range.Left);
            ValidateOffset(startRow, startColumn, range.Bottom, range.Right);
            if (range.Bottom < range.Top || range.Right < range.Left)
            {
                throw new ArgumentException("An Excel range has inverted boundaries.", nameof(settings));
            }
        }

        foreach (var cell in settings.Cells)
        {
            ValidateOffset(startRow, startColumn, cell.Row, cell.Column);
        }

        foreach (var (offset, width) in settings.ColumnWidths)
        {
            ValidateOffset(startRow, startColumn, 0, offset);
            if (width is < 0.1 or > 255)
            {
                throw new ArgumentException("Excel column widths must be between 0.1 and 255.", nameof(settings));
            }
        }

        if (settings.ColumnWidths.Count > 256 || settings.RowHeights.Count > 1000)
        {
            throw new ArgumentException("Too many explicit Excel row or column sizes were provided.", nameof(settings));
        }

        foreach (var (offset, height) in settings.RowHeights)
        {
            ValidateOffset(startRow, startColumn, offset, 0);
            if (height is < 0.1 or > 409)
            {
                throw new ArgumentException("Excel row heights must be between 0.1 and 409.", nameof(settings));
            }
        }
    }

    private static void ValidateOffset(
        int startRow,
        int startColumn,
        int rowOffset,
        int columnOffset)
    {
        if (rowOffset < 0 || columnOffset < 0 ||
            (long)startRow + rowOffset > MaximumExcelRows ||
            (long)startColumn + columnOffset > MaximumExcelColumns)
        {
            throw new ArgumentException("An Excel cell position is outside the worksheet.");
        }
    }

    private static void ApplyRanges(
        IXLWorksheet worksheet,
        ExcelExportSettings settings,
        int startRow,
        int startColumn)
    {
        foreach (var rangeSettings in settings.Ranges)
        {
            var range = worksheet.Range(
                startRow + rangeSettings.Top,
                startColumn + rangeSettings.Left,
                startRow + rangeSettings.Bottom,
                startColumn + rangeSettings.Right);
            if (rangeSettings.Merge)
            {
                range.Merge();
            }

            ApplyStyle(range.Style, rangeSettings.Style);
        }
    }

    private static void ApplyCells(
        IXLWorksheet worksheet,
        ExcelExportSettings settings,
        int startRow,
        int startColumn)
    {
        foreach (var cellSettings in settings.Cells)
        {
            var cell = worksheet.Cell(
                startRow + cellSettings.Row,
                startColumn + cellSettings.Column);
            if (cellSettings.Value is { } value)
            {
                SetJsonValue(cell, value, cellSettings.ValueType);
            }

            ApplyStyle(cell.Style, cellSettings.Style);
        }
    }

    private static void ApplyDimensions(
        IXLWorksheet worksheet,
        ExcelExportSettings settings,
        int startRow,
        int startColumn,
        int columnCount)
    {
        if (settings.AutoFitColumns && columnCount > 0)
        {
            worksheet.Columns(startColumn, startColumn + columnCount - 1).AdjustToContents();
        }

        foreach (var (offset, width) in settings.ColumnWidths)
        {
            worksheet.Column(startColumn + offset).Width = width;
        }

        foreach (var (offset, height) in settings.RowHeights)
        {
            worksheet.Row(startRow + offset).Height = height;
        }
    }

    private static void ApplyFreezePane(
        IXLWorksheet worksheet,
        ExcelExportSettings settings)
    {
        if (settings.FreezePane.Rows > 0)
        {
            worksheet.SheetView.FreezeRows(settings.FreezePane.Rows);
        }

        if (settings.FreezePane.Columns > 0)
        {
            worksheet.SheetView.FreezeColumns(settings.FreezePane.Columns);
        }
    }

    private static void ApplyStyle(IXLStyle style, ExcelStyleSettings? settings)
    {
        if (settings is null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(settings.NumberFormat))
        {
            style.NumberFormat.Format = settings.NumberFormat;
        }

        if (!string.IsNullOrWhiteSpace(settings.FillColor))
        {
            style.Fill.BackgroundColor = ParseColor(settings.FillColor);
        }

        if (!string.IsNullOrWhiteSpace(settings.FontColor))
        {
            style.Font.FontColor = ParseColor(settings.FontColor);
        }

        if (!string.IsNullOrWhiteSpace(settings.FontName))
        {
            style.Font.FontName = settings.FontName;
        }

        if (settings.FontSize is { } fontSize)
        {
            style.Font.FontSize = fontSize;
        }

        if (settings.Bold is { } bold)
        {
            style.Font.Bold = bold;
        }

        style.Alignment.Horizontal = settings.HorizontalAlignment switch
        {
            "left" => XLAlignmentHorizontalValues.Left,
            "center" => XLAlignmentHorizontalValues.Center,
            "right" => XLAlignmentHorizontalValues.Right,
            "justify" => XLAlignmentHorizontalValues.Justify,
            _ => style.Alignment.Horizontal
        };
        style.Alignment.Vertical = settings.VerticalAlignment switch
        {
            "top" => XLAlignmentVerticalValues.Top,
            "center" => XLAlignmentVerticalValues.Center,
            "bottom" => XLAlignmentVerticalValues.Bottom,
            "justify" => XLAlignmentVerticalValues.Justify,
            _ => style.Alignment.Vertical
        };

        if (settings.Border)
        {
            var color = string.IsNullOrWhiteSpace(settings.BorderColor)
                ? XLColor.FromHtml("#DCE2DC")
                : ParseColor(settings.BorderColor);
            style.Border.TopBorder = XLBorderStyleValues.Thin;
            style.Border.RightBorder = XLBorderStyleValues.Thin;
            style.Border.BottomBorder = XLBorderStyleValues.Thin;
            style.Border.LeftBorder = XLBorderStyleValues.Thin;
            style.Border.TopBorderColor = color;
            style.Border.RightBorderColor = color;
            style.Border.BottomBorderColor = color;
            style.Border.LeftBorderColor = color;
        }
    }

    private static void SetDatabaseValue(IXLCell cell, object? value)
    {
        switch (value)
        {
            case null:
                cell.Clear(XLClearOptions.Contents);
                break;
            case bool boolean:
                cell.Value = boolean;
                break;
            case byte number:
                cell.Value = number;
                break;
            case sbyte number:
                cell.Value = number;
                break;
            case short number:
                cell.Value = number;
                break;
            case ushort number:
                cell.Value = number;
                break;
            case int number:
                cell.Value = number;
                break;
            case uint number:
                cell.Value = number;
                break;
            case long number when Math.Abs(number) <= 9_999_999_999_999_999L:
                cell.Value = number;
                break;
            case ulong number when number <= 9_999_999_999_999_999UL:
                cell.Value = number;
                break;
            case float number:
                cell.Value = number;
                break;
            case double number:
                cell.Value = number;
                break;
            case decimal number:
                cell.Value = Convert.ToDouble(number, CultureInfo.InvariantCulture);
                break;
            case DateTime dateTime:
                cell.Value = dateTime;
                break;
            case DateTimeOffset dateTimeOffset:
                cell.Value = dateTimeOffset.UtcDateTime;
                break;
            case DateOnly date:
                cell.Value = date.ToDateTime(TimeOnly.MinValue);
                break;
            case TimeSpan duration:
                cell.Value = duration;
                break;
            default:
                SetText(cell, Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty);
                break;
        }
    }

    private static void SetJsonValue(IXLCell cell, JsonElement value, string valueType)
    {
        if (value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            cell.Clear(XLClearOptions.Contents);
            return;
        }

        switch (valueType)
        {
            case "string":
                SetText(cell, value.ValueKind == JsonValueKind.String
                    ? value.GetString() ?? string.Empty
                    : value.GetRawText());
                return;
            case "number" when value.TryGetDouble(out var number):
                cell.Value = number;
                return;
            case "boolean" when value.ValueKind is JsonValueKind.True or JsonValueKind.False:
                cell.Value = value.GetBoolean();
                return;
            case "date" when value.ValueKind == JsonValueKind.String &&
                             DateTimeOffset.TryParse(
                                 value.GetString(),
                                 CultureInfo.InvariantCulture,
                                 DateTimeStyles.RoundtripKind,
                                 out var date):
                cell.Value = date.UtcDateTime;
                return;
            case "auto":
                SetDatabaseValue(cell, value.ValueKind switch
                {
                    JsonValueKind.String => value.GetString(),
                    JsonValueKind.Number when value.TryGetInt64(out var integer) => integer,
                    JsonValueKind.Number => value.GetDouble(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    _ => value.GetRawText()
                });
                return;
            default:
                throw new ArgumentException($"The custom Excel cell value is not a valid {valueType} value.");
        }
    }

    private static void SetText(IXLCell cell, string value)
    {
        cell.Value = value.Length <= 32_767 ? value : value[..32_767];
        cell.Style.IncludeQuotePrefix = value.Length > 0 && value[0] is '=' or '+' or '-' or '@';
    }

    private static XLColor ParseColor(string color) =>
        XLColor.FromHtml(color.StartsWith('#') ? color : $"#{color}");

    private static string NormalizeFileName(string requestedName)
    {
        var baseName = Path.GetFileNameWithoutExtension(requestedName.Trim());
        var normalized = InvalidFileNameRegex().Replace(baseName, "-").Trim(' ', '.', '-');
        if (string.IsNullOrWhiteSpace(normalized))
        {
            normalized = "report";
        }

        return $"{normalized[..Math.Min(normalized.Length, 100)]}.xlsx";
    }

    [GeneratedRegex("[\\\\/:?*\\[\\]]")]
    private static partial Regex InvalidSheetNameRegex();

    [GeneratedRegex("[^A-Za-z0-9._ -]")]
    private static partial Regex InvalidFileNameRegex();
}
