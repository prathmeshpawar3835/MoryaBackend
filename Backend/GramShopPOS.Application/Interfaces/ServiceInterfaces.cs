using GramShopPOS.Application.Common;
using GramShopPOS.Application.DTOs.Auth;
using GramShopPOS.Application.DTOs.Billing;
using GramShopPOS.Application.DTOs.Catalog;
using GramShopPOS.Application.DTOs.Customers;
using GramShopPOS.Application.DTOs.Inventory;
using GramShopPOS.Application.DTOs.Reports;
using GramShopPOS.Application.DTOs.Settings;
using GramShopPOS.Application.DTOs.Stores;
using GramShopPOS.Application.DTOs.Users;
using GramShopPOS.Domain.Enums;

namespace GramShopPOS.Application.Interfaces;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task LogoutAsync(CancellationToken cancellationToken = default);
    Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default);
    Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);
    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
    Task<CurrentUserDto> GetMeAsync(CancellationToken cancellationToken = default);
}

public interface IUserService
{
    Task<PagedResponse<UserDto>> GetAsync(PagedRequest request, CancellationToken cancellationToken = default);
    Task<UserDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
    Task<UserDto> UpdateAsync(int id, UpdateUserRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DTOs.Operations.SalesPersonOptionDto>> GetSalesPersonsAsync(int storeId, CancellationToken cancellationToken = default);
}

public interface IStoreService
{
    Task<IReadOnlyList<StoreDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<StoreDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<StoreDto> CreateAsync(CreateStoreRequest request, CancellationToken cancellationToken = default);
    Task<StoreDto> UpdateAsync(int id, UpdateStoreRequest request, CancellationToken cancellationToken = default);
}

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<CategoryDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<CategoryDto> CreateAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default);
    Task<CategoryDto> UpdateAsync(int id, UpdateCategoryRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public interface IProductService
{
    Task<PagedResponse<ProductDto>> GetAsync(ProductListRequest request, CancellationToken cancellationToken = default);
    Task<ProductDto> GetByIdAsync(int id, int? storeId, CancellationToken cancellationToken = default);
    Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default);
    Task<ProductDto> UpdateAsync(int id, UpdateProductRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductDto>> SearchAsync(string query, int? storeId, CancellationToken cancellationToken = default);
    Task<ProductDto> GetByBarcodeAsync(string barcode, int? storeId, CancellationToken cancellationToken = default);
    Task<ImportPreviewResponse> PreviewImportAsync(Stream file, string fileName, CancellationToken cancellationToken = default);
    Task<ImportConfirmResponse> ConfirmImportAsync(Guid batchId, CancellationToken cancellationToken = default);
    Task<FileDownload> GetImportTemplateAsync(CancellationToken cancellationToken = default);
}

public interface IInventoryService
{
    Task<PagedResponse<InventoryDto>> GetAsync(InventoryListRequest request, CancellationToken cancellationToken = default);
    Task<InventoryDto> GetByProductAsync(int productId, int storeId, CancellationToken cancellationToken = default);
    Task<PagedResponse<StockMovementDto>> GetLedgerAsync(InventoryListRequest request, int? productId, CancellationToken cancellationToken = default);
    Task StockInAsync(StockInRequest request, CancellationToken cancellationToken = default);
    Task AdjustAsync(StockAdjustRequest request, CancellationToken cancellationToken = default);
    Task TransferAsync(StockTransferRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InventoryDto>> GetLowStockAsync(int? storeId, CancellationToken cancellationToken = default);
}

public interface IPurchaseService
{
    Task<PagedResponse<PurchaseDto>> GetAsync(PagedRequest request, CancellationToken cancellationToken = default);
    Task<PurchaseDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<PurchaseDto> CreateAsync(CreatePurchaseRequest request, CancellationToken cancellationToken = default);
}

public interface IBillingService
{
    Task<BillDto> CreateBillAsync(CreateBillRequest request, CancellationToken cancellationToken = default);
    Task<PagedResponse<BillDto>> GetBillsAsync(BillListRequest request, CancellationToken cancellationToken = default);
    Task<BillDto> GetBillAsync(int id, CancellationToken cancellationToken = default);
    Task<PagedResponse<BillDto>> SearchBillsAsync(BillListRequest request, CancellationToken cancellationToken = default);
    Task CancelBillAsync(int id, string? reason, CancellationToken cancellationToken = default);
    Task<InvoiceDto> GetInvoiceAsync(int id, CancellationToken cancellationToken = default);
    Task<HeldBillDto> HoldBillAsync(HeldBillRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HeldBillDto>> GetHeldBillsAsync(int? storeId, CancellationToken cancellationToken = default);
    Task<HeldBillDto> GetHeldBillAsync(int id, CancellationToken cancellationToken = default);
    Task<HeldBillDto> ResumeHeldBillAsync(int id, CancellationToken cancellationToken = default);
    Task DeleteHeldBillAsync(int id, CancellationToken cancellationToken = default);
}

public interface IReturnService
{
    Task<ReturnDto> CreateReturnAsync(CreateReturnRequest request, CancellationToken cancellationToken = default);
    Task<PagedResponse<ReturnDto>> GetAsync(PagedRequest request, CancellationToken cancellationToken = default);
    Task<ReturnDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ExchangeDto> CreateExchangeAsync(CreateExchangeRequest request, CancellationToken cancellationToken = default);
}

public interface ICustomerService
{
    Task<PagedResponse<CustomerDto>> GetAsync(PagedRequest request, CancellationToken cancellationToken = default);
    Task<CustomerDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<CustomerDto?> GetByMobileAsync(string mobile, int? storeId, CancellationToken cancellationToken = default);
    Task<CustomerDto> CreateAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default);
    Task<CustomerDto> UpdateAsync(int id, UpdateCustomerRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomerDto>> SearchAsync(string query, int? storeId, CancellationToken cancellationToken = default);
    Task<CustomerHistoryDto> GetHistoryAsync(int id, CancellationToken cancellationToken = default);
    Task<PagedResponse<LedgerEntryDto>> GetLedgerAsync(int id, PagedRequest request, CancellationToken cancellationToken = default);
    Task<PaymentDto> ReceivePaymentAsync(int customerId, CustomerPaymentRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PaymentDto>> GetPaymentsAsync(int customerId, CancellationToken cancellationToken = default);
    Task<WalletDto> GetWalletAsync(int customerId, CancellationToken cancellationToken = default);
    Task RedeemWalletAsync(int customerId, WalletRedeemRequest request, CancellationToken cancellationToken = default);
}

public interface IReferralService
{
    Task<PagedResponse<ReferralDto>> GetAsync(PagedRequest request, CancellationToken cancellationToken = default);
    Task<DTOs.Operations.ReferralValidationDto> ValidateCodeAsync(string code, int? excludeCustomerId, int? storeId, CancellationToken cancellationToken = default);
    Task<DTOs.Operations.ReferralPreviewDto> PreviewAsync(Domain.Entities.Customer? customer, string? referralCode, string? referringMobile, decimal eligibleAmount, int storeId, CancellationToken cancellationToken = default);
    Task ProcessSaleAsync(Domain.Entities.Customer customer, Domain.Entities.Bill bill, CreateBillRequest request, decimal eligibleAmount, decimal referralDiscount, CancellationToken cancellationToken = default);
    Task AdjustForReturnAsync(Domain.Entities.Bill originalBill, Domain.Entities.ProductReturn ret, CancellationToken cancellationToken = default);
}

public interface IDiscountService
{
    Task<IReadOnlyList<DTOs.Operations.StoreDiscountDto>> GetAsync(int? storeId, bool activeOnly, CancellationToken cancellationToken = default);
    Task<DTOs.Operations.StoreDiscountDto> CreateAsync(DTOs.Operations.StoreDiscountRequest request, CancellationToken cancellationToken = default);
    Task<DTOs.Operations.StoreDiscountDto> UpdateAsync(int id, DTOs.Operations.StoreDiscountRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public interface ISupplierService
{
    Task<PagedResponse<DTOs.Operations.SupplierDto>> GetAsync(PagedRequest request, CancellationToken cancellationToken = default);
    Task<DTOs.Operations.SupplierDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<DTOs.Operations.SupplierDto> CreateAsync(DTOs.Operations.SupplierRequest request, CancellationToken cancellationToken = default);
    Task<DTOs.Operations.SupplierDto> UpdateAsync(int id, DTOs.Operations.SupplierRequest request, CancellationToken cancellationToken = default);
}

public interface IRepairService
{
    Task<PagedResponse<DTOs.Operations.RepairJobDto>> GetAsync(PagedRequest request, CancellationToken cancellationToken = default);
    Task<DTOs.Operations.RepairJobDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<DTOs.Operations.RepairJobDto> CreateAsync(DTOs.Operations.CreateRepairJobRequest request, CancellationToken cancellationToken = default);
    Task<DTOs.Operations.RepairJobDto> UpdateAsync(int id, DTOs.Operations.UpdateRepairJobRequest request, CancellationToken cancellationToken = default);
}

public interface IDashboardService
{
    Task<DashboardDto> GetAsync(int? storeId, CancellationToken cancellationToken = default);
}

public interface IReportService
{
    Task<SalesReportDto> GetSalesAsync(ReportRequest request, CancellationToken cancellationToken = default);
    Task<PagedResponse<ProductSalesRowDto>> GetProductSalesAsync(ReportRequest request, bool slowMoving, CancellationToken cancellationToken = default);
    Task<PagedResponse<InventoryReportRowDto>> GetInventoryAsync(ReportRequest request, CancellationToken cancellationToken = default);
    Task<PagedResponse<PurchaseDto>> GetPurchasesAsync(ReportRequest request, CancellationToken cancellationToken = default);
    Task<PagedResponse<ReturnDto>> GetReturnsAsync(ReportRequest request, CancellationToken cancellationToken = default);
    Task<PagedResponse<CustomerDueRowDto>> GetCustomerDuesAsync(ReportRequest request, CancellationToken cancellationToken = default);
    Task<PagedResponse<ReferralReportRowDto>> GetReferralsAsync(ReportRequest request, CancellationToken cancellationToken = default);
    Task<PagedResponse<ProfitReportRowDto>> GetProfitAsync(ReportRequest request, CancellationToken cancellationToken = default);
    Task<FileDownload> ExportSalesExcelAsync(ReportRequest request, CancellationToken cancellationToken = default);
    Task<FileDownload> ExportInventoryExcelAsync(ReportRequest request, CancellationToken cancellationToken = default);
    Task<FileDownload> ExportCustomersExcelAsync(ReportRequest request, CancellationToken cancellationToken = default);
    Task<FileDownload> ExportProductSalesExcelAsync(ReportRequest request, CancellationToken cancellationToken = default);
    Task<FileDownload> ExportSalesPdfAsync(ReportRequest request, CancellationToken cancellationToken = default);
    Task<FileDownload> ExportInventoryPdfAsync(ReportRequest request, CancellationToken cancellationToken = default);
}

public interface IPdfService
{
    Task<FileDownload> InvoicePdfAsync(int billId, CancellationToken cancellationToken = default);
    Task<FileDownload> LedgerPdfAsync(int customerId, CancellationToken cancellationToken = default);
    Task<FileDownload> ReturnNotePdfAsync(int returnId, CancellationToken cancellationToken = default);
    FileDownload SalesReportPdf(SalesReportDto report);
    FileDownload InventoryReportPdf(IReadOnlyList<InventoryReportRowDto> rows);
}

public interface ISettingsService
{
    Task<SettingsDto> GetAsync(CancellationToken cancellationToken = default);
    Task<SettingsDto> UpdateAsync(UpdateSettingsRequest request, CancellationToken cancellationToken = default);
}

public interface IAuditService
{
    Task LogAsync(string action, string entityName, string? entityId, object? oldValue, object? newValue, int? storeId, CancellationToken cancellationToken = default);
    Task<PagedResponse<AuditLogDto>> GetAsync(PagedRequest request, CancellationToken cancellationToken = default);
}

public interface IJwtTokenService
{
    (string Token, DateTime Expiration, string Jti) CreateToken(int userId, string userName, string role, IReadOnlyList<int> storeIds);
}

public interface IPasswordService
{
    string Hash(string password);
    bool Verify(string password, string hash);
    void ValidateStrength(string password);
}

public interface IStockEngine
{
    Task<(decimal Previous, decimal New)> ChangeAsync(
        int storeId,
        int productId,
        decimal delta,
        StockMovementType type,
        int? referenceId,
        string? referenceNumber,
        string? reason,
        bool allowNegative,
        int userId,
        CancellationToken cancellationToken = default);
}

public interface IDocumentNumberGenerator
{
    Task<string> NextBillNumberAsync(int storeId, string prefix, int financialYearStartMonth, CancellationToken cancellationToken = default);
    Task<string> NextReturnNumberAsync(int storeId, string prefix, int financialYearStartMonth, CancellationToken cancellationToken = default);
}

public interface IExcelWorkbookService
{
    FileDownload CreateProductImportTemplate();
    IReadOnlyList<Dictionary<string, string>> ReadTable(Stream stream, string fileName);
    FileDownload CreateWorkbook(string sheetName, string fileName, IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<object?>> rows);
}
