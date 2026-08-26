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
                        cust.Item().Text($"Customer Code: {bill.Customer?.CustomerCode ?? "—"}");
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
                    col.Item().Text($"{customer.Name}  |  {customer.CustomerCode}  |  {customer.MobileNumber}");
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
            .Include(r => r.User)
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
            _ => "Return Receipt"
        };
        var rows = new List<(string Label, string Value)>
        {
            ("Store", ret.Store.StoreName),
            ("Store contact", ret.Store.ContactNumber ?? settings.Mobile ?? string.Empty),
            ("Customer", ret.Customer?.Name ?? "Walk-in"),
            ("Customer code", ret.Customer?.CustomerCode ?? "—"),
            ("Mobile", ret.Customer?.MobileNumber ?? "—"),
            ("Transaction number", ret.ReturnNumber),
            ("Date and time", ret.ReturnDate.ToString("dd-MMM-yyyy HH:mm")),
            ("Transaction type", title),
            ("Original invoice", ret.OriginalBillNumber),
        };
        if (ret.AppliedToBill != null) rows.Add(("Applied to sale", ret.AppliedToBill.BillNumber));
        if (ret.ExchangeBill != null)
        {
            rows.Add(("Linked invoice", ret.ExchangeBill.BillNumber));
            rows.Add(("New product value", ret.ExchangeBill.GrandTotal.ToString("0.00")));
            rows.Add(("Difference", (ret.ExchangeBill.GrandTotal - ret.ReturnAmount).ToString("0.00")));
        }
        rows.Add(("Original value", ret.GrossAmount.ToString("0.00")));
        if (ret.DeductionAmount > 0)
        {
            rows.Add(("Deduction", $"{ret.DeductionPercent:0.##}% / {ret.DeductionAmount:0.00}"));
        }
        rows.Add(("Final amount", ret.ReturnAmount.ToString("0.00")));
        rows.Add(("Payment / adjustment", ret.ReturnKind == ReturnKind.Buyback ? "Buyback payout / ledger credit" : "Adjusted to customer ledger / current sale"));
        rows.Add(("Received by", ret.SalesPerson?.FullName ?? ret.User?.FullName ?? "—"));
        foreach (var item in ret.Items)
        {
            rows.Add(("Item", $"{item.ProductName} ({item.ProductCode}) × {item.Quantity:0.##} = {item.Total:0.00}"));
        }
        if (ret.ExchangeBill?.Items.Count > 0)
        {
            foreach (var item in ret.ExchangeBill.Items)
            {
                rows.Add(("New item", $"{item.ProductName} × {item.Quantity:0.##} = {item.Total:0.00}"));
            }
        }
        return ComposeReceipt(settings.ShopName, $"{title} {ret.ReturnNumber}", [.. rows], $"{ret.ReturnKind.ToString().ToLowerInvariant()}-{ret.ReturnNumber}.pdf");
    }

    public async Task<FileDownload> LedgerReceiptPdfAsync(int customerId, int entryId, CancellationToken cancellationToken = default)
    {
        var customer = await _db.Customers.AsNoTracking().Include(c => c.Store)
            .FirstOrDefaultAsync(c => c.Id == customerId && !c.IsDeleted, cancellationToken)
            ?? throw new NotFoundAppException("Customer not found.");
        _currentUser.Access().EnsureStoreAccess(customer.StoreId);
        var entry = await _db.CustomerLedgers.AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == entryId && l.CustomerId == customerId, cancellationToken)
            ?? throw new NotFoundAppException("Ledger transaction not found.");
        var settings = await _db.BusinessSettings.AsNoTracking().FirstAsync(cancellationToken);
        var receivedBy = await _db.Users.AsNoTracking().Where(u => u.Id == entry.UserId).Select(u => u.FullName).FirstOrDefaultAsync(cancellationToken) ?? string.Empty;
        string? paymentMode = null;
        if (entry.ReferenceId.HasValue)
        {
            var payment = await _db.Payments.AsNoTracking().FirstOrDefaultAsync(p => p.Id == entry.ReferenceId.Value, cancellationToken);
            paymentMode = payment?.PaymentMode.ToString();
        }

        var amount = entry.Debit > 0 ? entry.Debit : entry.Credit;
        var bytes = ComposeReceipt(
            settings.ShopName,
            TitleForLedger(entry.TransactionType),
            [
                ("Store", customer.Store?.StoreName ?? string.Empty),
                ("Store contact", customer.Store?.ContactNumber ?? settings.Mobile ?? string.Empty),
                ("Customer", customer.Name),
                ("Customer code", customer.CustomerCode),
                ("Mobile", customer.MobileNumber),
                ("Transaction no.", entry.ReferenceNumber ?? $"LED-{entry.Id}"),
                ("Date & time", entry.TransactionDate.ToString("dd-MMM-yyyy HH:mm")),
                ("Transaction type", entry.TransactionType.ToString()),
                ("Amount", amount.ToString("0.00")),
                ("Debit", entry.Debit.ToString("0.00")),
                ("Credit", entry.Credit.ToString("0.00")),
                ("Balance", entry.Balance.ToString("0.00")),
                ("Payment mode", paymentMode ?? (entry.TransactionType == LedgerTransactionType.WalletRedeem ? "Customer credit" : "—")),
                ("Reference", entry.ReferenceNumber ?? "—"),
                ("Received by", receivedBy),
                ("Description", entry.Description)
            ],
            $"ledger-receipt-{entry.Id}.pdf");
        return bytes;
    }

    public async Task<FileDownload> RepairReceiptPdfAsync(int jobId, CancellationToken cancellationToken = default)
    {
        var job = await _db.RepairJobs.AsNoTracking()
            .Include(j => j.Store)
            .Include(j => j.Customer)
            .Include(j => j.User)
            .FirstOrDefaultAsync(j => j.Id == jobId && !j.IsDeleted, cancellationToken)
            ?? throw new NotFoundAppException("Repair / polish job not found.");
        _currentUser.Access().EnsureStoreAccess(job.StoreId);
        var settings = await _db.BusinessSettings.AsNoTracking().FirstAsync(cancellationToken);
        var charge = job.FinalAmount > 0 ? job.FinalAmount : job.EstimatedAmount;
        var due = Math.Max(0, charge - job.PaidAmount);
        var title = job.JobType == RepairJobType.Polish ? "Polish Receipt" : "Repair Receipt";
        return ComposeReceipt(
            settings.ShopName,
            $"{title} {job.JobNumber}",
            [
                ("Store", job.Store.StoreName),
                ("Store contact", job.Store.ContactNumber ?? settings.Mobile ?? string.Empty),
                ("Customer", job.CustomerName),
                ("Customer code", job.Customer?.CustomerCode ?? "—"),
                ("Mobile", job.MobileNumber),
                ("Job number", job.JobNumber),
                ("Date & time", job.ReceivedDate.ToString("dd-MMM-yyyy HH:mm")),
                ("Type", job.JobType.ToString()),
                ("Status", job.Status.ToString()),
                ("Product", job.ProductName),
                ("Description", job.ProductDetails ?? job.Notes ?? "—"),
                ("Estimated amount", job.EstimatedAmount.ToString("0.00")),
                ("Final amount", charge.ToString("0.00")),
                ("Paid amount", job.PaidAmount.ToString("0.00")),
                ("Due amount", due.ToString("0.00")),
                ("Payment mode", job.PaymentMode?.ToString() ?? "—"),
                ("Reference", job.PaymentReference ?? "—"),
                ("Received by", job.User?.FullName ?? string.Empty)
            ],
            $"{job.JobType.ToString().ToLowerInvariant()}-{job.JobNumber}.pdf");
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

    private static string TitleForLedger(LedgerTransactionType type) => type switch
    {
        LedgerTransactionType.PaymentReceived => "Payment Receipt",
        LedgerTransactionType.RepairPayment => "Repair Payment Receipt",
        LedgerTransactionType.PolishPayment => "Polish Payment Receipt",
        LedgerTransactionType.RepairCharge => "Repair Charge Receipt",
        LedgerTransactionType.PolishCharge => "Polish Charge Receipt",
        LedgerTransactionType.WalletRedeem => "Customer Credit Receipt",
        LedgerTransactionType.Return => "Return Receipt",
        LedgerTransactionType.ExchangeAdjustment => "Exchange Receipt",
        LedgerTransactionType.Buyback => "Buyback Receipt",
        LedgerTransactionType.Sale => "Sales Receipt",
        _ => "Transaction Receipt"
    };

    private static FileDownload ComposeReceipt(string shopName, string title, (string Label, string Value)[] rows, string fileName)
    {
        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A5);
                page.Margin(28);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(Colors.Grey.Darken4));
                page.Header().Column(col =>
                {
                    col.Item().Text(shopName).FontSize(16).Bold().FontColor(Color.FromHex("0f2744"));
                    col.Item().PaddingTop(2).Text(title).FontSize(12).Bold().FontColor(Color.FromHex("c9a227"));
                    col.Item().PaddingTop(6).LineHorizontal(1).LineColor(Color.FromHex("c9a227"));
                });
                page.Content().PaddingTop(12).Column(col =>
                {
                    foreach (var (label, value) in rows)
                    {
                        col.Item().PaddingVertical(2).Row(row =>
                        {
                            row.RelativeItem(2).Text(label).FontColor(Colors.Grey.Darken1);
                            row.RelativeItem(3).Text(value).Bold();
                        });
                    }
                });
                page.Footer().AlignCenter().Text("Thank you · Gram Shop POS").FontSize(8).FontColor(Colors.Grey.Medium);
            });
        }).GeneratePdf();
        return Pdf(bytes, fileName);
    }

    private static FileDownload Pdf(byte[] bytes, string name) => new()
    {
        Content = bytes,
        ContentType = "application/pdf",
        FileName = name
    };
}
