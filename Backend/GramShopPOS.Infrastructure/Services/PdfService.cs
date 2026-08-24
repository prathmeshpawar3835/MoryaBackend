using GramShopPOS.Application.DTOs.Reports;
using GramShopPOS.Application.Exceptions;
using GramShopPOS.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GramShopPOS.Infrastructure.Services;

public sealed class PdfService : IPdfService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public PdfService(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<FileDownload> InvoicePdfAsync(int billId, CancellationToken cancellationToken = default)
    {
        var bill = await _db.Bills.AsNoTracking()
            .Include(b => b.Items)
            .Include(b => b.Payments)
            .Include(b => b.Store)
            .Include(b => b.Customer)
            .FirstOrDefaultAsync(b => b.Id == billId, cancellationToken)
            ?? throw new NotFoundAppException("Bill not found.");
        _currentUser.Access().EnsureStoreAccess(bill.StoreId);
        var settings = await _db.BusinessSettings.AsNoTracking().FirstAsync(cancellationToken);

        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);
                page.Header().Column(col =>
                {
                    col.Item().Text(settings.ShopName).FontSize(18).Bold();
                    col.Item().Text(settings.Address ?? string.Empty);
                    col.Item().Text($"GST: {settings.GSTNumber}  |  {settings.Mobile}");
                    col.Item().Text($"TAX INVOICE / {bill.BillNumber}").FontSize(14).Bold();
                    col.Item().Text($"Date: {bill.BillDate:dd-MMM-yyyy HH:mm}  |  Store: {bill.Store.StoreName}");
                });
                page.Content().PaddingVertical(12).Column(col =>
                {
                    col.Item().Text($"Customer: {bill.Customer?.Name ?? "Walk-in"}  {bill.Customer?.MobileNumber}");
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(3);
                            c.RelativeColumn();
                            c.RelativeColumn();
                            c.RelativeColumn();
                            c.RelativeColumn();
                        });
                        table.Header(h =>
                        {
                            h.Cell().Text("Item").Bold();
                            h.Cell().Text("Qty").Bold();
                            h.Cell().Text("Rate").Bold();
                            h.Cell().Text("Tax").Bold();
                            h.Cell().Text("Total").Bold();
                        });
                        foreach (var item in bill.Items)
                        {
                            table.Cell().Text(item.ProductName);
                            table.Cell().Text(item.Quantity.ToString("0.##"));
                            table.Cell().Text(item.Rate.ToString("0.00"));
                            table.Cell().Text(item.TaxAmount.ToString("0.00"));
                            table.Cell().Text(item.Total.ToString("0.00"));
                        }
                    });
                    col.Item().AlignRight().Text($"Subtotal: {bill.Subtotal:0.00}");
                    col.Item().AlignRight().Text($"Discount: {bill.ItemDiscountTotal + bill.BillDiscount:0.00}");
                    col.Item().AlignRight().Text($"Tax: {bill.TaxAmount:0.00}");
                    col.Item().AlignRight().Text($"Grand Total: {bill.GrandTotal:0.00}").Bold();
                    col.Item().AlignRight().Text($"Paid: {bill.PaidAmount:0.00}  Due: {bill.DueAmount:0.00}");
                    col.Item().PaddingTop(10).Text(settings.InvoiceFooter ?? string.Empty);
                    col.Item().Text(settings.ReturnPolicy ?? string.Empty).FontSize(9);
                });
                page.Footer().AlignCenter().Text("Gram Shop POS").FontSize(9);
            });
        }).GeneratePdf();

        return Pdf(bytes, $"invoice-{bill.BillNumber}.pdf");
    }

    public async Task<FileDownload> LedgerPdfAsync(int customerId, CancellationToken cancellationToken = default)
    {
        var customer = await _db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == customerId, cancellationToken)
            ?? throw new NotFoundAppException("Customer not found.");
        _currentUser.Access().EnsureStoreAccess(customer.StoreId);
        var entries = await _db.CustomerLedgers.AsNoTracking()
            .Where(l => l.CustomerId == customerId)
            .OrderBy(l => l.Id)
            .ToListAsync(cancellationToken);
        var settings = await _db.BusinessSettings.AsNoTracking().FirstAsync(cancellationToken);

        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);
                page.Header().Text($"{settings.ShopName} - Customer Ledger").FontSize(16).Bold();
                page.Content().Column(col =>
                {
                    col.Item().Text($"{customer.Name}  |  {customer.MobileNumber}");
                    col.Item().Text($"Outstanding: {customer.OutstandingBalance:0.00}");
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn();
                            c.RelativeColumn(2);
                            c.RelativeColumn();
                            c.RelativeColumn();
                            c.RelativeColumn();
                        });
                        table.Header(h =>
                        {
                            h.Cell().Text("Date").Bold();
                            h.Cell().Text("Description").Bold();
                            h.Cell().Text("Debit").Bold();
                            h.Cell().Text("Credit").Bold();
                            h.Cell().Text("Balance").Bold();
                        });
                        foreach (var e in entries)
                        {
                            table.Cell().Text(e.TransactionDate.ToString("dd-MMM-yyyy"));
                            table.Cell().Text(e.Description);
                            table.Cell().Text(e.Debit.ToString("0.00"));
                            table.Cell().Text(e.Credit.ToString("0.00"));
                            table.Cell().Text(e.Balance.ToString("0.00"));
                        }
                    });
                });
            });
        }).GeneratePdf();
        return Pdf(bytes, $"ledger-{customer.MobileNumber}.pdf");
    }

    public async Task<FileDownload> ReturnNotePdfAsync(int returnId, CancellationToken cancellationToken = default)
    {
        var ret = await _db.Returns.AsNoTracking().Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == returnId, cancellationToken)
            ?? throw new NotFoundAppException("Return not found.");
        _currentUser.Access().EnsureStoreAccess(ret.StoreId);
        var settings = await _db.BusinessSettings.AsNoTracking().FirstAsync(cancellationToken);
        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);
                page.Header().Text($"{settings.ShopName} - Credit Note {ret.ReturnNumber}").FontSize(16).Bold();
                page.Content().Column(col =>
                {
                    col.Item().Text($"Original Bill: {ret.OriginalBillNumber}");
                    col.Item().Text($"Date: {ret.ReturnDate:dd-MMM-yyyy}  Amount: {ret.ReturnAmount:0.00}");
                    foreach (var item in ret.Items)
                    {
                        col.Item().Text($"{item.ProductName}  x {item.Quantity}  = {item.Total:0.00}");
                    }
                });
            });
        }).GeneratePdf();
        return Pdf(bytes, $"credit-note-{ret.ReturnNumber}.pdf");
    }

    public FileDownload SalesReportPdf(SalesReportDto report)
    {
        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);
                page.Header().Text("Sales Report").FontSize(16).Bold();
                page.Content().Column(col =>
                {
                    col.Item().Text($"Bills: {report.BillCount}  Sales: {report.TotalSales:0.00}  Tax: {report.Tax:0.00}");
                    foreach (var bill in report.Bills.Items)
                    {
                        col.Item().Text($"{bill.BillNumber}  {bill.BillDate:dd-MMM}  {bill.GrandTotal:0.00}");
                    }
                });
            });
        }).GeneratePdf();
        return Pdf(bytes, "sales-report.pdf");
    }

    public FileDownload InventoryReportPdf(IReadOnlyList<InventoryReportRowDto> rows)
    {
        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);
                page.Header().Text("Inventory Report").FontSize(16).Bold();
                page.Content().Column(col =>
                {
                    foreach (var row in rows)
                    {
                        col.Item().Text($"{row.StoreCode} {row.ProductCode} {row.ProductName} Qty:{row.Quantity:0.##}");
                    }
                });
            });
        }).GeneratePdf();
        return Pdf(bytes, "inventory-report.pdf");
    }

    private static FileDownload Pdf(byte[] bytes, string name) => new()
    {
        Content = bytes,
        ContentType = "application/pdf",
        FileName = name
    };
}
