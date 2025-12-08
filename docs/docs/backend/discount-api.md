# DiscountApi

The DiscountApi manages discounts, vouchers, campaigns, and combo offers.

## Overview

**Location:** `solution/backend/DiscountApi/`  
**Port:** 5009 (local)  
**Database Schema:** `discounts`  
**Framework:** ASP.NET Core 8

## Key Features

- Discount management
- Voucher system
- Campaign discounts
- Combo offers
- Discount validation
- Discount history

## Database Schema

### discounts.campaigns
```sql
CREATE TABLE discounts.campaigns (
    campaign_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name TEXT NOT NULL,
    description TEXT,
    discount_type TEXT NOT NULL CHECK (discount_type IN ('percentage', 'fixed', 'buy_x_get_y')),
    discount_value NUMERIC(18,2) NOT NULL,
    start_date DATE NOT NULL,
    end_date DATE,
    is_active BOOLEAN NOT NULL DEFAULT true,
    conditions JSONB,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

### discounts.vouchers
```sql
CREATE TABLE discounts.vouchers (
    voucher_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    code TEXT NOT NULL UNIQUE,
    discount_type TEXT NOT NULL,
    discount_value NUMERIC(18,2) NOT NULL,
    min_purchase_amount NUMERIC(18,2),
    max_discount_amount NUMERIC(18,2),
    usage_limit INTEGER,
    used_count INTEGER NOT NULL DEFAULT 0,
    valid_from DATE NOT NULL,
    valid_until DATE,
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

### discounts.combo_offers
```sql
CREATE TABLE discounts.combo_offers (
    combo_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name TEXT NOT NULL,
    items JSONB NOT NULL,
    discount_amount NUMERIC(18,2) NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

## Endpoints

### Discounts

**GET** `/api/discounts`
- **Description:** List all active discounts
- **Response:** `200 OK` with `DiscountDto[]`

**GET** `/api/discounts/&#123;id&#125;`
- **Description:** Get discount by ID
- **Response:** `200 OK` with `DiscountDto` | `404 Not Found`

**POST** `/api/discounts`
- **Description:** Create new discount
- **Request Body:** `CreateDiscountDto`
- **Response:** `201 Created` with `DiscountDto`

**PUT** `/api/discounts/&#123;id&#125;`
- **Description:** Update discount
- **Request Body:** `UpdateDiscountDto`
- **Response:** `200 OK` | `404 Not Found`

**DELETE** `/api/discounts/&#123;id&#125;`
- **Description:** Delete discount
- **Response:** `204 No Content` | `404 Not Found`

### Vouchers

**GET** `/api/discounts/vouchers`
- **Description:** List vouchers
- **Response:** `200 OK` with `VoucherDto[]`

**POST** `/api/discounts/vouchers/validate`
- **Description:** Validate voucher code
- **Request Body:** `{ "code": "string", "orderTotal": decimal }`
- **Response:** `200 OK` with validation result

### Campaigns

**GET** `/api/discounts/campaigns`
- **Description:** List active campaigns
- **Response:** `200 OK` with `CampaignDto[]`

**POST** `/api/discounts/campaigns`
- **Description:** Create campaign
- **Request Body:** `CreateCampaignDto`
- **Response:** `201 Created` with `CampaignDto`

### Health

**GET** `/health`
- **Description:** Health check
- **Response:** `200 OK`

## Deployment

**Cloud Run Deployment:**
```powershell
.\solution\backend\DiscountApi\deploy.ps1 `
    -ProjectId "bola8pos" `
    -Region "northamerica-south1" `
    -ServiceName "magidesk-discount" `
    -CloudSqlInstance "bola8pos:northamerica-south1:pos-app-1"
```

**Service URL:** `https://magidesk-discount-904541739138.northamerica-south1.run.app`

## Related Documentation

- [Discount Management](../features/discounts.md)
- [Payment Processing](../backend/payment-api.md)
