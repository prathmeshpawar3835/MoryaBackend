using GramShopPOS.Application.DTOs.Billing;
using GramShopPOS.Application.Interfaces;

namespace GramShopPOS.Application.Common;

public static class WhatsAppDelivery
{
    public static string? NormalizePhone(string? mobile)
    {
        if (string.IsNullOrWhiteSpace(mobile))
        {
            return null;
        }

        var digits = new string(mobile.Where(char.IsDigit).ToArray());
        if (digits.Length == 10)
        {
            digits = "91" + digits;
        }

        return digits.Length >= 10 ? digits : null;
    }

    public static WhatsAppShareDto Preview(string? mobile, string message, string documentNumber)
    {
        var digits = NormalizePhone(mobile);
        if (digits is null)
        {
            return new WhatsAppShareDto
            {
                Sent = false,
                InvoiceNumber = documentNumber,
                Message = string.Empty,
                ShareUrl = string.Empty,
                Error = "Customer mobile number is not available."
            };
        }

        return new WhatsAppShareDto
        {
            Sent = false,
            Phone = digits,
            Message = message,
            ShareUrl = $"https://wa.me/{digits}?text={Uri.EscapeDataString(message)}",
            InvoiceNumber = documentNumber,
            Delivery = "share"
        };
    }

    public static async Task<WhatsAppShareDto> SendPdfAsync(
        IWhatsAppService whatsApp,
        WhatsAppShareDto share,
        byte[] content,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(share.Phone) || content.Length == 0)
        {
            return share;
        }

        var send = await whatsApp.SendDocumentAsync(share.Phone, content, fileName, share.Message, cancellationToken);
        if (send.Success)
        {
            share.Sent = true;
            share.DocumentAttached = true;
            share.Delivery = "cloud";
            share.Error = null;
            share.ShareUrl = string.Empty;
            return share;
        }

        share.Sent = false;
        share.DocumentAttached = false;
        if (send.Configured)
        {
            share.Error = send.Error ?? "WhatsApp PDF sending failed.";
        }

        return share;
    }
}
