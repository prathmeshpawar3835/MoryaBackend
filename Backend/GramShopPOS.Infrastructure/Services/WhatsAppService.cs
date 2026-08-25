using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using GramShopPOS.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GramShopPOS.Infrastructure.Services;

public sealed class WhatsAppService : IWhatsAppService
{
    private readonly IAppDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;

    public WhatsAppService(IAppDbContext db, IHttpClientFactory httpClientFactory)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<WhatsAppSendResult> SendTextAsync(string mobileNumber, string message, CancellationToken cancellationToken = default)
    {
        var settings = await _db.BusinessSettings.AsNoTracking().FirstAsync(cancellationToken);
        if (!settings.WhatsAppEnabled
            || string.IsNullOrWhiteSpace(settings.WhatsAppAccessToken)
            || string.IsNullOrWhiteSpace(settings.WhatsAppPhoneNumberId))
        {
            return new WhatsAppSendResult(false, "WhatsApp provider is not configured.");
        }

        var digits = new string(mobileNumber.Where(char.IsDigit).ToArray());
        if (digits.Length == 10)
        {
            digits = "91" + digits;
        }

        if (digits.Length < 10)
        {
            return new WhatsAppSendResult(false, "Customer mobile number is not valid for WhatsApp.");
        }

        var baseUrl = string.IsNullOrWhiteSpace(settings.WhatsAppApiBaseUrl)
            ? "https://graph.facebook.com/v21.0"
            : settings.WhatsAppApiBaseUrl.TrimEnd('/');
        var url = $"{baseUrl}/{settings.WhatsAppPhoneNumberId.Trim()}/messages";
        var payload = JsonSerializer.Serialize(new
        {
            messaging_product = "whatsapp",
            to = digits,
            type = "text",
            text = new { body = message }
        });

        try
        {
            var client = _httpClientFactory.CreateClient(nameof(WhatsAppService));
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.WhatsAppAccessToken.Trim());
            request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
            using var response = await client.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return new WhatsAppSendResult(true);
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            return new WhatsAppSendResult(false, $"WhatsApp API returned {(int)response.StatusCode}: {Trim(body)}");
        }
        catch (Exception ex)
        {
            return new WhatsAppSendResult(false, ex.Message);
        }
    }

    private static string Trim(string value) => value.Length <= 300 ? value : value[..300];
}
