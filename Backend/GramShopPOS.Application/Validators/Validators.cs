using FluentValidation;
using GramShopPOS.Application.DTOs.Auth;
using GramShopPOS.Application.DTOs.Billing;
using GramShopPOS.Application.DTOs.Catalog;
using GramShopPOS.Application.DTOs.Customers;
using GramShopPOS.Application.DTOs.Inventory;
using GramShopPOS.Application.DTOs.Stores;
using GramShopPOS.Application.DTOs.Users;
using GramShopPOS.Domain.Constants;
using Microsoft.Extensions.DependencyInjection;

namespace GramShopPOS.Application.Validators;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.UserName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Password).NotEmpty();
    }
}

public sealed class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.UserName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(PasswordRules.MinLength);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Role).Must(r => Roles.All.Contains(r));
    }
}

public sealed class CreateStoreRequestValidator : AbstractValidator<CreateStoreRequest>
{
    public CreateStoreRequestValidator()
    {
        RuleFor(x => x.StoreCode).NotEmpty().MaximumLength(20);
        RuleFor(x => x.StoreName).NotEmpty().MaximumLength(200);
    }
}

public sealed class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(x => x.ProductCode).NotEmpty().MaximumLength(50);
        RuleFor(x => x.ProductName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CategoryId).GreaterThan(0);
        RuleFor(x => x.PurchasePrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.SellingPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MRP).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TaxPercent).InclusiveBetween(0, 100);
    }
}

public sealed class UpdateProductUnitRequestValidator : AbstractValidator<UpdateProductUnitRequest>
{
    public UpdateProductUnitRequestValidator()
    {
        RuleFor(x => x.SellingPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MRP).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PurchasePrice).GreaterThanOrEqualTo(0).When(x => x.PurchasePrice.HasValue);
    }
}

public sealed class CreateBillRequestValidator : AbstractValidator<CreateBillRequest>
{
    public CreateBillRequestValidator()
    {
        RuleFor(x => x.StoreId).GreaterThan(0);
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).ChildRules(i =>
        {
            i.RuleFor(x => x.ProductId).GreaterThan(0);
            i.RuleFor(x => x.Quantity).GreaterThan(0);
            i.RuleFor(x => x.DiscountAmount).GreaterThanOrEqualTo(0);
        });
        RuleFor(x => x.BillDiscount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.WalletRedeemAmount).GreaterThanOrEqualTo(0);
        RuleForEach(x => x.Adjustments).ChildRules(a =>
        {
            a.RuleFor(x => x.OriginalBillId).GreaterThan(0);
            a.RuleFor(x => x.Items).NotEmpty();
            a.RuleForEach(x => x.Items).ChildRules(i =>
            {
                i.RuleFor(x => x.OriginalBillItemId).GreaterThan(0);
                i.RuleFor(x => x.Quantity).GreaterThan(0);
            });
        });
    }
}

public sealed class CreateCustomerRequestValidator : AbstractValidator<CreateCustomerRequest>
{
    public CreateCustomerRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.MobileNumber).NotEmpty().MaximumLength(20);
        RuleFor(x => x.StoreId).GreaterThan(0);
    }
}

public sealed class CreatePurchaseRequestValidator : AbstractValidator<CreatePurchaseRequest>
{
    public CreatePurchaseRequestValidator()
    {
        RuleFor(x => x.StoreId).GreaterThan(0);
        RuleFor(x => x.InvoiceNumber).NotEmpty();
        RuleFor(x => x.Items).NotEmpty();
        RuleFor(x => x.SupplierName).NotEmpty().When(x => !x.SupplierId.HasValue);
    }
}

public sealed class ApplicationServiceRegistration
{
    public static void AddApplication(IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();
        services.AddScoped<Interfaces.IPasswordService, Services.PasswordService>();
        services.AddScoped<Interfaces.IAuditService, Services.AuditService>();
        services.AddScoped<Interfaces.IStockEngine, Services.StockEngine>();
        services.AddScoped<Interfaces.IDocumentNumberGenerator, Services.DocumentNumberGenerator>();
        services.AddScoped<Interfaces.IAuthService, Services.AuthService>();
        services.AddScoped<Interfaces.IUserService, Services.UserService>();
        services.AddScoped<Interfaces.IStoreService, Services.StoreService>();
        services.AddScoped<Interfaces.ICategoryService, Services.CategoryService>();
        services.AddScoped<Interfaces.IProductService, Services.ProductService>();
        services.AddScoped<Interfaces.IProductUnitService, Services.ProductUnitService>();
        services.AddScoped<Interfaces.IInventoryService, Services.InventoryService>();
        services.AddScoped<Interfaces.IPurchaseService, Services.PurchaseService>();
        services.AddScoped<Interfaces.IBillingService, Services.BillingService>();
        services.AddScoped<Services.IReturnDocumentService, Services.ReturnDocumentService>();
        services.AddScoped<Interfaces.IReturnService, Services.ReturnService>();
        services.AddScoped<Interfaces.ICustomerService, Services.CustomerService>();
        services.AddScoped<Interfaces.IReferralService, Services.ReferralService>();
        services.AddScoped<Interfaces.IDiscountService, Services.DiscountService>();
        services.AddScoped<Interfaces.ISupplierService, Services.SupplierService>();
        services.AddScoped<Interfaces.IRepairService, Services.RepairService>();
        services.AddScoped<Interfaces.IDashboardService, Services.DashboardService>();
        services.AddScoped<Interfaces.IReportService, Services.ReportService>();
        services.AddScoped<Interfaces.ISettingsService, Services.SettingsService>();
        services.AddScoped<Interfaces.IBirthdayService, Services.BirthdayService>();
    }
}
