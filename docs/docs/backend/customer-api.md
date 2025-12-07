# CustomerApi

The CustomerApi manages customers, customer segments, loyalty programs, campaigns, and customer communications.

## Overview

**Location:** `solution/backend/CustomerApi/`  
**Port:** 5008 (local)  
**Database Schema:** `customers`  
**Framework:** ASP.NET Core 8

## Key Features

- Customer management (CRUD)
- Customer segmentation
- Loyalty programs
- Marketing campaigns
- Customer communications
- Behavioral triggers
- Wallet management
- Membership management

## Controllers

### CustomersController
- Customer CRUD operations
- Customer search and filtering
- Customer statistics

### CustomerSegmentsController
- Segment creation and management
- Segment assignment
- Segment analytics

### CampaignsController
- Campaign management
- Campaign targeting
- Campaign analytics

### CommunicationsController
- Email/SMS communications
- Communication history
- Template management

### LoyaltyController
- Loyalty program management
- Points management
- Rewards

### MembershipController
- Membership tiers
- Membership benefits
- Membership status

### WalletController
- Wallet balance management
- Transaction history
- Wallet operations

### BehavioralTriggersController
- Trigger definitions
- Trigger execution
- Trigger analytics

## Database Schema

### customers.customers
```sql
CREATE TABLE customers.customers (
    customer_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    first_name TEXT NOT NULL,
    last_name TEXT NOT NULL,
    email TEXT,
    phone TEXT,
    address JSONB,
    date_of_birth DATE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

### customers.customer_segments
```sql
CREATE TABLE customers.customer_segments (
    segment_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name TEXT NOT NULL UNIQUE,
    criteria JSONB NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

### customers.loyalty_programs
```sql
CREATE TABLE customers.loyalty_programs (
    program_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name TEXT NOT NULL,
    points_per_dollar NUMERIC(5,2) NOT NULL DEFAULT 1.0,
    redemption_rules JSONB,
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

### customers.wallets
```sql
CREATE TABLE customers.wallets (
    wallet_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    customer_id UUID NOT NULL REFERENCES customers.customers(customer_id),
    balance NUMERIC(18,2) NOT NULL DEFAULT 0,
    currency TEXT NOT NULL DEFAULT 'USD',
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

## Endpoints

### Customers

**GET** `/api/customers`
- **Description:** List customers with pagination
- **Query Parameters:**
  - `page` (int, default: 1)
  - `pageSize` (int, default: 20)
  - `search` (string, optional)
- **Response:** `200 OK` with `PagedResult<CustomerDto>`

**GET** `/api/customers/&#123;id&#125;`
- **Description:** Get customer by ID
- **Response:** `200 OK` with `CustomerDto` | `404 Not Found`

**POST** `/api/customers`
- **Description:** Create new customer
- **Request Body:** `CreateCustomerDto`
- **Response:** `201 Created` with `CustomerDto`

**PUT** `/api/customers/&#123;id&#125;`
- **Description:** Update customer
- **Request Body:** `UpdateCustomerDto`
- **Response:** `200 OK` | `404 Not Found`

**DELETE** `/api/customers/&#123;id&#125;`
- **Description:** Delete customer (soft delete)
- **Response:** `204 No Content` | `404 Not Found`

### Segments

**GET** `/api/customers/segments`
- **Description:** List all segments
- **Response:** `200 OK` with `SegmentDto[]`

**POST** `/api/customers/segments`
- **Description:** Create segment
- **Request Body:** `CreateSegmentDto`
- **Response:** `201 Created` with `SegmentDto`

### Loyalty

**GET** `/api/customers/loyalty/programs`
- **Description:** List loyalty programs
- **Response:** `200 OK` with `LoyaltyProgramDto[]`

**GET** `/api/customers/&#123;customerId&#125;/loyalty/points`
- **Description:** Get customer loyalty points
- **Response:** `200 OK` with points balance

### Campaigns

**GET** `/api/customers/campaigns`
- **Description:** List campaigns
- **Response:** `200 OK` with `CampaignDto[]`

**POST** `/api/customers/campaigns`
- **Description:** Create campaign
- **Request Body:** `CreateCampaignDto`
- **Response:** `201 Created` with `CampaignDto`

### Wallets

**GET** `/api/customers/&#123;customerId&#125;/wallet`
- **Description:** Get customer wallet
- **Response:** `200 OK` with `WalletDto` | `404 Not Found`

**POST** `/api/customers/&#123;customerId&#125;/wallet/credit`
- **Description:** Credit wallet
- **Request Body:** `{ "amount": decimal, "reason": "string" }`
- **Response:** `200 OK` with updated wallet

**POST** `/api/customers/&#123;customerId&#125;/wallet/debit`
- **Description:** Debit wallet
- **Request Body:** `{ "amount": decimal, "reason": "string" }`
- **Response:** `200 OK` with updated wallet

## Deployment

**Cloud Run Deployment:**
```powershell
.\solution\backend\deploy-customer-cloudrun.ps1 `
    -ProjectId "bola8pos" `
    -Region "northamerica-south1" `
    -ServiceName "magidesk-customer" `
    -CloudSqlInstance "bola8pos:northamerica-south1:pos-app-1"
```

**Service URL:** `https://magidesk-customer-904541739138.northamerica-south1.run.app`

## Related Documentation

- [Customer Management](../features/customers.md)
- [Loyalty Programs](../features/loyalty.md)
