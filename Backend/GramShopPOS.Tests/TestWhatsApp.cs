using GramShopPOS.Application.Interfaces;

namespace GramShopPOS.Tests;

public sealed class DisabledWhatsAppService : IWhatsAppService
{
    public Task<bool> IsConfiguredAsync(CancellationToken cancellationToken = default) => Task.FromResult(false);

    public Task<WhatsAppSendResult> SendTextAsync(string mobileNumber, string message, CancellationToken cancellationToken = default) =>
        Task.FromResult(new WhatsAppSendResult(false, "WhatsApp provider is not configured.", false));

    public Task<WhatsAppSendResult> SendDocumentAsync(
        string mobileNumber,
        byte[] content,
        string fileName,
        string caption,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new WhatsAppSendResult(false, "WhatsApp provider is not configured.", false));
}

public sealed class RecordingWhatsAppService : IWhatsAppService
{
    public List<(string Mobile, string Message)> Sent { get; } = [];
    public List<(string Mobile, string FileName, string Caption, byte[] Content)> Documents { get; } = [];
    public bool Succeed { get; set; } = true;
    public bool Configured { get; set; } = true;
    public string? Error { get; set; } = "simulated failure";

    public Task<bool> IsConfiguredAsync(CancellationToken cancellationToken = default) => Task.FromResult(Configured);

    public Task<WhatsAppSendResult> SendTextAsync(string mobileNumber, string message, CancellationToken cancellationToken = default)
    {
        Sent.Add((mobileNumber, message));
        return Task.FromResult(Succeed
            ? new WhatsAppSendResult(true)
            : new WhatsAppSendResult(false, Error, Configured));
    }

    public Task<WhatsAppSendResult> SendDocumentAsync(
        string mobileNumber,
        byte[] content,
        string fileName,
        string caption,
        CancellationToken cancellationToken = default)
    {
        Documents.Add((mobileNumber, fileName, caption, content));
        return Task.FromResult(Succeed
            ? new WhatsAppSendResult(true)
            : new WhatsAppSendResult(false, Error, Configured));
    }
}
