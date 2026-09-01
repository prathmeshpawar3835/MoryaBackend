namespace GramShopPOS.Application.Interfaces;

public sealed record WhatsAppSendResult(bool Success, string? Error = null, bool Configured = true);

public interface IWhatsAppService
{
    Task<bool> IsConfiguredAsync(CancellationToken cancellationToken = default);
    Task<WhatsAppSendResult> SendTextAsync(string mobileNumber, string message, CancellationToken cancellationToken = default);
    Task<WhatsAppSendResult> SendDocumentAsync(
        string mobileNumber,
        byte[] content,
        string fileName,
        string caption,
        CancellationToken cancellationToken = default);
}
