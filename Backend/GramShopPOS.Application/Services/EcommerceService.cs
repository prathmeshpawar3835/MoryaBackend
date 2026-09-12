using System.Text;
using GramShopPOS.Application.Common;
using GramShopPOS.Application.DTOs.Catalog;
using GramShopPOS.Application.DTOs.Ecommerce;
using GramShopPOS.Application.DTOs.Billing;
using GramShopPOS.Application.Exceptions;
using GramShopPOS.Application.Interfaces;
using GramShopPOS.Domain.Constants;
using GramShopPOS.Domain.Entities;
using GramShopPOS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GramShopPOS.Application.Services;

public sealed class EcommerceService : IEcommerceService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IPasswordService _passwords;
    private readonly IJwtTokenService _jwt;
    private readonly IBillingService _billing;
    private readonly IWhatsAppService _whatsApp;
    private readonly IAuditService _audit;
    private readonly ILogger<EcommerceService> _logger;

    public EcommerceService(
        IAppDbContext db,
        ICurrentUser currentUser,
        IPasswordService passwords,
        IJwtTokenService jwt,
        IBillingService billing,
        IWhatsAppService whatsApp,
        IAuditService audit,
        ILogger<EcommerceService> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _passwords = passwords;
        _jwt = jwt;
        _billing = billing;
        _whatsApp = whatsApp;
        _audit = audit;
        _logger = logger;
    }

    public async Task<ShopInfoDto> GetShopInfoAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _db.BusinessSettings.AsNoTracking().FirstAsync(cancellationToken);
        var store = await ResolveStoreAsync(cancellationToken);
        return new ShopInfoDto
        {
            ShopName = settings.ShopName,
            LogoPath = settings.LogoPath,
            Address = settings.Address ?? store.Address,
            Mobile = settings.Mobile,
            Email = settings.Email,
            StoreId = store.Id,
            StoreName = store.StoreName
        };
    }

    public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Categories.AsNoTracking()
            .Where(c => c.IsActive && !c.IsDeleted)
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                CodePrefix = c.CodePrefix,
                Description = c.Description,
                IsActive = c.IsActive,
                CreatedDate = c.CreatedDate
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResponse<ShopProductDto>> GetCatalogAsync(ShopCatalogRequest request, CancellationToken cancellationToken = default)
    {
        var store = await ResolveStoreAsync(cancellationToken);
        var query = CatalogQuery(store.Id);

        if (request.CategoryId.HasValue)
        {
            query = query.Where(x => x.CategoryId == request.CategoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim();
            query = query.Where(x => x.ProductName.Contains(s) || x.ProductCode.Contains(s) || x.CategoryName.Contains(s) || (x.Metal != null && x.Metal.Contains(s)));
        }

        if (request.MinPrice.HasValue)
        {
            query = query.Where(x => x.SellingPrice >= request.MinPrice.Value);
        }

        if (request.MaxPrice.HasValue)
        {
            query = query.Where(x => x.SellingPrice <= request.MaxPrice.Value);
        }

        if (request.InStock == true)
        {
            query = query.Where(x => x.InStock);
        }
        else if (request.InStock == false)
        {
            query = query.Where(x => !x.InStock);
        }

        query = (request.SortColumn?.Trim().ToLowerInvariant()) switch
        {
            "price" => request.SortDirection == "asc" ? query.OrderBy(x => x.SellingPrice) : query.OrderByDescending(x => x.SellingPrice),
            "name" => request.SortDirection == "asc" ? query.OrderBy(x => x.ProductName) : query.OrderByDescending(x => x.ProductName),
            "stock" => request.SortDirection == "asc" ? query.OrderBy(x => x.StockQuantity) : query.OrderByDescending(x => x.StockQuantity),
            _ => query.OrderByDescending(x => x.InStock).ThenBy(x => x.ProductName)
        };

        return await query.ToPagedAsync(request, cancellationToken);
    }

    public async Task<ShopProductDetailDto> GetProductAsync(int id, CancellationToken cancellationToken = default)
    {
        var store = await ResolveStoreAsync(cancellationToken);
        var product = await CatalogQuery(store.Id).FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundAppException("Product not found.");
        var source = await _db.Products.AsNoTracking().FirstAsync(p => p.Id == id, cancellationToken);
        var related = await CatalogQuery(store.Id)
            .Where(x => x.CategoryId == product.CategoryId && x.Id != id)
            .OrderByDescending(x => x.InStock)
            .ThenBy(x => x.ProductName)
            .Take(4)
            .ToListAsync(cancellationToken);

        return new ShopProductDetailDto
        {
            Id = product.Id,
            ProductCode = product.ProductCode,
            ProductName = product.ProductName,
            CategoryId = product.CategoryId,
            CategoryName = product.CategoryName,
            Unit = product.Unit,
            SellingPrice = product.SellingPrice,
            MRP = product.MRP,
            DiscountPercent = product.DiscountPercent,
            ImagePath = product.ImagePath,
            ImageUrl = product.ImageUrl,
            WeightGrams = product.WeightGrams,
            Metal = product.Metal,
            StockQuantity = product.StockQuantity,
            InStock = product.InStock,
            TaxPercent = source.TaxPercent,
            Description = BuildDescription(source),
            Related = related
        };
    }

    public async Task<IReadOnlyList<ShopOfferDto>> GetOffersAsync(CancellationToken cancellationToken = default)
    {
        var store = await ResolveStoreAsync(cancellationToken);
        var today = DateTime.UtcNow.Date;
        return await _db.StoreDiscounts.AsNoTracking()
            .Where(d => !d.IsDeleted
                && d.IsActive
                && d.EcommerceOnly
                && d.StoreId == store.Id
                && d.OfferCategory != OfferCategory.Birthday
                && (d.ValidFrom == null || d.ValidFrom.Value.Date <= today)
                && (d.ValidTo == null || d.ValidTo.Value.Date >= today))
            .OrderBy(d => d.ValidTo)
            .ThenBy(d => d.Name)
            .Select(d => new ShopOfferDto
            {
                Id = d.Id,
                Name = d.Name,
                Description = d.Description,
                DiscountKind = d.DiscountKind,
                Value = d.Value,
                ValidFrom = d.ValidFrom,
                ValidTo = d.ValidTo
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ShopAuthResponse> RegisterAsync(ShopRegisterRequest request, CancellationToken cancellationToken = default)
    {
        var name = (request.Name ?? string.Empty).Trim();
        var mobile = NormalizeMobile(request.Mobile);
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(mobile))
        {
            throw new ValidationAppException("Name and mobile number are required.");
        }

        _passwords.ValidateStrength(request.Password);
        var store = await ResolveStoreAsync(cancellationToken);
        var existing = await _db.Customers.FirstOrDefaultAsync(c => c.MobileNumber == mobile && !c.IsDeleted, cancellationToken);
        if (existing is not null)
        {
            if (!string.IsNullOrWhiteSpace(existing.PasswordHash))
            {
                throw new ConflictAppException("An account already exists for this mobile number. Please sign in.");
            }

            existing.Name = name;
            if (!string.IsNullOrWhiteSpace(request.Address))
            {
                existing.Address = request.Address.Trim();
            }

            existing.PasswordHash = _passwords.Hash(request.Password);
            existing.UpdatedDate = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return IssueToken(existing);
        }

        var customer = new Customer
        {
            StoreId = store.Id,
            Name = name,
            MobileNumber = mobile,
            Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim(),
            ReferralCode = await CustomerReferral.NextCodeAsync(_db, cancellationToken),
            CustomerCode = UniqueTempCode(),
            PasswordHash = _passwords.Hash(request.Password),
            CreatedDate = DateTime.UtcNow,
            IsActive = true
        };
        _db.Customers.Add(customer);
        await _db.SaveChangesAsync(cancellationToken);
        customer.CustomerCode = CustomerService.FormatCustomerCode(customer.Id);
        await _db.SaveChangesAsync(cancellationToken);
        return IssueToken(customer);
    }

    public async Task<ShopAuthResponse> LoginAsync(ShopLoginRequest request, CancellationToken cancellationToken = default)
    {
        var mobile = NormalizeMobile(request.Mobile);
        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.MobileNumber == mobile && !c.IsDeleted, cancellationToken)
            ?? throw new UnauthorizedAppException("Invalid mobile number or password.");
        if (!customer.IsActive)
        {
            throw new BusinessAppException("This account is inactive. Please contact the store.");
        }

        if (string.IsNullOrWhiteSpace(customer.PasswordHash) || !_passwords.Verify(request.Password, customer.PasswordHash))
        {
            throw new UnauthorizedAppException("Invalid mobile number or password.");
        }

        return IssueToken(customer);
    }

    public async Task<ShopCustomerDto> GetMeAsync(CancellationToken cancellationToken = default)
    {
        var customer = await RequireCustomerAsync(cancellationToken);
        return MapCustomer(customer);
    }

    public async Task<PagedResponse<ShopOrderDto>> GetOrdersAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        var customer = await RequireCustomerAsync(cancellationToken);
        var query = _db.Bills.AsNoTracking()
            .Where(b => b.CustomerId == customer.Id && !b.IsDeleted)
            .OrderByDescending(b => b.BillDate)
            .Select(b => new ShopOrderDto
            {
                Id = b.Id,
                OrderNumber = b.BillNumber,
                OrderDate = b.BillDate,
                Status = b.Status,
                Subtotal = b.Subtotal,
                Discount = b.BillDiscount + b.StoreDiscountAmount + b.ItemDiscountTotal,
                DiscountName = b.StoreDiscountName,
                TaxAmount = b.TaxAmount,
                GrandTotal = b.GrandTotal,
                PaidAmount = b.PaidAmount,
                DueAmount = b.DueAmount,
                Notes = b.Notes,
                CustomerName = b.Customer != null ? b.Customer.Name : null,
                CustomerMobile = b.Customer != null ? b.Customer.MobileNumber : null,
                CustomerAddress = b.Customer != null ? b.Customer.Address : null
            });
        return await query.ToPagedAsync(request, cancellationToken);
    }

    public async Task<ShopOrderDto> GetOrderAsync(int id, CancellationToken cancellationToken = default)
    {
        var customer = await RequireCustomerAsync(cancellationToken);
        var bill = await _db.Bills.AsNoTracking()
            .Include(b => b.Items)
            .Include(b => b.Payments)
            .Include(b => b.Customer)
            .FirstOrDefaultAsync(b => b.Id == id && b.CustomerId == customer.Id && !b.IsDeleted, cancellationToken)
            ?? throw new NotFoundAppException("Order not found.");
        return MapOrder(bill);
    }

    public async Task<ShopCheckoutResponse> CheckoutAsync(ShopCheckoutRequest request, CancellationToken cancellationToken = default)
    {
        var customer = await RequireCustomerAsync(cancellationToken);
        if (request.Items is null || request.Items.Count == 0)
        {
            throw new ValidationAppException("Your cart is empty.");
        }

        if (!string.IsNullOrWhiteSpace(request.Address))
        {
            customer.Address = request.Address.Trim();
            customer.UpdatedDate = DateTime.UtcNow;
        }

        if (string.IsNullOrWhiteSpace(customer.Address))
        {
            throw new ValidationAppException("Delivery address is required.");
        }

        var store = await ResolveStoreAsync(cancellationToken);
        if (customer.StoreId != store.Id)
        {
            throw new ValidationAppException("This customer account belongs to another store.");
        }

        var grouped = request.Items
            .GroupBy(i => i.ProductId)
            .Select(g => new { ProductId = g.Key, Quantity = g.Sum(x => x.Quantity) })
            .ToList();
        if (grouped.Any(g => g.Quantity <= 0))
        {
            throw new ValidationAppException("Item quantity must be greater than zero.");
        }

        var productIds = grouped.Select(g => g.ProductId).ToList();
        var products = await _db.Products.Where(p => productIds.Contains(p.Id) && !p.IsDeleted).ToListAsync(cancellationToken);
        if (products.Count != productIds.Count || products.Any(p => !p.IsActive))
        {
            throw new ValidationAppException("One or more products are no longer available.");
        }

        var stocks = await _db.Inventories.Where(i => i.StoreId == store.Id && productIds.Contains(i.ProductId) && !i.IsDeleted)
            .ToDictionaryAsync(i => i.ProductId, i => i.Quantity, cancellationToken);

        var billItems = new List<CreateBillItemRequest>();
        var lineInputs = new List<(decimal Qty, decimal Rate, decimal Discount, decimal Tax, Product Product)>();
        foreach (var line in grouped)
        {
            var product = products.First(p => p.Id == line.ProductId);
            stocks.TryGetValue(product.Id, out var qty);
            if (qty < line.Quantity)
            {
                throw new BusinessAppException($"{product.ProductName} is out of stock or does not have enough quantity.");
            }

            var unitIds = await PickAvailableUnitIdsAsync(store.Id, product, line.Quantity, cancellationToken);
            billItems.Add(new CreateBillItemRequest
            {
                ProductId = product.Id,
                Quantity = line.Quantity,
                DiscountAmount = 0,
                ProductUnitIds = unitIds
            });
            lineInputs.Add((line.Quantity, product.SellingPrice, 0, product.TaxPercent, product));
        }

        var eligible = Money.Round(lineInputs.Sum(l => (l.Qty * l.Rate) - l.Discount));
        var storeDiscountAmount = 0m;
        if (request.OfferId.HasValue)
        {
            var today = DateTime.UtcNow.Date;
            var offer = await _db.StoreDiscounts.AsNoTracking().FirstOrDefaultAsync(d => d.Id == request.OfferId && !d.IsDeleted, cancellationToken)
                ?? throw new NotFoundAppException("Offer not found.");
            if (!offer.EcommerceOnly || !offer.IsActive || offer.StoreId != store.Id
                || (offer.ValidFrom.HasValue && today < offer.ValidFrom.Value.Date)
                || (offer.ValidTo.HasValue && today > offer.ValidTo.Value.Date))
            {
                throw new BusinessAppException("This offer is no longer valid.");
            }

            storeDiscountAmount = offer.DiscountKind == DiscountKind.Percentage
                ? ReferralCalculator.ComputeBenefit(eligible, offer.Value, RewardType.Percentage)
                : ReferralCalculator.ComputeBenefit(eligible, offer.Value, RewardType.FixedAmount);
        }

        var totals = BillCalculator.CalculateTotals(
            lineInputs.Select(l => (l.Qty, l.Rate, l.Discount, l.Tax)).ToList(),
            storeDiscountAmount);

        var salesPersonId = await ResolveEcommerceSalesPersonIdAsync(store.Id, cancellationToken);
        var prepaid = request.PaymentMode is PaymentMode.Cash or PaymentMode.Upi or PaymentMode.Card;
        var payments = prepaid
            ? new List<CreatePaymentRequest>
            {
                new()
                {
                    PaymentMode = request.PaymentMode,
                    Amount = totals.GrandTotal,
                    ReferenceNumber = string.IsNullOrWhiteSpace(request.PaymentReference) ? null : request.PaymentReference.Trim()
                }
            }
            : [];

        var bill = await _billing.CreateEcommerceBillAsync(new CreateBillRequest
        {
            StoreId = store.Id,
            CustomerId = customer.Id,
            SalesPersonId = salesPersonId,
            StoreDiscountId = request.OfferId,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? "E-commerce order" : $"E-commerce order. {request.Notes.Trim()}",
            Items = billItems,
            Payments = payments
        }, cancellationToken);

        var order = await GetOrderAsync(bill.Id, cancellationToken);
        var notification = await NotifyOwnerAsync(order, customer, cancellationToken);
        return new ShopCheckoutResponse
        {
            Order = order,
            NotificationSent = notification.Sent,
            NotificationMessage = notification.Message
        };
    }

    private IQueryable<ShopProductDto> CatalogQuery(int storeId) =>
        from p in _db.Products.AsNoTracking()
        join c in _db.Categories.AsNoTracking() on p.CategoryId equals c.Id
        join i in _db.Inventories.AsNoTracking().Where(x => x.StoreId == storeId && !x.IsDeleted)
            on p.Id equals i.ProductId into inv
        from i in inv.DefaultIfEmpty()
        where p.IsActive && !p.IsDeleted
        select new ShopProductDto
        {
            Id = p.Id,
            ProductCode = p.ProductCode,
            ProductName = p.ProductName,
            CategoryId = p.CategoryId,
            CategoryName = c.Name,
            Unit = p.Unit,
            SellingPrice = p.SellingPrice,
            MRP = p.MRP,
            DiscountPercent = p.MRP > p.SellingPrice && p.MRP > 0
                ? (p.MRP - p.SellingPrice) * 100m / p.MRP
                : null,
            ImagePath = p.ImagePath,
            ImageUrl = p.ImagePath ?? "/images/default-jewellery.svg",
            WeightGrams = p.WeightGrams,
            Metal = p.Metal,
            StockQuantity = i != null ? i.Quantity : 0,
            InStock = i != null && i.Quantity > 0
        };

    private async Task<Store> ResolveStoreAsync(CancellationToken cancellationToken) =>
        await _db.Stores.AsNoTracking().Where(s => s.IsActive && !s.IsDeleted).OrderBy(s => s.Id).FirstOrDefaultAsync(cancellationToken)
        ?? throw new BusinessAppException("No active store is available for online orders.");

    private async Task<Customer> RequireCustomerAsync(CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !string.Equals(_currentUser.Role, Roles.Customer, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAppException("Please sign in to continue.");
        }

        return await _db.Customers.FirstOrDefaultAsync(c => c.Id == _currentUser.UserId && !c.IsDeleted, cancellationToken)
            ?? throw new UnauthorizedAppException("Please sign in to continue.");
    }

    private ShopAuthResponse IssueToken(Customer customer)
    {
        var (token, expiration, _) = _jwt.CreateToken(customer.Id, customer.MobileNumber, Roles.Customer, [customer.StoreId]);
        return new ShopAuthResponse
        {
            AccessToken = token,
            Expiration = expiration,
            Customer = MapCustomer(customer)
        };
    }

    private static ShopCustomerDto MapCustomer(Customer customer) => new()
    {
        Id = customer.Id,
        Name = customer.Name,
        Mobile = customer.MobileNumber,
        Address = customer.Address,
        CustomerCode = customer.CustomerCode
    };

    private static ShopOrderDto MapOrderExpr(Bill b) => new()
    {
        Id = b.Id,
        OrderNumber = b.BillNumber,
        OrderDate = b.BillDate,
        Status = b.Status,
        StatusName = b.Status.ToString(),
        Subtotal = b.Subtotal,
        Discount = b.BillDiscount + b.StoreDiscountAmount + b.ItemDiscountTotal,
        DiscountName = b.StoreDiscountName,
        TaxAmount = b.TaxAmount,
        GrandTotal = b.GrandTotal,
        PaidAmount = b.PaidAmount,
        DueAmount = b.DueAmount,
        Notes = b.Notes,
        CustomerName = b.Customer != null ? b.Customer.Name : null,
        CustomerMobile = b.Customer != null ? b.Customer.MobileNumber : null,
        CustomerAddress = b.Customer != null ? b.Customer.Address : null
    };

    private static ShopOrderDto MapOrder(Bill b)
    {
        var dto = MapOrderExpr(b);
        dto.PaymentMode = b.Payments.OrderByDescending(p => p.Id).Select(p => p.PaymentMode.ToString()).FirstOrDefault()
            ?? (b.DueAmount > 0 ? "Cash on delivery" : null);
        dto.Items = b.Items.Select(i => new ShopOrderItemDto
        {
            ProductId = i.ProductId,
            ProductCode = i.ProductCode,
            ProductName = i.ProductName,
            Quantity = i.Quantity,
            Rate = i.Rate,
            DiscountAmount = i.DiscountAmount,
            Total = i.Total
        }).ToList();
        return dto;
    }

    private async Task<IReadOnlyList<int>?> PickAvailableUnitIdsAsync(int storeId, Product product, decimal quantity, CancellationToken cancellationToken)
    {
        if (!PieceCount.TryGet(product.Unit, quantity, out var count) || count == 0)
        {
            return null;
        }

        var hasUnits = await _db.ProductUnits.AnyAsync(u => u.ProductId == product.Id && u.StoreId == storeId && !u.IsDeleted, cancellationToken);
        if (!hasUnits)
        {
            return null;
        }

        var ids = await _db.ProductUnits.AsNoTracking()
            .Where(u => u.ProductId == product.Id
                && u.StoreId == storeId
                && !u.IsDeleted
                && (u.Status == ProductUnitStatus.Available || u.Status == ProductUnitStatus.Returned || u.Status == ProductUnitStatus.Exchanged)
                && (u.SellingPrice ?? product.SellingPrice) == product.SellingPrice)
            .OrderBy(u => u.Id)
            .Take(count)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        if (ids.Count < count)
        {
            throw new BusinessAppException($"{product.ProductName} does not have enough tagged pieces in stock.");
        }

        return ids;
    }

    private async Task<int> ResolveEcommerceSalesPersonIdAsync(int storeId, CancellationToken cancellationToken)
    {
        var adminId = await _db.Users.AsNoTracking()
            .Where(u => !u.IsDeleted && u.IsActive && u.UserRoles.Any(r => r.Role.Name == Roles.Admin))
            .OrderBy(u => u.Id)
            .Select(u => (int?)u.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (adminId.HasValue)
        {
            return adminId.Value;
        }

        var salesId = await _db.Users.AsNoTracking()
            .Where(u => !u.IsDeleted && u.IsActive
                && u.UserRoles.Any(r => r.Role.Name == Roles.SalesPerson)
                && u.StoreUsers.Any(s => s.StoreId == storeId))
            .OrderBy(u => u.Id)
            .Select(u => (int?)u.Id)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new BusinessAppException("Online checkout is not available until a staff account is configured.");
        return salesId;
    }

    private async Task<(bool Sent, string? Message)> NotifyOwnerAsync(ShopOrderDto order, Customer customer, CancellationToken cancellationToken)
    {
        try
        {
            var settings = await _db.BusinessSettings.AsNoTracking().FirstAsync(cancellationToken);
            var mobile = string.IsNullOrWhiteSpace(settings.EcommerceNotifyMobile) ? settings.Mobile : settings.EcommerceNotifyMobile;
            if (string.IsNullOrWhiteSpace(mobile))
            {
                return (false, "Order saved. Add a shop mobile number in Business Settings to receive WhatsApp alerts.");
            }

            if (!await _whatsApp.IsConfiguredAsync(cancellationToken))
            {
                return (false, "Order saved. WhatsApp Cloud API is not configured, so the owner alert was not sent.");
            }

            var result = await _whatsApp.SendTextAsync(mobile, BuildWhatsAppMessage(order, customer), cancellationToken);
            if (result.Success)
            {
                await _audit.LogAsync(AuditActions.EcommerceOrderWhatsAppSent, nameof(Bill), order.Id.ToString(), null, new { order.OrderNumber }, null, cancellationToken);
                return (true, null);
            }

            await _audit.LogAsync(AuditActions.EcommerceOrderWhatsAppFailed, nameof(Bill), order.Id.ToString(), null, new { order.OrderNumber, result.Error }, null, cancellationToken);
            return (false, "Order placed. WhatsApp notification could not be sent.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "E-commerce WhatsApp notification failed for order {OrderId}", order.Id);
            try
            {
                await _audit.LogAsync(AuditActions.EcommerceOrderWhatsAppFailed, nameof(Bill), order.Id.ToString(), null, new { order.OrderNumber, ex.Message }, null, cancellationToken);
            }
            catch
            {
                // Notification must never fail the order.
            }

            return (false, "Order placed. WhatsApp notification could not be sent.");
        }
    }

    private static string BuildWhatsAppMessage(ShopOrderDto order, Customer customer)
    {
        var sb = new StringBuilder();
        sb.AppendLine("NEW E-COMMERCE ORDER");
        sb.AppendLine();
        sb.AppendLine($"Order ID: #{order.OrderNumber}");
        sb.AppendLine();
        sb.AppendLine("Customer:");
        sb.AppendLine($"Name: {customer.Name}");
        sb.AppendLine($"Mobile: {customer.MobileNumber}");
        sb.AppendLine($"Address: {customer.Address}");
        sb.AppendLine();
        sb.AppendLine("Products:");
        var n = 1;
        foreach (var item in order.Items)
        {
            sb.AppendLine($"{n}. {item.ProductName}");
            sb.AppendLine($"   Item ID: {item.ProductCode}");
            sb.AppendLine($"   Qty: {item.Quantity:0.##}");
            sb.AppendLine($"   Price: ₹{item.Rate:0.00}");
            if (item.DiscountAmount > 0)
            {
                sb.AppendLine($"   Discount: ₹{item.DiscountAmount:0.00}");
            }

            n++;
        }

        sb.AppendLine();
        if (order.Discount > 0)
        {
            sb.AppendLine($"Discount: ₹{order.Discount:0.00}{(string.IsNullOrWhiteSpace(order.DiscountName) ? "" : $" ({order.DiscountName})")}");
        }

        sb.AppendLine($"Total: ₹{order.GrandTotal:0.00}");
        sb.AppendLine($"Order Date: {order.OrderDate.ToLocalTime():dd/MM/yyyy HH:mm}");
        sb.AppendLine($"Payment Status: {order.StatusName}");
        if (!string.IsNullOrWhiteSpace(order.PaymentMode))
        {
            sb.AppendLine($"Payment Mode: {order.PaymentMode}");
        }

        return sb.ToString().Trim();
    }

    private static string? BuildDescription(Product product)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(product.Metal))
        {
            parts.Add($"{product.Metal} jewellery");
        }

        if (product.WeightGrams.HasValue && product.WeightGrams.Value > 0)
        {
            parts.Add($"{product.WeightGrams.Value:0.##} g");
        }

        parts.Add($"SKU {product.ProductCode}");
        return string.Join(" · ", parts);
    }

    private static string NormalizeMobile(string? mobile) => (mobile ?? string.Empty).Trim();

    private static string UniqueTempCode() => $"T{Guid.NewGuid():N}"[..20];
}
