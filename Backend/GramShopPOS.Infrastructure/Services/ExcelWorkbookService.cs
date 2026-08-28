using System.Globalization;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using GramShopPOS.Application.DTOs.Reports;
using GramShopPOS.Application.Interfaces;

namespace GramShopPOS.Infrastructure.Services;

public sealed class ExcelWorkbookService : IExcelWorkbookService
{
    public FileDownload CreateProductImportTemplate()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Products");
        var headers = new[]
        {
            "Product Code", "Product Name", "Category", "Unit", "Purchase Price", "Selling Price",
            "MRP", "Tax %", "Opening Stock", "Store Code", "Barcode", "Quantity"
        };
        for (var i = 0; i < headers.Length; i++)
        {
            sheet.Cell(1, i + 1).Value = headers[i];
            sheet.Cell(1, i + 1).Style.Font.Bold = true;
        }

        sheet.Cell(2, 1).Value = "1G-CHAIN-001";
        sheet.Cell(2, 2).Value = "1 Gram Gold Chain";
        sheet.Cell(2, 3).Value = "Chains";
        sheet.Cell(2, 4).Value = "PCS";
        sheet.Cell(2, 5).Value = 4500;
        sheet.Cell(2, 6).Value = 5200;
        sheet.Cell(2, 7).Value = 5500;
        sheet.Cell(2, 8).Value = 3;
        sheet.Cell(2, 9).Value = 10;
        sheet.Cell(2, 10).Value = "STORE01";
        sheet.Cell(2, 11).Value = "890000000001";
        sheet.Cell(2, 12).Value = 10;
        sheet.Cell(3, 1).Value = "";
        sheet.Cell(3, 2).Value = "Gold Ring";
        sheet.Cell(3, 3).Value = "Ring";
        sheet.Cell(3, 6).Value = 22500;
        sheet.Cell(3, 7).Value = 25000;
        sheet.Cell(3, 12).Value = 10;
        sheet.Columns().AdjustToContents();
        return ToDownload(workbook, "product-import-template.xlsx");
    }

    public IReadOnlyList<Dictionary<string, string>> ReadTable(Stream stream, string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (ext is ".csv")
        {
            using var reader = new StreamReader(stream);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                TrimOptions = TrimOptions.Trim,
                MissingFieldFound = null,
                HeaderValidated = null
            });
            csv.Read();
            csv.ReadHeader();
            var rows = new List<Dictionary<string, string>>();
            while (csv.Read())
            {
                var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var header in csv.HeaderRecord ?? [])
                {
                    dict[header] = csv.GetField(header) ?? string.Empty;
                }

                if (dict.Values.Any(v => !string.IsNullOrWhiteSpace(v)))
                {
                    rows.Add(dict);
                }
            }

            return rows;
        }

        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheets.First();
        var firstRow = sheet.FirstRowUsed() ?? throw new InvalidOperationException("The file is empty.");
        var headers2 = firstRow.CellsUsed().Select(c => c.GetString().Trim()).ToList();
        var result = new List<Dictionary<string, string>>();
        foreach (var row in sheet.RowsUsed().Skip(1))
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < headers2.Count; i++)
            {
                dict[headers2[i]] = row.Cell(i + 1).GetString().Trim();
            }

            if (dict.Values.Any(v => !string.IsNullOrWhiteSpace(v)))
            {
                result.Add(dict);
            }
        }

        return result;
    }

    public FileDownload CreateWorkbook(string sheetName, string fileName, IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<object?>> rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet(sheetName);
        for (var i = 0; i < headers.Count; i++)
        {
            sheet.Cell(1, i + 1).Value = headers[i];
            sheet.Cell(1, i + 1).Style.Font.Bold = true;
        }

        var r = 2;
        foreach (var row in rows)
        {
            for (var c = 0; c < row.Count; c++)
            {
                var value = row[c];
                if (value is decimal d)
                {
                    sheet.Cell(r, c + 1).Value = d;
                }
                else if (value is DateTime dt)
                {
                    sheet.Cell(r, c + 1).Value = dt;
                }
                else if (value is bool b)
                {
                    sheet.Cell(r, c + 1).Value = b;
                }
                else
                {
                    sheet.Cell(r, c + 1).Value = value?.ToString() ?? string.Empty;
                }
            }

            r++;
        }

        sheet.Columns().AdjustToContents();
        return ToDownload(workbook, fileName);
    }

    private static FileDownload ToDownload(XLWorkbook workbook, string fileName)
    {
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return new FileDownload
        {
            Content = stream.ToArray(),
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            FileName = fileName
        };
    }
}
