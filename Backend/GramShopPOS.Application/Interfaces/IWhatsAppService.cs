namespace GramShopPOS.Application.Interfaces;

public sealed record WhatsAppSendResult(bool Success, string? Error = null);

public interface IWhatsAppService
{
    Task<WhatsAppSendResult> SendTextAsync(string mobileNumber, string message, CancellationToken cancellationToken = default);
}
