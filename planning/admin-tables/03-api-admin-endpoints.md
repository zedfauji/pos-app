# API Contracts: Admin Table Management

## 1. Table Management (`/tables`)

### Create Table
**POST** `/tables`
**Request**: `CreateTableRequest`
```json
{
  "name": "Patio 1",
  "typeId": "uuid",
  "capacity": 4
}
```
**Response**: `200 OK` (with `TableStatusDto`) or `400 Bad Request`

### Update Table
**PUT** `/tables/{id}`
**Request**: `UpdateTableRequest`
```json
{
  "name": "Patio 1 Updated",
  "typeId": "uuid",
  "capacity": 6,
  "isActive": true
}
```
**Response**: `200 OK` (with updated DTO)

### Delete Table (Soft)
**DELETE** `/tables/{id}`
**Response**: `204 No Content`
**Rules**: Returns `409 Conflict` if table has `CurrentSessionId != null`.

## 2. Table Type Management (`/tables/types`)

### Update Type Config
**PUT** `/tables/types/{id}`
**Request**: `UpdateTableTypeRequest`
```json
{
  "name": "Billiard V2",
  "hourlyRate": 15.00,
  "hasTimer": true,
  "requiresServer": true,
  "allowOrders": true
}
```
**Response**: `200 OK` (with updated `TableTypeDto`)
**Warning**: Client must display warning about active sessions before calling this.
