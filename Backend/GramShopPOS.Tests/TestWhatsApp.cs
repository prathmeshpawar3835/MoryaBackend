using GramShopPOS.Application.Interfaces;

namespace GramShopPOS.Tests;

public sealed class DisabledWhatsAppService : IWhatsAppService
{
    public Task<WhatsAppSendResult> SendTextAsync(string mobileNumber, string message, CancellationToken cancellationToken = default) =>
        Task.FromResult(new WhatsAppSendResult(false, "WhatsApp provider is not configured."));
}

public sealed class RecordingWhatsAppService : IWhatsAppService
{
    public List<(string Mobile, string Message)> Sent { get; } = [];
    public bool Succeed { get; set; } = true;
    public string? Error { get; set; } = "simulated failure";

    public Task<WhatsAppSendResult> SendTextAsync(string mobileNumber, string message, CancellationToken cancellationToken = default)
    {
        Sent.Add((mobileNumber, message));
        return Task.FromResult(Succeed
            ? new WhatsAppSendResult(true)
            : new WhatsAppSendResult(false, Error));
    }
}
