# ID TYPE POLICY

**Scope**: All Entities

## 1. Canonical ID Types
| Entity Type | ID Type (DB) | ID Type (Code) | Note |
| :--- | :--- | :--- | :--- |
| Order | `uuid` | `Guid` | |
| Bill | `uuid` | `Guid` | |
| Payment | `uuid` | `Guid` | |
| Session | `uuid` | `Guid` | |
| MenuItem | `uuid` | `Guid` | |
| Modifier | `bigint` | `long` | Legacy exception. |
| Combo | `bigint` | `long` | Legacy exception. |
| User | `varchar` | `string` | Legacy exception. Care required. |
| Setting | `bigint` | `long` | |
| InventoryItem | `uuid` | `Guid` | |

## 2. Rules
- **Mixes Forbidden**: Do not join UUID on BigInt.
- **New Entities**: Must use UUID.
