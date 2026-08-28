using GramShopPOS.Application.Interfaces;
using GramShopPOS.Infrastructure.Data;
using GramShopPOS.Infrastructure.Identity;
using GramShopPOS.Infrastructure.Seed;
using GramShopPOS.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GramShopPOS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString, sql => sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IExcelWorkbookService, ExcelWorkbookService>();
        services.AddScoped<IPdfService, PdfService>();
        services.AddScoped<ILabelDocumentService, LabelDocumentService>();
        services.AddHttpClient(nameof(WhatsAppService));
        services.AddScoped<IWhatsAppService, WhatsAppService>();
        services.AddHostedService<Jobs.BirthdayNotificationHostedService>();
        services.AddScoped<DatabaseSeeder>();
        return services;
    }
}
