using GramShopPOS.Application.DTOs.Reports;
using GramShopPOS.Application.Exceptions;
using GramShopPOS.Application.Interfaces;
using GramShopPOS.Domain.Enums;
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
            .Include(b => b.SalesPerson)
            .Include(b => b.Payments)
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
                    col.Item().PaddingBottom(8).Column(cust =>
                    {
                        cust.Item().Text("Customer Details").Bold();
                        cust.Item().Text($"Customer Name: {bill.Customer?.Name ?? "Walk-in Customer"}");
                        cust.Item().Text($"Mobile: {bill.Customer?.MobileNumber ?? "—"}");
                        cust.Item().Text($"Customer Code: {bill.Customer?.ReferralCode ?? "—"}");
                        if (!string.IsNullOrWhiteSpace(bill.Customer?.Address))
                        {
                            cust.Item().Text($"Address: {bill.Customer.Address}");
                        }
                    });
                    if (bill.ReferralDiscount > 0 || !string.IsNullOrWhiteSpace(bill.ReferrerCode))
                    {
                        col.Item().PaddingBottom(8).Column(refCol =>
                        {
                            refCol.Item().Text("Referral Information").Bold();
                            refCol.Item().Text($"Referral Customer: {bill.ReferrerName}");
                            refCol.Item().Text($"Referral Code: {bill.ReferrerCode}");
                            if (bill.ReferralDiscountPercent > 0)
                            {
                                refCol.Item().Text($"Referral Discount: {bill.ReferralDiscountPercent:0.##}%");
                            }
                            refCol.Item().Text($"Referral Discount Amount: -{bill.ReferralDiscount:0.00}");
                        });
                    }
                    if (bill.BirthdayDiscount > 0)
                    {
                        col.Item().PaddingBottom(8).Column(bday =>
                        {
                            bday.Item().Text("Birthday Offer").Bold();
                            bday.Item().Text($"Birthday Offer: {bill.BirthdayOfferName ?? "Birthday Offer"}");
                            if (bill.BirthdayDiscountPercent > 0)
                            {
                                bday.Item().Text($"Birthday Offer Percentage: {bill.BirthdayDiscountPercent:0.##}%");
                            }
                            bday.Item().Text($"Birthday Discount: -{bill.BirthdayDiscount:0.00}");
                        });
                    }
                    col.Item().Text($"Sales Person: {bill.SalesPerson?.FullName}");
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
                    if (bill.ItemDiscountTotal > 0) col.Item().AlignRight().Text($"Item Discount: -{bill.ItemDiscountTotal:0.00}");
                    if (bill.ReferralDiscount > 0)
                    {
                        var pct = bill.ReferralDiscountPercent > 0 ? $" ({bill.ReferralDiscountPercent:0.##}%)" : string.Empty;
                        col.Item().AlignRight().Text($"Referral Discount{pct}: -{bill.ReferralDiscount:0.00}");
                    }
                    if (bill.BirthdayDiscount > 0)
                    {
                        var name = string.IsNullOrWhiteSpace(bill.BirthdayOfferName) ? "Birthday Offer" : bill.BirthdayOfferName;
                        var pct = bill.BirthdayDiscountPercent > 0 ? $" ({bill.BirthdayDiscountPercent:0.##}%)" : string.Empty;
                        col.Item().AlignRight().Text($"{name}{pct}: -{bill.BirthdayDiscount:0.00}");
                    }
                    if (bill.StoreDiscountAmount > 0)
                    {
                        var name = string.IsNullOrWhiteSpace(bill.StoreDiscountName) ? "Store Discount" : bill.StoreDiscountName;
                        var pct = bill.StoreDiscountPercent > 0 ? $" ({bill.StoreDiscountPercent:0.##}%)" : string.Empty;
                        col.Item().AlignRight().Text($"{name}{pct}: -{bill.StoreDiscountAmount:0.00}");
                    }
                    var otherDiscount = bill.BillDiscount - bill.ReferralDiscount - bill.StoreDiscountAmount - bill.BirthdayDiscount;
                    if (otherDiscount > 0) col.Item().AlignRight().Text($"Other Discount: -{otherDiscount:0.00}");
                    var totalDiscount = bill.ItemDiscountTotal + bill.BillDiscount;
                    if (totalDiscount > 0) col.Item().AlignRight().Text($"Total Discount: -{totalDiscount:0.00}");
                    col.Item().AlignRight().Text($"Tax: {bill.TaxAmount:0.00}");
                    col.Item().AlignRight().Text($"Grand Total: {bill.GrandTotal:0.00}").Bold();
                    if (bill.ReturnAdjustment > 0) col.Item().AlignRight().Text($"Return Adjustment: -{bill.ReturnAdjustment:0.00}");
                    if (bill.ExchangeAdjustment > 0) col.Item().AlignRight().Text($"Exchange Adjustment: -{bill.ExchangeAdjustment:0.00}");
                    if (bill.BuybackAdjustment > 0) col.Item().AlignRight().Text($"Buyback Adjustment: -{bill.BuybackAdjustment:0.00}");
                    if (bill.WalletRedeemed > 0) col.Item().AlignRight().Text($"Customer Credit Used: -{bill.WalletRedeemed:0.00}");
                    if (bill.CreditGenerated > 0) col.Item().AlignRight().Text($"Credit Generated: {bill.CreditGenerated:0.00}");
                    col.Item().AlignRight().Text($"Final Payable: {bill.PayableAmount:0.00}").Bold();
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
            .Include(r => r.Customer)
            .Include(r => r.SalesPerson)
            .Include(r => r.Store)
            .Include(r => r.OriginalBill)
            .Include(r => r.ExchangeBill).ThenInclude(b => b!.Items)
            .Include(r => r.AppliedToBill)
            .FirstOrDefaultAsync(r => r.Id == returnId, cancellationToken)
            ?? throw new NotFoundAppException("Return not found.");
        _currentUser.Access().EnsureStoreAccess(ret.StoreId);
        var settings = await _db.BusinessSettings.AsNoTracking().FirstAsync(cancellationToken);
        var title = ret.ReturnKind switch
        {
            ReturnKind.Exchange => "Exchange Receipt",
            ReturnKind.Buyback => "Buyback Receipt",
            _ => "Return Receipt / Credit Note"
        };
        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);
                page.Header().Text($"{settings.ShopName} - {title} {ret.ReturnNumber}").FontSize(16).Bold();
                page.Content().Column(col =>
                {
                    col.Item().Text($"Original Invoice: {ret.OriginalBillNumber}");
                    if (ret.AppliedToBill != null) col.Item().Text($"Applied to current sale: {ret.AppliedToBill.BillNumber}");
                    if (ret.ExchangeBill != null)
                    {
                        col.Item().Text($"Linked Sale / Exchange Invoice: {ret.ExchangeBill.BillNumber}");
                        col.Item().Text($"New product value: {ret.ExchangeBill.GrandTotal:0.00}");
                        col.Item().Text($"Difference / credit adjustment: {(ret.ExchangeBill.GrandTotal - ret.ReturnAmount):0.00}");
                    }
                    col.Item().Text($"Customer: {ret.Customer?.Name}  {ret.Customer?.MobileNumber}  Code: {ret.Customer?.ReferralCode}");
                    col.Item().Text($"Store: {ret.Store.StoreName}  Sales Person: {ret.SalesPerson?.FullName}");
                    col.Item().Text($"Date: {ret.ReturnDate:dd-MMM-yyyy HH:mm}  Amount: {ret.ReturnAmount:0.00}");
                    col.Item().Text("Original / returned products:");
                    foreach (var item in ret.Items)
                    {
                        col.Item().Text($"  {item.ProductName}  x {item.Quantity}  = {item.Total:0.00}");
                    }
                    if (ret.ExchangeBill?.Items.Count > 0)
                    {
                        col.Item().Text("New products:");
                        foreach (var item in ret.ExchangeBill.Items)
                        {
                            col.Item().Text($"  {item.ProductName}  x {item.Quantity}  = {item.Total:0.00}");
                        }
                    }
                });
            });
        }).GeneratePdf();
        return Pdf(bytes, $"{ret.ReturnKind.ToString().ToLowerInvariant()}-{ret.ReturnNumber}.pdf");
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
