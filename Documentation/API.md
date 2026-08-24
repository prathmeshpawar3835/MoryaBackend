# Gram Shop POS API

Base URL: `/api`

All JSON responses:

```json
{ "success": true, "message": "Created successfully.", "data": {}, "errors": [] }
```

Authenticate with `Authorization: Bearer {accessToken}`.

Roles: `Admin`, `SalesPerson`. Store isolation is always enforced server-side.

## Authentication

| Method | URL | Auth | Notes |
|---|---|---|---|
| POST | `/api/auth/login` | Anonymous | Returns accessToken, expiration, userId, userName, role, assignedStores |
| POST | `/api/auth/logout` | JWT | Revokes current jti |
| POST | `/api/auth/change-password` | JWT | Current + new password |
| POST | `/api/auth/forgot-password` | Anonymous | Dev may return DevelopmentResetToken |
| POST | `/api/auth/reset-password` | Anonymous | userName, token, newPassword |
| GET | `/api/auth/me` | JWT | Current profile |

Login body: `{ "userName": "admin", "password": "ChangeMe@123" }`

Errors: 400 validation, 401 invalid credentials, 403 locked / no store.

## Users / roles / stores

| Method | URL | Auth |
|---|---|---|
| GET/POST/PUT | `/api/users`, `/api/users/{id}` | Admin |
| GET | `/api/stores` | JWT (salesperson sees assigned only) |
| POST/PUT | `/api/stores`, `/api/stores/{id}` | Admin |
| GET/POST/PUT/DELETE | `/api/categories` | GET any JWT; mutations Admin |

## Products / Excel import

| Method | URL | Auth |
|---|---|---|
| GET | `/api/products` | JWT, paging/search/store/category |
| GET | `/api/products/{id}` | JWT |
| POST/PUT/DELETE | `/api/products` | Admin |
| GET | `/api/products/search?query=` | JWT |
| GET | `/api/products/barcode/{barcode}` | JWT |
| POST | `/api/products/import/preview` | Admin, multipart file |
| POST | `/api/products/import/confirm?batchId=` | Admin |
| GET | `/api/products/import/template` | Admin, xlsx |

## Inventory / purchases

| Method | URL | Auth |
|---|---|---|
| GET | `/api/inventory` | JWT |
| GET | `/api/inventory/{productId}?storeId=` | JWT |
| GET | `/api/inventory/ledger` | JWT |
| POST | `/api/inventory/stock-in` | JWT |
| POST | `/api/inventory/adjust` | Admin |
| POST | `/api/inventory/transfer` | Admin |
| GET | `/api/inventory/low-stock` | JWT |
| GET/POST | `/api/purchases` | JWT |
| GET | `/api/purchases/{id}` | JWT |

## POS / bills / held bills

| Method | URL | Auth |
|---|---|---|
| POST | `/api/pos/bills` | JWT |
| GET | `/api/bills`, `/api/bills/search`, `/api/bills/{id}` | JWT |
| GET | `/api/bills/{id}/invoice` | JWT |
| GET | `/api/bills/{id}/invoice/pdf` | JWT |
| POST | `/api/bills/{id}/cancel` | JWT |
| POST/GET/DELETE | `/api/pos/held-bills` | JWT |
| POST | `/api/pos/held-bills/{id}/resume` | JWT |

Create bill body (prices/tax/stock are **not** trusted from the client):

```json
{
  "storeId": 1,
  "customerId": 1,
  "billDiscount": 0,
  "walletRedeemAmount": 0,
  "items": [{ "productId": 1, "quantity": 1, "discountAmount": 0 }],
  "payments": [{ "paymentMode": 1, "amount": 5150 }]
}
```

PaymentMode: 1 Cash, 2 UPI, 3 Card, 4 Credit, 5 Wallet.

Split payments must equal grand total minus credit/wallet.

## Returns / exchange

| Method | URL | Auth |
|---|---|---|
| POST/GET | `/api/returns` | JWT |
| GET | `/api/returns/{id}`, `/api/returns/{id}/pdf` | JWT |
| POST | `/api/exchanges` | JWT |

Original bills are never deleted.

## Customers / ledger / wallet / referrals

| Method | URL | Auth |
|---|---|---|
| GET/POST/PUT | `/api/customers` | JWT |
| GET | `/api/customers/search` | JWT |
| GET | `/api/customers/{id}/history` | JWT |
| GET | `/api/customers/{id}/ledger` | JWT |
| GET | `/api/customers/{id}/ledger/pdf` | JWT |
| POST/GET | `/api/customers/{id}/payments` | JWT |
| GET | `/api/customers/{id}/wallet` | JWT |
| POST | `/api/customers/{id}/wallet/redeem` | JWT |
| GET | `/api/referrals` | JWT |

## Dashboard / reports / settings / audit

| Method | URL | Auth |
|---|---|---|
| GET | `/api/dashboard?storeId=` | JWT |
| GET | `/api/reports/sales` | JWT, period=daily\|weekly\|monthly\|custom |
| GET | `/api/reports/product-sales` | JWT |
| GET | `/api/reports/inventory` | JWT |
| GET | `/api/reports/purchases` | JWT |
| GET | `/api/reports/returns` | JWT |
| GET | `/api/reports/customer-dues` | JWT |
| GET | `/api/reports/referrals` | JWT |
| GET | `/api/reports/profit` | **Admin only** |
| GET | `/api/reports/*/export/excel` | JWT |
| GET | `/api/reports/sales/export/pdf` | JWT |
| GET | `/api/reports/inventory/export/pdf` | JWT |
| GET/PUT | `/api/settings` | Admin |
| GET | `/api/audit-logs` | Admin |

Common query: `pageNumber`, `pageSize`, `search`, `sortColumn`, `sortDirection`, `fromDate`, `toDate`, `storeId`.

## Errors

| Code | Meaning |
|---|---|
| 400 | Validation |
| 401 | Missing/invalid JWT |
| 403 | Role or store isolation |
| 404 | Not found |
| 409 | Conflict (unique code, duplicate bill) |
| 422 | Business rule (stock, wallet) |
| 429 | Rate limited |
| 500 | Unexpected (no stack traces in Production) |
