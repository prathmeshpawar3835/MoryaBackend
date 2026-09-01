using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using GramShopPOS.Application.Common;
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

    public async Task<bool> IsConfiguredAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _db.BusinessSettings.AsNoTracking().FirstAsync(cancellationToken);
        return IsConfigured(settings.WhatsAppEnabled, settings.WhatsAppAccessToken, settings.WhatsAppPhoneNumberId);
    }

    public Task<WhatsAppSendResult> SendTextAsync(string mobileNumber, string message, CancellationToken cancellationToken = default) =>
        SendMessageAsync(mobileNumber, JsonSerializer.Serialize(new
        {
            messaging_product = "whatsapp",
            to = WhatsAppDelivery.NormalizePhone(mobileNumber) ?? mobileNumber,
            type = "text",
            text = new { body = message }
        }), cancellationToken);

    public async Task<WhatsAppSendResult> SendDocumentAsync(
        string mobileNumber,
        byte[] content,
        string fileName,
        string caption,
        CancellationToken cancellationToken = default)
    {
        var settings = await _db.BusinessSettings.AsNoTracking().FirstAsync(cancellationToken);
        if (!IsConfigured(settings.WhatsAppEnabled, settings.WhatsAppAccessToken, settings.WhatsAppPhoneNumberId))
        {
            return new WhatsAppSendResult(false, "WhatsApp provider is not configured.", false);
        }

        var digits = WhatsAppDelivery.NormalizePhone(mobileNumber);
        if (digits is null)
        {
            return new WhatsAppSendResult(false, "Customer mobile number is not valid for WhatsApp.");
        }

        if (content.Length == 0)
        {
            return new WhatsAppSendResult(false, "PDF document is empty.");
        }

        var baseUrl = string.IsNullOrWhiteSpace(settings.WhatsAppApiBaseUrl)
            ? "https://graph.facebook.com/v21.0"
            : settings.WhatsAppApiBaseUrl.TrimEnd('/');
        var phoneId = settings.WhatsAppPhoneNumberId!.Trim();
        var token = settings.WhatsAppAccessToken!.Trim();
        var safeName = string.IsNullOrWhiteSpace(fileName) ? "document.pdf" : Path.GetFileName(fileName);

        try
        {
            var client = _httpClientFactory.CreateClient(nameof(WhatsAppService));
            var mediaId = await UploadMediaAsync(client, baseUrl, phoneId, token, content, safeName, cancellationToken);
            if (string.IsNullOrWhiteSpace(mediaId))
            {
                return new WhatsAppSendResult(false, "WhatsApp media upload did not return an id.");
            }

            var payload = JsonSerializer.Serialize(new
            {
                messaging_product = "whatsapp",
                to = digits,
                type = "document",
                document = new
                {
                    id = mediaId,
                    filename = safeName,
                    caption = caption.Length <= 1024 ? caption : caption[..1024]
                }
            });
            return await PostJsonAsync(client, $"{baseUrl}/{phoneId}/messages", token, payload, cancellationToken);
        }
        catch (Exception ex)
        {
            return new WhatsAppSendResult(false, ex.Message);
        }
    }

    private async Task<WhatsAppSendResult> SendMessageAsync(string mobileNumber, string payload, CancellationToken cancellationToken)
    {
        var settings = await _db.BusinessSettings.AsNoTracking().FirstAsync(cancellationToken);
        if (!IsConfigured(settings.WhatsAppEnabled, settings.WhatsAppAccessToken, settings.WhatsAppPhoneNumberId))
        {
            return new WhatsAppSendResult(false, "WhatsApp provider is not configured.", false);
        }

        var digits = WhatsAppDelivery.NormalizePhone(mobileNumber);
        if (digits is null)
        {
            return new WhatsAppSendResult(false, "Customer mobile number is not valid for WhatsApp.");
        }

        var baseUrl = string.IsNullOrWhiteSpace(settings.WhatsAppApiBaseUrl)
            ? "https://graph.facebook.com/v21.0"
            : settings.WhatsAppApiBaseUrl.TrimEnd('/');
        var url = $"{baseUrl}/{settings.WhatsAppPhoneNumberId!.Trim()}/messages";

        try
        {
            var client = _httpClientFactory.CreateClient(nameof(WhatsAppService));
            return await PostJsonAsync(client, url, settings.WhatsAppAccessToken!.Trim(), payload, cancellationToken);
        }
        catch (Exception ex)
        {
            return new WhatsAppSendResult(false, ex.Message);
        }
    }

    private static async Task<string?> UploadMediaAsync(
        HttpClient client,
        string baseUrl,
        string phoneId,
        string token,
        byte[] content,
        string fileName,
        CancellationToken cancellationToken)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent("whatsapp"), "messaging_product");
        form.Add(new StringContent("application/pdf"), "type");
        var file = new ByteArrayContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        form.Add(file, "file", fileName);

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/{phoneId}/media");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = form;
        using var response = await client.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"WhatsApp media upload returned {(int)response.StatusCode}: {Trim(body)}");
        }

        using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);
        return doc.RootElement.TryGetProperty("id", out var id) ? id.GetString() : null;
    }

    private static async Task<WhatsAppSendResult> PostJsonAsync(
        HttpClient client,
        string url,
        string token,
        string payload,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
        using var response = await client.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return new WhatsAppSendResult(true);
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        return new WhatsAppSendResult(false, $"WhatsApp API returned {(int)response.StatusCode}: {Trim(body)}");
    }

    private static bool IsConfigured(bool enabled, string? token, string? phoneId) =>
        enabled && !string.IsNullOrWhiteSpace(token) && !string.IsNullOrWhiteSpace(phoneId);

    private static string Trim(string value) => value.Length <= 300 ? value : value[..300];
}
