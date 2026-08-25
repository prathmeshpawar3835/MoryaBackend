using GramShopPOS.Application.DTOs.Billing;
using GramShopPOS.Application.DTOs.Operations;
using GramShopPOS.Application.DTOs.Reports;
using GramShopPOS.Domain.Entities;

namespace GramShopPOS.Application.Interfaces;

public interface IBirthdayService
{
    Task<BirthdayEligibilityDto> GetEligibilityAsync(int customerId, int? storeId, CancellationToken cancellationToken = default);
    Task<BirthdayDiscountApplication> ResolveForSaleAsync(Customer customer, int storeId, int? birthdayOfferId, decimal eligibleAmount, CancellationToken cancellationToken = default);
    Task RecordRedemptionAsync(Customer customer, Bill bill, BirthdayDiscountApplication applied, int salesPersonId, CancellationToken cancellationToken = default);
    Task ReleaseRedemptionForCancelledBillAsync(int billId, CancellationToken cancellationToken = default);
    Task<DailyBirthdayRunResult> ProcessDailyAsync(CancellationToken cancellationToken = default);
    Task<Common.PagedResponse<BirthdayReportRowDto>> GetReportAsync(ReportRequest request, CancellationToken cancellationToken = default);
}
