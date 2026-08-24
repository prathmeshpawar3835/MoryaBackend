using GramShopPOS.Application.Common;
using GramShopPOS.Application.DTOs.Customers;
using GramShopPOS.Application.DTOs.Reports;
using GramShopPOS.Application.DTOs.Settings;
using GramShopPOS.Application.Interfaces;
using GramShopPOS.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GramShopPOS.API.Controllers;

[ApiController]
[Authorize]
[Route("api/customers")]
public sealed class CustomersController : ControllerBase
{
    private readonly ICustomerService _customers;
    private readonly IPdfService _pdf;
    public CustomersController(ICustomerService customers, IPdfService pdf)
    {
        _customers = customers;
        _pdf = pdf;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] PagedRequest request, CancellationToken cancellationToken) =>
        Ok(await _customers.GetAsync(request, cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await _customers.GetByIdAsync(id, cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCustomerRequest request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await _customers.CreateAsync(request, cancellationToken));

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCustomerRequest request, CancellationToken cancellationToken) =>
        Ok(await _customers.UpdateAsync(id, request, cancellationToken));

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string query, [FromQuery] int? storeId, CancellationToken cancellationToken) =>
        Ok(await _customers.SearchAsync(query ?? string.Empty, storeId, cancellationToken));

    [HttpGet("{id:int}/history")]
    public async Task<IActionResult> History(int id, CancellationToken cancellationToken) =>
        Ok(await _customers.GetHistoryAsync(id, cancellationToken));

    [HttpGet("{id:int}/ledger")]
    public async Task<IActionResult> Ledger(int id, [FromQuery] PagedRequest request, CancellationToken cancellationToken) =>
        Ok(await _customers.GetLedgerAsync(id, request, cancellationToken));

    [HttpGet("{id:int}/ledger/pdf")]
    public async Task<IActionResult> LedgerPdf(int id, CancellationToken cancellationToken) =>
        Ok(await _pdf.LedgerPdfAsync(id, cancellationToken));

    [HttpPost("{id:int}/payments")]
    public async Task<IActionResult> Pay(int id, [FromBody] CustomerPaymentRequest request, CancellationToken cancellationToken) =>
        Ok(await _customers.ReceivePaymentAsync(id, request, cancellationToken));

    [HttpGet("{id:int}/payments")]
    public async Task<IActionResult> Payments(int id, CancellationToken cancellationToken) =>
        Ok(await _customers.GetPaymentsAsync(id, cancellationToken));

    [HttpGet("{id:int}/wallet")]
    public async Task<IActionResult> Wallet(int id, CancellationToken cancellationToken) =>
        Ok(await _customers.GetWalletAsync(id, cancellationToken));

    [HttpPost("{id:int}/wallet/redeem")]
    public async Task<IActionResult> Redeem(int id, [FromBody] WalletRedeemRequest request, CancellationToken cancellationToken)
    {
        await _customers.RedeemWalletAsync(id, request, cancellationToken);
        return Ok();
    }
}

[ApiController]
[Authorize]
[Route("api/referrals")]
public sealed class ReferralsController : ControllerBase
{
    private readonly IReferralService _referrals;
    public ReferralsController(IReferralService referrals) => _referrals = referrals;

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] PagedRequest request, CancellationToken cancellationToken) =>
        Ok(await _referrals.GetAsync(request, cancellationToken));
}

[ApiController]
[Authorize]
[Route("api/dashboard")]
public sealed class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboard;
    public DashboardController(IDashboardService dashboard) => _dashboard = dashboard;

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int? storeId, CancellationToken cancellationToken) =>
        Ok(await _dashboard.GetAsync(storeId, cancellationToken));
}

[ApiController]
[Authorize]
[Route("api/reports")]
public sealed class ReportsController : ControllerBase
{
    private readonly IReportService _reports;
    public ReportsController(IReportService reports) => _reports = reports;

    [HttpGet("sales")]
    public async Task<IActionResult> Sales([FromQuery] ReportRequest request, CancellationToken cancellationToken) =>
        Ok(await _reports.GetSalesAsync(request, cancellationToken));

    [HttpGet("product-sales")]
    public async Task<IActionResult> ProductSales([FromQuery] ReportRequest request, [FromQuery] bool slowMoving, CancellationToken cancellationToken) =>
        Ok(await _reports.GetProductSalesAsync(request, slowMoving, cancellationToken));

    [HttpGet("inventory")]
    public async Task<IActionResult> Inventory([FromQuery] ReportRequest request, CancellationToken cancellationToken) =>
        Ok(await _reports.GetInventoryAsync(request, cancellationToken));

    [HttpGet("purchases")]
    public async Task<IActionResult> Purchases([FromQuery] ReportRequest request, CancellationToken cancellationToken) =>
        Ok(await _reports.GetPurchasesAsync(request, cancellationToken));

    [HttpGet("returns")]
    public async Task<IActionResult> Returns([FromQuery] ReportRequest request, CancellationToken cancellationToken) =>
        Ok(await _reports.GetReturnsAsync(request, cancellationToken));

    [HttpGet("customer-dues")]
    public async Task<IActionResult> Dues([FromQuery] ReportRequest request, CancellationToken cancellationToken) =>
        Ok(await _reports.GetCustomerDuesAsync(request, cancellationToken));

    [HttpGet("referrals")]
    public async Task<IActionResult> Referrals([FromQuery] ReportRequest request, CancellationToken cancellationToken) =>
        Ok(await _reports.GetReferralsAsync(request, cancellationToken));

    [Authorize(Roles = Roles.Admin)]
    [HttpGet("profit")]
    public async Task<IActionResult> Profit([FromQuery] ReportRequest request, CancellationToken cancellationToken) =>
        Ok(await _reports.GetProfitAsync(request, cancellationToken));

    [HttpGet("sales/export/excel")]
    public async Task<IActionResult> SalesExcel([FromQuery] ReportRequest request, CancellationToken cancellationToken) =>
        Ok(await _reports.ExportSalesExcelAsync(request, cancellationToken));

    [HttpGet("inventory/export/excel")]
    public async Task<IActionResult> InventoryExcel([FromQuery] ReportRequest request, CancellationToken cancellationToken) =>
        Ok(await _reports.ExportInventoryExcelAsync(request, cancellationToken));

    [HttpGet("customers/export/excel")]
    public async Task<IActionResult> CustomersExcel([FromQuery] ReportRequest request, CancellationToken cancellationToken) =>
        Ok(await _reports.ExportCustomersExcelAsync(request, cancellationToken));

    [HttpGet("product-sales/export/excel")]
    public async Task<IActionResult> ProductSalesExcel([FromQuery] ReportRequest request, CancellationToken cancellationToken) =>
        Ok(await _reports.ExportProductSalesExcelAsync(request, cancellationToken));

    [HttpGet("sales/export/pdf")]
    public async Task<IActionResult> SalesPdf([FromQuery] ReportRequest request, CancellationToken cancellationToken) =>
        Ok(await _reports.ExportSalesPdfAsync(request, cancellationToken));

    [HttpGet("inventory/export/pdf")]
    public async Task<IActionResult> InventoryPdf([FromQuery] ReportRequest request, CancellationToken cancellationToken) =>
        Ok(await _reports.ExportInventoryPdfAsync(request, cancellationToken));
}

[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("api/settings")]
public sealed class SettingsController : ControllerBase
{
    private readonly ISettingsService _settings;
    public SettingsController(ISettingsService settings) => _settings = settings;

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken) =>
        Ok(await _settings.GetAsync(cancellationToken));

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateSettingsRequest request, CancellationToken cancellationToken) =>
        Ok(await _settings.UpdateAsync(request, cancellationToken));
}

[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("api/audit-logs")]
public sealed class AuditLogsController : ControllerBase
{
    private readonly IAuditService _audit;
    public AuditLogsController(IAuditService audit) => _audit = audit;

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] PagedRequest request, CancellationToken cancellationToken) =>
        Ok(await _audit.GetAsync(request, cancellationToken));
}
