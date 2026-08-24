using FluentAssertions;
using GramShopPOS.Application.DTOs.Billing;
using GramShopPOS.Application.DTOs.Customers;
using GramShopPOS.Application.Exceptions;
using GramShopPOS.Application.Services;
using GramShopPOS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GramShopPOS.Tests;

public class ConcurrencyTests
{
    [Fact]
    public async Task Concurrent_bills_receive_distinct_numbers()
    {
        await using var fx = new SqliteFixture();
        var productId = fx.Db.Products.First().Id;
        var customerId = fx.Db.Customers.First().Id;

        async Task<string> CreateBillAsync()
        {
            await using var db = fx.CreateContext();
            var user = new TestCurrentUser { UserId = fx.User.UserId, Role = fx.User.Role, AssignedStoreIds = [1] };
            var billing = new BillingService(db, user, new StockEngine(db), new DocumentNumberGenerator(db), new AuditService(db, user));
            var bill = await billing.CreateBillAsync(new CreateBillRequest
            {
                StoreId = 1,
                CustomerId = customerId,
                Items = [new CreateBillItemRequest { ProductId = productId, Quantity = 1 }],
                Payments = [new CreatePaymentRequest { PaymentMode = PaymentMode.Cash, Amount = 5150 }]
            });
            return bill.BillNumber;
        }

        var results = await Task.WhenAll(CreateBillAsync(), CreateBillAsync());
        results.Should().OnlyHaveUniqueItems();
        results.Should().AllSatisfy(n => n.Should().StartWith("STORE01-FY"));
    }

    [Fact]
    public async Task Concurrent_sales_cannot_oversell_last_unit()
    {
        await using var fx = new SqliteFixture();
        var inventory = fx.Db.Inventories.First();
        inventory.Quantity = 1;
        await fx.Db.SaveChangesAsync();
        var productId = inventory.ProductId;
        var customerId = fx.Db.Customers.First().Id;

        async Task<bool> TrySaleAsync()
        {
            try
            {
                await using var db = fx.CreateContext();
                var user = new TestCurrentUser { UserId = fx.User.UserId, Role = fx.User.Role, AssignedStoreIds = [1] };
                var billing = new BillingService(db, user, new StockEngine(db), new DocumentNumberGenerator(db), new AuditService(db, user));
                await billing.CreateBillAsync(new CreateBillRequest
                {
                    StoreId = 1,
                    CustomerId = customerId,
                    Items = [new CreateBillItemRequest { ProductId = productId, Quantity = 1 }],
                    Payments = [new CreatePaymentRequest { PaymentMode = PaymentMode.Cash, Amount = 5150 }]
                });
                return true;
            }
            catch (InsufficientStockException)
            {
                return false;
            }
            catch (DbUpdateException)
            {
                return false;
            }
        }

        var outcomes = await Task.WhenAll(TrySaleAsync(), TrySaleAsync());
        outcomes.Count(x => x).Should().Be(1);
        (await fx.CreateContext().Inventories.FirstAsync(i => i.ProductId == productId)).Quantity.Should().Be(0);
    }

    [Fact]
    public async Task Concurrent_wallet_redeem_cannot_go_negative()
    {
        await using var fx = new SqliteFixture();
        var customerId = fx.Db.Customers.First().Id;

        async Task<bool> TryRedeemAsync()
        {
            try
            {
                await using var db = fx.CreateContext();
                var user = new TestCurrentUser { UserId = fx.User.UserId, Role = fx.User.Role, AssignedStoreIds = [1] };
                var customers = new CustomerService(db, user, new AuditService(db, user));
                await customers.RedeemWalletAsync(customerId, new WalletRedeemRequest { StoreId = 1, Amount = 400 });
                return true;
            }
            catch (BusinessAppException)
            {
                return false;
            }
        }

        var outcomes = await Task.WhenAll(TryRedeemAsync(), TryRedeemAsync());
        outcomes.Count(x => x).Should().Be(1);
        (await fx.CreateContext().Customers.FirstAsync(c => c.Id == customerId)).WalletBalance.Should().Be(100);
    }
}
