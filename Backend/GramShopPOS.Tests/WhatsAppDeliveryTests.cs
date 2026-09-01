using FluentAssertions;
using GramShopPOS.Application.Common;
using GramShopPOS.Application.DTOs.Billing;

namespace GramShopPOS.Tests;

public class WhatsAppDeliveryTests
{
    [Fact]
    public void Preview_prefixes_indian_ten_digit_mobile()
    {
        var share = WhatsAppDelivery.Preview("9876543210", "Invoice PDF attached", "INV-1");
        share.Phone.Should().Be("919876543210");
        share.ShareUrl.Should().StartWith("https://wa.me/919876543210?text=");
        share.InvoiceNumber.Should().Be("INV-1");
        share.Sent.Should().BeFalse();
        share.DocumentAttached.Should().BeFalse();
    }

    [Fact]
    public void Preview_without_mobile_returns_error()
    {
        var share = WhatsAppDelivery.Preview(" ", "hi", "INV-1");
        share.Error.Should().Be("Customer mobile number is not available.");
        share.ShareUrl.Should().BeEmpty();
    }

    [Fact]
    public async Task SendPdf_uses_cloud_api_when_configured()
    {
        var wa = new RecordingWhatsAppService();
        var share = WhatsAppDelivery.Preview("9876543210", "Please find your return PDF", "CN-1");
        var pdf = new byte[] { 1, 2, 3, 4 };

        var result = await WhatsAppDelivery.SendPdfAsync(wa, share, pdf, "return-CN-1.pdf");

        result.Sent.Should().BeTrue();
        result.DocumentAttached.Should().BeTrue();
        result.Delivery.Should().Be("cloud");
        result.ShareUrl.Should().BeEmpty();
        wa.Documents.Should().ContainSingle();
        wa.Documents[0].FileName.Should().Be("return-CN-1.pdf");
        wa.Documents[0].Caption.Should().Contain("return PDF");
        wa.Documents[0].Content.Should().Equal(pdf);
    }

    [Fact]
    public async Task SendPdf_falls_back_to_share_link_when_not_configured()
    {
        var wa = new DisabledWhatsAppService();
        var share = WhatsAppDelivery.Preview("9876543210", "Invoice PDF attached", "INV-9");

        var result = await WhatsAppDelivery.SendPdfAsync(wa, share, [9, 8, 7], "invoice-INV-9.pdf");

        result.Sent.Should().BeFalse();
        result.DocumentAttached.Should().BeFalse();
        result.ShareUrl.Should().Contain("wa.me/919876543210");
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task SendPdf_keeps_share_link_when_configured_api_fails()
    {
        var wa = new RecordingWhatsAppService { Succeed = false, Error = "API down" };
        var share = WhatsAppDelivery.Preview("9876543210", "Buyback PDF attached", "BB-2");

        var result = await WhatsAppDelivery.SendPdfAsync(wa, share, [1], "buyback-BB-2.pdf");

        result.Sent.Should().BeFalse();
        result.DocumentAttached.Should().BeFalse();
        result.Error.Should().Be("API down");
        result.ShareUrl.Should().Contain("wa.me/");
    }
}
