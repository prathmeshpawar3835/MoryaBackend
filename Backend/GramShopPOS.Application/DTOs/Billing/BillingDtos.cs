using GramShopPOS.Domain.Enums;

namespace GramShopPOS.Application.DTOs.Billing;

public class CreateBillRequest
{
    public int StoreId { get; set; }
    public int? CustomerId { get; set; }
    public decimal BillDiscount { get; set; }
    public string? Notes { get; set; }
    public int? HeldBillId { get; set; }
    public string? ReferralCode { get; set; }
    public string? ReferringMobileNumber { get; set; }
    public decimal WalletRedeemAmount { get; set; }
    public int? SalesPersonId { get; set; }
    public int? StoreDiscountId { get; set; }
    public IReadOnlyList<CreateBillItemRequest> Items { get; set; } = [];
    public IReadOnlyList<CreatePaymentRequest> Payments { get; set; } = [];
    public IReadOnlyList<SaleAdjustmentRequest> Adjustments { get; set; } = [];
    public int? BirthdayOfferId { get; set; }
}

public class CreateBillItemRequest
{
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal DiscountAmount { get; set; }
    public IReadOnlyList<int>? ProductUnitIds { get; set; }
}

public class CreatePaymentRequest
{
    public PaymentMode PaymentMode { get; set; }
    public decimal Amount { get; set; }
    public string? ReferenceNumber { get; set; }
}

public class SaleAdjustmentRequest
{
    public ReturnKind Kind { get; set; } = ReturnKind.Return;
    public int OriginalBillId { get; set; }
    public string? Reason { get; set; }
    public decimal? Amount { get; set; }
    public IReadOnlyList<CreateReturnItemRequest> Items { get; set; } = [];
}

public class BillDto
{
    public int Id { get; set; }
    public int StoreId { get; set; }
    public string StoreCode { get; set; } = string.Empty;
    public string StoreName { get; set; } = string.Empty;
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerMobile { get; set; }
    public int SalesPersonId { get; set; }
    public string SalesPersonName { get; set; } = string.Empty;
    public string BillNumber { get; set; } = string.Empty;
    public DateTime BillDate { get; set; }
    public BillType BillType { get; set; }
    public BillStatus Status { get; set; }
    public decimal Subtotal { get; set; }
    public decimal ItemDiscountTotal { get; set; }
    public decimal BillDiscount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal DueAmount { get; set; }
    public decimal WalletRedeemed { get; set; }
    public decimal ReferralDiscount { get; set; }
    public decimal ReferralDiscountPercent { get; set; }
    public int? ReferrerCustomerId { get; set; }
    public string? ReferrerName { get; set; }
    public string? ReferrerCode { get; set; }
    public decimal ReferrerBenefitPercent { get; set; }
    public decimal ReferrerBenefitAmount { get; set; }
    public decimal StoreDiscountAmount { get; set; }
    public decimal StoreDiscountPercent { get; set; }
    public int? StoreDiscountId { get; set; }
    public string? StoreDiscountName { get; set; }
    public decimal BirthdayDiscount { get; set; }
    public decimal BirthdayDiscountPercent { get; set; }
    public int? BirthdayOfferId { get; set; }
    public string? BirthdayOfferName { get; set; }
    public string? CustomerCode { get; set; }
    public string? CustomerReferralCode { get; set; }
    public decimal ReturnAdjustment { get; set; }
    public decimal ExchangeAdjustment { get; set; }
    public decimal BuybackAdjustment { get; set; }
    public decimal CreditGenerated { get; set; }
    public decimal PayableAmount { get; set; }
    public string? Notes { get; set; }
    public IReadOnlyList<BillItemDto> Items { get; set; } = [];
    public IReadOnlyList<PaymentDto> Payments { get; set; } = [];
    public IReadOnlyList<ReturnDto> Adjustments { get; set; } = [];
}

public class BillItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Rate { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public decimal ReturnedQuantity { get; set; }
    public decimal ExchangedQuantity { get; set; }
    public decimal RemainingQuantity { get; set; }
    public decimal BoughtBackQuantity { get; set; }
    public BillItemFulfillmentStatus FulfillmentStatus { get; set; }
}

public class PaymentDto
{
    public int Id { get; set; }
    public PaymentMode PaymentMode { get; set; }
    public decimal Amount { get; set; }
    public string? ReferenceNumber { get; set; }
    public DateTime PaymentDate { get; set; }
}

public class BillListRequest : Common.PagedRequest
{
    public BillStatus? Status { get; set; }
    public int? CustomerId { get; set; }
}

public class HeldBillRequest
{
    public int StoreId { get; set; }
    public int? CustomerId { get; set; }
    public decimal BillDiscount { get; set; }
    public string? Notes { get; set; }
    public IReadOnlyList<CreateBillItemRequest> Items { get; set; } = [];
}

public class HeldBillDto
{
    public int Id { get; set; }
    public int StoreId { get; set; }
    public int? CustomerId { get; set; }
    public string HoldReference { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public decimal BillDiscount { get; set; }
    public DateTime CreatedDate { get; set; }
    public IReadOnlyList<CreateBillItemRequest> Items { get; set; } = [];
}

public class CreateReturnRequest
{
    public int OriginalBillId { get; set; }
    public string? Reason { get; set; }
    public int? SalesPersonId { get; set; }
    public decimal? Amount { get; set; }
    public IReadOnlyList<CreateReturnItemRequest> Items { get; set; } = [];
}

public class CreateReturnItemRequest
{
    public int OriginalBillItemId { get; set; }
    public decimal Quantity { get; set; }
}

public class ReturnDto
{
    public int Id { get; set; }
    public int StoreId { get; set; }
    public int OriginalBillId { get; set; }
    public string OriginalBillNumber { get; set; } = string.Empty;
    public string ReturnNumber { get; set; } = string.Empty;
    public DateTime ReturnDate { get; set; }
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerCode { get; set; }
    public string? CustomerMobile { get; set; }
    public string? StoreName { get; set; }
    public decimal ReturnAmount { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal DeductionPercent { get; set; }
    public decimal DeductionAmount { get; set; }
    public string? Reason { get; set; }
    public ReturnKind ReturnKind { get; set; }
    public int? ExchangeBillId { get; set; }
    public string? ExchangeBillNumber { get; set; }
    public int? SalesPersonId { get; set; }
    public string? SalesPersonName { get; set; }
    public int? AppliedToBillId { get; set; }
    public string? AppliedToBillNumber { get; set; }
    public IReadOnlyList<ReturnItemDto> Items { get; set; } = [];
}

public class ReturnItemDto
{
    public int OriginalBillItemId { get; set; }
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Rate { get; set; }
    public decimal Total { get; set; }
}

public class CreateExchangeRequest
{
    public int OriginalBillId { get; set; }
    public string? Reason { get; set; }
    public IReadOnlyList<CreateReturnItemRequest> ReturnItems { get; set; } = [];
    public IReadOnlyList<CreateBillItemRequest> NewItems { get; set; } = [];
    public decimal BillDiscount { get; set; }
    public decimal WalletRedeemAmount { get; set; }
    public int? SalesPersonId { get; set; }
    public decimal? Amount { get; set; }
    public IReadOnlyList<CreatePaymentRequest> Payments { get; set; } = [];
}

public class CreateBuybackRequest
{
    public int OriginalBillId { get; set; }
    public string? Reason { get; set; }
    public decimal? Amount { get; set; }
    public int? SalesPersonId { get; set; }
    public IReadOnlyList<CreateReturnItemRequest> Items { get; set; } = [];
}

public class ExchangeDto
{
    public ReturnDto Return { get; set; } = null!;
    public BillDto NewBill { get; set; } = null!;
    public decimal DifferencePayable { get; set; }
}

public class InvoiceDto
{
    public string ShopName { get; set; } = string.Empty;
    public string? LogoPath { get; set; }
    public string? BusinessAddress { get; set; }
    public string? BusinessMobile { get; set; }
    public string? BusinessEmail { get; set; }
    public string? GSTNumber { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public string? StoreAddress { get; set; }
    public string? StoreContact { get; set; }
    public string? StoreGST { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerMobile { get; set; }
    public string? CustomerAddress { get; set; }
    public string? CustomerCode { get; set; }
    public string? CustomerReferralCode { get; set; }
    public DateOnly? CustomerDateOfBirth { get; set; }
    public string? SalesPersonName { get; set; }
    public IReadOnlyList<BillItemDto> Products { get; set; } = [];
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal ItemDiscount { get; set; }
    public decimal OtherDiscount { get; set; }
    public decimal ReferralDiscount { get; set; }
    public decimal ReferralDiscountPercent { get; set; }
    public decimal BirthdayDiscount { get; set; }
    public decimal BirthdayDiscountPercent { get; set; }
    public string? BirthdayOfferName { get; set; }
    public decimal StoreDiscount { get; set; }
    public decimal StoreDiscountPercent { get; set; }
    public string? StoreDiscountName { get; set; }
    public decimal TotalDiscount { get; set; }
    public IReadOnlyList<InvoiceDiscountLineDto> DiscountLines { get; set; } = [];
    public bool HasReferral { get; set; }
    public string? ReferrerName { get; set; }
    public string? ReferrerCode { get; set; }
    public decimal ReferrerBenefitPercent { get; set; }
    public decimal ReferrerBenefitAmount { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }
    public decimal ReturnAdjustment { get; set; }
    public decimal ExchangeAdjustment { get; set; }
    public decimal BuybackAdjustment { get; set; }
    public decimal WalletRedeemed { get; set; }
    public decimal CreditGenerated { get; set; }
    public decimal PayableAmount { get; set; }
    public IReadOnlyList<PaymentDto> Payments { get; set; } = [];
    public IReadOnlyList<ReturnDto> Adjustments { get; set; } = [];
    public decimal AmountPaid { get; set; }
    public decimal AmountDue { get; set; }
    public string? Footer { get; set; }
    public string? ReturnPolicy { get; set; }
}

public class InvoiceDiscountLineDto
{
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal? Percent { get; set; }
    public decimal Amount { get; set; }
    public string? Reason { get; set; }
}

public class WhatsAppShareDto
{
    public bool Sent { get; set; }
    public bool DocumentAttached { get; set; }
    public string Delivery { get; set; } = "share";
    public string Message { get; set; } = string.Empty;
    public string ShareUrl { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Error { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
}
