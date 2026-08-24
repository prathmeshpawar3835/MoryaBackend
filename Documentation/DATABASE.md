# Database

SQL Server database: **GramShopPOS**

EF Core is the source of schema. `Database/02_Tables.sql` is the idempotent EF migration script.

## Architecture

Store-aware financial data: Inventory, Bills, Customers, Payments, Returns, Purchases, StockMovements, Referrals, Ledgers.

Master catalog (Products, Categories) is global. Stock is per store (`Inventories` unique on StoreId + ProductId).

## Important tables

- Users, Roles, UserRoles, StoreUsers
- Stores
- Categories, Products, Inventories, StockMovements, StockTransfers, Purchases
- Customers, CustomerLedgers, CustomerPayments, WalletTransactions
- Bills, BillItems, Payments, HeldBills, BillSequences
- Returns, ReturnItems, ReturnSequences
- Referrals, ReferralRewards
- BusinessSettings, TaxSettings, AuditLogs

## Relationships

```
Store 1—n StoreUser n—1 User
Store 1—n Inventory n—1 Product
Store 1—n Bill 1—n BillItem n—1 Product
Bill 1—n Payment
Customer 1—n Bill / Ledger / WalletTransaction
Bill 1—n Return (original bill is never mutated)
```

## Indexes

ProductCode, Barcode, ProductName, CategoryId, Inventory(StoreId,ProductId), StockMovement(ProductId,StoreId,CreatedDate), Customer.MobileNumber, Bill(BillNumber,StoreId,BillDate,CustomerId), CustomerLedger(CustomerId,StoreId), Referral(Referrer,Referred).

## Financial flow

Bill create (single transaction): validate store → load server prices → calculate tax/discount → validate payments → allocate bill number (`UPDATE ... OUTPUT`) → insert bill/items/payments → deduct stock (`UPDATE Quantity WHERE Quantity + delta >= 0`) → ledger → wallet/referral → audit.

Failure rolls back everything.

## Inventory flow

Purchase / stock-in / return increase quantity. Sale / transfer-out / adjustment-out decrease it. Every change writes `StockMovements`. `AllowNegativeStock` is a BusinessSettings flag (default false).

## Customer ledger / wallet

Ledger is running balance (debit sale, credit payment/return). Wallet uses atomic `UPDATE ... WHERE WalletBalance >= amount` so concurrent redemptions cannot go negative.

## Store isolation

SalesPerson JWT contains assigned store ids. Services call `EnsureStoreAccess(storeId)` and reject mismatches with 403. Entity store is always loaded from the database, never trusted from the client.
