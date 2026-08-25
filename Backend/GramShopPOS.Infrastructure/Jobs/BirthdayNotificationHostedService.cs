using GramShopPOS.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GramShopPOS.Infrastructure.Jobs;

public sealed class BirthdayNotificationHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<BirthdayNotificationHostedService> _logger;

    public BirthdayNotificationHostedService(IServiceScopeFactory scopes, ILogger<BirthdayNotificationHostedService> logger)
    {
        _scopes = scopes;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await SafeDelay(TimeSpan.FromSeconds(20), stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var birthdays = scope.ServiceProvider.GetRequiredService<IBirthdayService>();
                var result = await birthdays.ProcessDailyAsync(stoppingToken);
                _logger.LogInformation(
                    "Birthday WhatsApp job finished. Customers={Customers} Sent={Sent} Failed={Failed} Skipped={Skipped}",
                    result.CustomersFound,
                    result.MessagesSent,
                    result.MessagesFailed,
                    result.MessagesSkipped);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Birthday WhatsApp job failed.");
            }

            await SafeDelay(DelayUntilNextMorning(), stoppingToken);
        }
    }

    private static TimeSpan DelayUntilNextMorning()
    {
        try
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById(
                OperatingSystem.IsWindows() ? "India Standard Time" : "Asia/Kolkata");
            var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
            var next = now.Date.AddHours(6).AddMinutes(15);
            if (now >= next)
            {
                next = next.AddDays(1);
            }

            return next - now;
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeSpan.FromHours(24);
        }
    }

    private static async Task SafeDelay(TimeSpan delay, CancellationToken token)
    {
        if (delay < TimeSpan.FromSeconds(5))
        {
            delay = TimeSpan.FromSeconds(5);
        }

        try
        {
            await Task.Delay(delay, token);
        }
        catch (OperationCanceledException)
        {
            // shutting down
        }
    }
}
