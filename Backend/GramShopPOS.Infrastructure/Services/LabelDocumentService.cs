using System.IO.Compression;
using System.Text;
using GramShopPOS.Application.DTOs.Catalog;
using GramShopPOS.Application.DTOs.Reports;
using GramShopPOS.Application.Interfaces;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GramShopPOS.Infrastructure.Services;

public sealed class LabelDocumentService : ILabelDocumentService
{
    public LabelDocumentService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] QrPng(string uniqueNumber)
    {
        var payload = (uniqueNumber ?? string.Empty).Trim().ToUpperInvariant();
        if (payload.Length == 0)
        {
            throw new ArgumentException("Unique number is required to generate a QR code.", nameof(uniqueNumber));
        }

        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.M);
        using var png = new PngByteQRCode(data);
        return png.GetGraphic(20, true);
    }

    public byte[] BarcodePng(string uniqueNumber)
    {
        var svg = Code128Encoder.ToSvg(uniqueNumber);
        return Encoding.UTF8.GetBytes(svg);
    }

    public FileDownload QrZip(IReadOnlyList<ProductUnitLabelDto> units)
    {
        using var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            foreach (var unit in units)
            {
                var qrEntry = zip.CreateEntry($"{unit.UniqueNumber}.png");
                using (var entryStream = qrEntry.Open())
                {
                    var png = QrPng(unit.UniqueNumber);
                    entryStream.Write(png, 0, png.Length);
                }

                var barcodeEntry = zip.CreateEntry($"{unit.UniqueNumber}-code128.svg");
                using (var entryStream = barcodeEntry.Open())
                {
                    var svg = Encoding.UTF8.GetBytes(Code128Encoder.ToSvg(unit.UniqueNumber));
                    entryStream.Write(svg, 0, svg.Length);
                }
            }
        }

        return new FileDownload
        {
            Content = stream.ToArray(),
            ContentType = "application/zip",
            FileName = "jewellery-qr-codes.zip"
        };
    }

    public FileDownload TagsPdf(IReadOnlyList<ProductUnitLabelDto> units, decimal widthMm, decimal heightMm)
    {
        var width = Math.Clamp((float)widthMm, 20f, 120f);
        var height = Math.Clamp((float)heightMm, 15f, 80f);
        var bytes = Document.Create(container =>
        {
            foreach (var unit in units)
            {
                var qr = QrPng(unit.UniqueNumber);
                var modules = Code128Encoder.ToModules(unit.UniqueNumber);
                container.Page(page =>
                {
                    page.Size(new PageSize(width, height, Unit.Millimetre));
                    page.Margin(1.2f, Unit.Millimetre);
                    page.DefaultTextStyle(x => x.FontSize(7).FontColor(Colors.Grey.Darken4));
                    page.Content().Row(row =>
                    {
                        row.ConstantItem(height - 4).Image(qr).FitArea();
                        row.RelativeItem().PaddingLeft(1.5f).Column(col =>
                        {
                            col.Item().Text(unit.UniqueNumber).Bold().FontSize(8);
                            col.Item().Text(unit.CategoryName.ToUpperInvariant()).FontSize(6);
                            col.Item().Text($"MRP: ₹{unit.MRP:N0}");
                            col.Item().Text($"Selling: ₹{unit.SellingPrice:N0}").Bold();
                            col.Item().PaddingTop(1).Height(8).Row(bars =>
                            {
                                foreach (var module in modules)
                                {
                                    if (module)
                                    {
                                        bars.RelativeItem().Background(Colors.Black);
                                    }
                                    else
                                    {
                                        bars.RelativeItem().Background(Colors.White);
                                    }
                                }
                            });
                        });
                    });
                });
            }
        }).GeneratePdf();

        return new FileDownload
        {
            Content = bytes,
            ContentType = "application/pdf",
            FileName = "jewellery-tags.pdf"
        };
    }
}

internal static class Code128Encoder
{
    private static readonly string[] Patterns =
    [
        "11011001100","11001101100","11001100110","10010011000","10010001100","10001001100","10011001000","10011000100","10001100100","11001001000",
        "11001000100","11000100100","10110011100","10011011100","10011001110","10111001100","10011101100","10011100110","11001110010","11001011100",
        "11001001110","11011100100","11001110100","11101101110","11101001100","11100101100","11100100110","11101100100","11100110100","11100110010",
        "11011011000","11011000110","11000110110","10100011000","10001011000","10001000110","10110001000","10001101000","10001100010","11010001000",
        "11000101000","11000100010","10110111000","10110001110","10001101110","10111011000","10111000110","10001110110","11101110110","11010001110",
        "11000101110","11011101000","11011100010","11011101110","11101011000","11101000110","11100010110","11101101000","11101100010","11100011010",
        "11101111010","11001000010","11110001010","10100110000","10100001100","10010110000","10010000110","10000101100","10000100110","10110010000",
        "10110000100","10011010000","10011000010","10000110100","10000110010","11000010010","11001010000","11110111010","11000010100","10001111010",
        "10100111100","10010111100","10010011110","10111100100","10011110100","10011110010","11110100100","11110010100","11110010010","11011011110",
        "11011110110","11110110110","10101111000","10100011110","10001011110","10111101000","10111100010","11110101000","11110100010","10111011110",
        "10111101110","11101011110","11110101110","11010000100","11010010000","11010011100","11000111010"
    ];

    public static bool[] ToModules(string value)
    {
        var codes = new List<int> { 104 };
        var checksum = 104;
        var weight = 1;
        foreach (var ch in value)
        {
            var code = ch >= 32 && ch <= 126 ? ch - 32 : 31;
            codes.Add(code);
            checksum += code * weight;
            weight++;
        }

        codes.Add(checksum % 103);
        codes.Add(106);

        var modules = new List<bool>();
        foreach (var code in codes)
        {
            var pattern = Patterns[Math.Clamp(code, 0, Patterns.Length - 1)];
            foreach (var bit in pattern)
            {
                modules.Add(bit == '1');
            }
        }

        return [.. modules];
    }

    public static string ToSvg(string value)
    {
        var modules = ToModules(value);
        const int barWidth = 2;
        const int height = 48;
        var width = modules.Length * barWidth + 16;
        var sb = new StringBuilder();
        sb.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{width}\" height=\"{height + 16}\" viewBox=\"0 0 {width} {height + 16}\">");
        sb.Append("<rect width=\"100%\" height=\"100%\" fill=\"white\"/>");
        var x = 8;
        foreach (var module in modules)
        {
            if (module)
            {
                sb.Append($"<rect x=\"{x}\" y=\"2\" width=\"{barWidth}\" height=\"{height}\" fill=\"black\"/>");
            }

            x += barWidth;
        }

        sb.Append($"<text x=\"{width / 2}\" y=\"{height + 13}\" text-anchor=\"middle\" font-size=\"10\" font-family=\"monospace\">{System.Security.SecurityElement.Escape(value)}</text>");
        sb.Append("</svg>");
        return sb.ToString();
    }
}
