namespace GramShopPOS.Domain.Enums;

public enum LedgerTransactionType
{
    Sale = 1,
    Return = 2,
    Credit = 3,
    PaymentReceived = 4,
    WalletCredit = 5,
    WalletRedeem = 6,
    ReferralCredit = 7,
    ReferralReversal = 8,
    ExchangeAdjustment = 9,
    Buyback = 10
}
