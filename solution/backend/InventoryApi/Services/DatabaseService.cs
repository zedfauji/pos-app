using Npgsql;
using MagiDesk.Shared.DTOs;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Text.Json;
using System.Globalization;
using CsvHelper;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace InventoryApi.Services;

public class DatabaseOptions
{
    public string ConnectionString { get; set; } = string.Empty;
}

public class AllVendorsDump
{
    public int VendorCount { get; set; }
    public List<VendorDump> Vendors { get; set; } = new();
}

public class VendorDump
{
    public string Id { get; set; } = string.Empty;
    public Dictionary<string, object?> Fields { get; set; } = new();
    public Dictionary<string, List<Dictionary<string, object?>>> Subcollections { get; set; } = new();
}

public class VendorDebugInfo
{
    public string VendorId { get; set; } = string.Empty;
    public bool DocumentExists { get; set; }
    public Dictionary<string, object?> Fields { get; set; } = new();
    public List<string> Subcollections { get; set; } = new();
    public Dictionary<string, List<Dictionary<string, object?>>> Samples { get; set; } = new();
}

public interface IDatabaseService
{
    // Vendors
    Task<List<VendorDto>> GetVendorsAsync(CancellationToken ct = default);
    Task<VendorDto?> GetVendorAsync(string id, CancellationToken ct = default);
    Task<VendorDto> CreateVendorAsync(VendorDto dto, CancellationToken ct = default);
    Task<bool> UpdateVendorAsync(string id, VendorDto dto, CancellationToken ct = default);
    Task<bool> DeleteVendorAsync(string id, CancellationToken ct = default);

    // Items (related to vendor)
    Task<List<ItemDto>> GetItemsAsync(string vendorId, CancellationToken ct = default);
    Task<ItemDto?> GetItemAsync(string vendorId, string id, CancellationToken ct = default);
    Task<ItemDto> CreateItemAsync(string vendorId, ItemDto dto, CancellationToken ct = default);
    Task<bool> UpdateItemAsync(string vendorId, string id, ItemDto dto, CancellationToken ct = default);
    Task<bool> DeleteItemAsync(string vendorId, string id, CancellationToken ct = default);
    Task<List<ItemDto>> GetMenuAvailableItemsAsync(CancellationToken ct = default);

    // Transactions
    Task<bool> RecordInventoryTransactionAsync(string itemId, string vendorId, int quantityChange, string transactionType, string source, string userId, CancellationToken ct = default);

    // Debug helpers
    Task<VendorDebugInfo> GetVendorDebugAsync(string vendorId, CancellationToken ct = default);
    Task<AllVendorsDump> GetAllVendorsDumpAsync(CancellationToken ct = default);
    Task<string> ExportVendorItemsAsync(string vendorId, string format, CancellationToken ct = default);
    Task ImportVendorItemsAsync(string vendorId, string format, Stream fileStream, CancellationToken ct = default);
}

public class DatabaseService : IDatabaseService
{
    private readonly string _connectionString;

    public DatabaseService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException("Database connection string is not configured");
        }
    }

    private async Task<NpgsqlConnection> GetConnectionAsync(CancellationToken ct)
    {
        var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(ct);
        return connection;
    }

    public async Task<string> ExportVendorItemsAsync(string vendorId, string format, CancellationToken ct = default)
    {
        var items = await GetItemsAsync(vendorId, ct);
        format = format.ToLowerInvariant();
        switch (format)
        {
            case "json":
                return JsonSerializer.Serialize(items, new JsonSerializerOptions { WriteIndented = true });
            case "yaml":
                var serializer = new SerializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();
                return serializer.Serialize(items);
            case "csv":
                using (var sw = new StringWriter())
                using (var csv = new CsvWriter(sw, CultureInfo.InvariantCulture))
                {
                    csv.WriteRecords(items);
                    return sw.ToString();
                }
            default:
                throw new ArgumentException("Unsupported format. Use csv, json, or yaml");
        }
    }

    public async Task ImportVendorItemsAsync(string vendorId, string format, Stream fileStream, CancellationToken ct = default)
    {
        format = format.ToLowerInvariant();
        List<ItemDto> items;
        if (format == "json")
        {
            using var sr = new StreamReader(fileStream);
            var text = await sr.ReadToEndAsync();
            items = new();
            try
            {
                var arr = JsonSerializer.Deserialize<List<ItemDto>>(text);
                if (arr is not null) items = arr;
            }
            catch
            {
                using var doc = JsonDocument.Parse(text);
                JsonElement root = doc.RootElement;
                JsonElement itemsElement;
                if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("items", out itemsElement) && itemsElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var el in itemsElement.EnumerateArray())
                    {
                        items.Add(ParseVendorItemElement(el));
                    }
                }
                else
                {
                    throw new InvalidOperationException("JSON root must be an array of items or contain 'items'.");
                }
            }
        }
        else if (format == "yaml")
        {
            using var sr = new StreamReader(fileStream);
            var deserializer = new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();
            var text = await sr.ReadToEndAsync();
            items = deserializer.Deserialize<List<ItemDto>>(text) ?? new();
        }
        else if (format == "csv")
        {
            using var sr = new StreamReader(fileStream);
            using var csv = new CsvReader(sr, CultureInfo.InvariantCulture);
            items = csv.GetRecords<ItemDto>().ToList();
        }
        else
        {
            throw new ArgumentException("Unsupported format. Use csv, json, or yaml");
        }

        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.Id)) item.Id = Guid.NewGuid().ToString();
            item.Sku = item.Id;
            var updated = await UpdateItemAsync(vendorId, item.Id, item, ct);
            if (!updated)
            {
                await CreateItemAsync(vendorId, item, ct);
            }
        }
    }

    private static ItemDto ParseVendorItemElement(JsonElement el)
    {
        string id = el.TryGetProperty("id", out var idProp) ? idProp.GetString() ?? Guid.NewGuid().ToString() : Guid.NewGuid().ToString();
        string name = el.TryGetProperty("name", out var nProp) ? nProp.GetString() ?? string.Empty : string.Empty;
        decimal price = 0m;
        if (el.TryGetProperty("price", out var pProp))
        {
            if (pProp.ValueKind == JsonValueKind.Number)
            {
                if (pProp.TryGetDecimal(out var dec)) price = dec;
                else if (pProp.TryGetDouble(out var dbl)) price = Convert.ToDecimal(dbl);
                else if (pProp.TryGetInt64(out var lng)) price = lng;
            }
        }
        int stock = 0;
        if (el.TryGetProperty("inStock", out var sProp) && sProp.ValueKind == JsonValueKind.True) stock = 1;
        return new ItemDto
        {
            Id = id,
            Sku = id,
            Name = name,
            Price = price,
            Stock = stock
        };
    }

    // Vendors
    public async Task<List<VendorDto>> GetVendorsAsync(CancellationToken ct = default)
    {
        var vendors = new List<VendorDto>();
        using var connection = await GetConnectionAsync(ct);
        using var command = new NpgsqlCommand("SELECT id, name, contact_info, status, budget, reminder, reminder_enabled FROM vendors", connection);
        using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            vendors.Add(new VendorDto
            {
                Id = reader.GetString(0),
                Name = reader.GetString(1),
                ContactInfo = reader.IsDBNull(2) ? null : reader.GetString(2),
                Status = reader.GetString(3),
                Budget = reader.GetDecimal(4),
                Reminder = reader.IsDBNull(5) ? null : reader.GetString(5),
                ReminderEnabled = reader.GetBoolean(6)
            });
        }
        return vendors;
    }

    public async Task<VendorDto?> GetVendorAsync(string id, CancellationToken ct = default)
    {
        using var connection = await GetConnectionAsync(ct);
        using var command = new NpgsqlCommand("SELECT id, name, contact_info, status, budget, reminder, reminder_enabled FROM vendors WHERE id = $1", connection);
        command.Parameters.AddWithValue(id);
        using var reader = await command.ExecuteReaderAsync(ct);
        if (await reader.ReadAsync(ct))
        {
            return new VendorDto
            {
                Id = reader.GetString(0),
                Name = reader.GetString(1),
                ContactInfo = reader.IsDBNull(2) ? null : reader.GetString(2),
                Status = reader.GetString(3),
                Budget = reader.GetDecimal(4),
                Reminder = reader.IsDBNull(5) ? null : reader.GetString(5),
                ReminderEnabled = reader.GetBoolean(6)
            };
        }
        return null;
    }

    public async Task<VendorDto> CreateVendorAsync(VendorDto dto, CancellationToken ct = default)
    {
        using var connection = await GetConnectionAsync(ct);
        using var command = new NpgsqlCommand(
            "INSERT INTO vendors (id, name, contact_info, status, budget, reminder, reminder_enabled) VALUES ($1, $2, $3, $4, $5, $6, $7) RETURNING id",
            connection);
        if (string.IsNullOrWhiteSpace(dto.Id)) dto.Id = Guid.NewGuid().ToString();
        command.Parameters.AddWithValue(dto.Id);
        command.Parameters.AddWithValue(dto.Name);
        command.Parameters.AddWithValue(dto.ContactInfo ?? (object)DBNull.Value);
        command.Parameters.AddWithValue(dto.Status);
        command.Parameters.AddWithValue(dto.Budget);
        command.Parameters.AddWithValue(dto.Reminder ?? (object)DBNull.Value);
        command.Parameters.AddWithValue(dto.ReminderEnabled);
        await command.ExecuteScalarAsync(ct);
        return dto;
    }

    public async Task<bool> UpdateVendorAsync(string id, VendorDto dto, CancellationToken ct = default)
    {
        using var connection = await GetConnectionAsync(ct);
        using var command = new NpgsqlCommand(
            "UPDATE vendors SET name = $2, contact_info = $3, status = $4, budget = $5, reminder = $6, reminder_enabled = $7, updated_at = CURRENT_TIMESTAMP WHERE id = $1",
            connection);
        command.Parameters.AddWithValue(id);
        command.Parameters.AddWithValue(dto.Name);
        command.Parameters.AddWithValue(dto.ContactInfo ?? (object)DBNull.Value);
        command.Parameters.AddWithValue(dto.Status);
        command.Parameters.AddWithValue(dto.Budget);
        command.Parameters.AddWithValue(dto.Reminder ?? (object)DBNull.Value);
        command.Parameters.AddWithValue(dto.ReminderEnabled);
        var rowsAffected = await command.ExecuteNonQueryAsync(ct);
        return rowsAffected > 0;
    }

    public async Task<bool> DeleteVendorAsync(string id, CancellationToken ct = default)
    {
        using var connection = await GetConnectionAsync(ct);
        using var command = new NpgsqlCommand("DELETE FROM vendors WHERE id = $1", connection);
        command.Parameters.AddWithValue(id);
        var rowsAffected = await command.ExecuteNonQueryAsync(ct);
        return rowsAffected > 0;
    }

    public async Task<VendorDebugInfo> GetVendorDebugAsync(string vendorId, CancellationToken ct = default)
    {
        var result = new VendorDebugInfo { VendorId = vendorId };
        using var connection = await GetConnectionAsync(ct);
        using var command = new NpgsqlCommand("SELECT id, name, contact_info, status, budget, reminder, reminder_enabled FROM vendors WHERE id = $1", connection);
        command.Parameters.AddWithValue(vendorId);
        using var reader = await command.ExecuteReaderAsync(ct);
        if (await reader.ReadAsync(ct))
        {
            result.DocumentExists = true;
            result.Fields = new Dictionary<string, object?>
            {
                {"id", reader.GetString(0)},
                {"name", reader.GetString(1)},
                {"contact_info", reader.IsDBNull(2) ? null : reader.GetString(2)},
                {"status", reader.GetString(3)},
                {"budget", reader.GetDecimal(4)},
                {"reminder", reader.IsDBNull(5) ? null : reader.GetString(5)},
                {"reminder_enabled", reader.GetBoolean(6)}
            };
        }
        else
        {
            result.DocumentExists = false;
        }
        reader.Close();

        // Fetch related data (e.g., items)
        result.Subcollections.Add("items");
        using var itemsCommand = new NpgsqlCommand("SELECT id, sku, name, price, stock, buying_price, category, description, reorder_threshold, batch_number, lot_number, is_menu_available FROM inventory_items WHERE vendor_id = $1 LIMIT 3", connection);
        itemsCommand.Parameters.AddWithValue(vendorId);
        using var itemsReader = await itemsCommand.ExecuteReaderAsync(ct);
        var itemsList = new List<Dictionary<string, object?>>();
        while (await itemsReader.ReadAsync(ct))
        {
            itemsList.Add(new Dictionary<string, object?>
            {
                {"id", itemsReader.GetString(0)},
                {"sku", itemsReader.GetString(1)},
                {"name", itemsReader.GetString(2)},
                {"price", itemsReader.GetDecimal(3)},
                {"stock", itemsReader.GetInt32(4)},
                {"buying_price", itemsReader.GetDecimal(5)},
                {"category", itemsReader.IsDBNull(6) ? null : itemsReader.GetString(6)},
                {"description", itemsReader.IsDBNull(7) ? null : itemsReader.GetString(7)},
                {"reorder_threshold", itemsReader.GetInt32(8)},
                {"batch_number", itemsReader.IsDBNull(9) ? null : itemsReader.GetString(9)},
                {"lot_number", itemsReader.IsDBNull(10) ? null : itemsReader.GetString(10)},
                {"is_menu_available", itemsReader.GetBoolean(11)}
            });
        }
        result.Samples["items"] = itemsList;

        return result;
    }

    public async Task<AllVendorsDump> GetAllVendorsDumpAsync(CancellationToken ct = default)
    {
        var dump = new AllVendorsDump();
        using var connection = await GetConnectionAsync(ct);
        using var command = new NpgsqlCommand("SELECT id, name, contact_info, status, budget, reminder, reminder_enabled FROM vendors", connection);
        using var reader = await command.ExecuteReaderAsync(ct);
        var vendors = new List<VendorDump>();
        while (await reader.ReadAsync(ct))
        {
            var vendorId = reader.GetString(0);
            var fields = new Dictionary<string, object?>
            {
                {"id", vendorId},
                {"name", reader.GetString(1)},
                {"contact_info", reader.IsDBNull(2) ? null : reader.GetString(2)},
                {"status", reader.GetString(3)},
                {"budget", reader.GetDecimal(4)},
                {"reminder", reader.IsDBNull(5) ? null : reader.GetString(5)},
                {"reminder_enabled", reader.GetBoolean(6)}
            };
            vendors.Add(new VendorDump { Id = vendorId, Fields = fields, Subcollections = new Dictionary<string, List<Dictionary<string, object?>>>() });
        }
        reader.Close();

        dump.VendorCount = vendors.Count;
        dump.Vendors = vendors;

        foreach (var vendor in vendors)
        {
            using var itemsCommand = new NpgsqlCommand("SELECT id, sku, name, price, stock, buying_price, category, description, reorder_threshold, batch_number, lot_number, is_menu_available FROM inventory_items WHERE vendor_id = $1", connection);
            itemsCommand.Parameters.AddWithValue(vendor.Id);
            using var itemsReader = await itemsCommand.ExecuteReaderAsync(ct);
            var itemsList = new List<Dictionary<string, object?>>();
            while (await itemsReader.ReadAsync(ct))
            {
                itemsList.Add(new Dictionary<string, object?>
                {
                    {"id", itemsReader.GetString(0)},
                    {"sku", itemsReader.GetString(1)},
                    {"name", itemsReader.GetString(2)},
                    {"price", itemsReader.GetDecimal(3)},
                    {"stock", itemsReader.GetInt32(4)},
                    {"buying_price", itemsReader.GetDecimal(5)},
                    {"category", itemsReader.IsDBNull(6) ? null : itemsReader.GetString(6)},
                    {"description", itemsReader.IsDBNull(7) ? null : itemsReader.GetString(7)},
                    {"reorder_threshold", itemsReader.GetInt32(8)},
                    {"batch_number", itemsReader.IsDBNull(9) ? null : itemsReader.GetString(9)},
                    {"lot_number", itemsReader.IsDBNull(10) ? null : itemsReader.GetString(10)},
                    {"is_menu_available", itemsReader.GetBoolean(11)}
                });
            }
            vendor.Subcollections["items"] = itemsList;
            itemsReader.Close();
        }

        return dump;
    }

    // Items
    public async Task<List<ItemDto>> GetItemsAsync(string vendorId, CancellationToken ct = default)
    {
        var items = new List<ItemDto>();
        using var connection = await GetConnectionAsync(ct);
        using var command = new NpgsqlCommand("SELECT id, sku, name, price, stock, buying_price, category, description, reorder_threshold, batch_number, lot_number, is_menu_available FROM inventory_items WHERE vendor_id = $1", connection);
        command.Parameters.AddWithValue(vendorId);
        using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            items.Add(new ItemDto
            {
                Id = reader.GetString(0),
                Sku = reader.GetString(1),
                Name = reader.GetString(2),
                Price = reader.GetDecimal(3),
                Stock = reader.GetInt32(4),
                BuyingPrice = reader.GetDecimal(5),
                Category = reader.IsDBNull(6) ? null : reader.GetString(6),
                Description = reader.IsDBNull(7) ? null : reader.GetString(7),
                ReorderThreshold = reader.GetInt32(8),
                BatchNumber = reader.IsDBNull(9) ? null : reader.GetString(9),
                LotNumber = reader.IsDBNull(10) ? null : reader.GetString(10),
                IsMenuAvailable = reader.GetBoolean(11)
            });
        }
        return items;
    }

    public async Task<List<ItemDto>> GetMenuAvailableItemsAsync(CancellationToken ct = default)
    {
        var items = new List<ItemDto>();
        using var connection = await GetConnectionAsync(ct);
        using var command = new NpgsqlCommand("SELECT id, sku, name, price, stock, buying_price, category, description, reorder_threshold, batch_number, lot_number, is_menu_available FROM inventory_items WHERE is_menu_available = true", connection);
        using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            items.Add(new ItemDto
            {
                Id = reader.GetString(0),
                Sku = reader.GetString(1),
                Name = reader.GetString(2),
                Price = reader.GetDecimal(3),
                Stock = reader.GetInt32(4),
                BuyingPrice = reader.GetDecimal(5),
                Category = reader.IsDBNull(6) ? null : reader.GetString(6),
                Description = reader.IsDBNull(7) ? null : reader.GetString(7),
                ReorderThreshold = reader.GetInt32(8),
                BatchNumber = reader.IsDBNull(9) ? null : reader.GetString(9),
                LotNumber = reader.IsDBNull(10) ? null : reader.GetString(10),
                IsMenuAvailable = reader.GetBoolean(11)
            });
        }
        return items;
    }

    public async Task<ItemDto?> GetItemAsync(string vendorId, string id, CancellationToken ct = default)
    {
        using var connection = await GetConnectionAsync(ct);
        using var command = new NpgsqlCommand("SELECT id, sku, name, price, stock, buying_price, category, description, reorder_threshold, batch_number, lot_number, is_menu_available FROM inventory_items WHERE vendor_id = $1 AND id = $2", connection);
        command.Parameters.AddWithValue(vendorId);
        command.Parameters.AddWithValue(id);
        using var reader = await command.ExecuteReaderAsync(ct);
        if (await reader.ReadAsync(ct))
        {
            return new ItemDto
            {
                Id = reader.GetString(0),
                Sku = reader.GetString(1),
                Name = reader.GetString(2),
                Price = reader.GetDecimal(3),
                Stock = reader.GetInt32(4),
                BuyingPrice = reader.GetDecimal(5),
                Category = reader.IsDBNull(6) ? null : reader.GetString(6),
                Description = reader.IsDBNull(7) ? null : reader.GetString(7),
                ReorderThreshold = reader.GetInt32(8),
                BatchNumber = reader.IsDBNull(9) ? null : reader.GetString(9),
                LotNumber = reader.IsDBNull(10) ? null : reader.GetString(10),
                IsMenuAvailable = reader.GetBoolean(11)
            };
        }
        return null;
    }

    public async Task<ItemDto> CreateItemAsync(string vendorId, ItemDto dto, CancellationToken ct = default)
    {
        using var connection = await GetConnectionAsync(ct);
        using var command = new NpgsqlCommand(
            "INSERT INTO inventory_items (id, vendor_id, sku, name, price, stock, buying_price, category, description, reorder_threshold, batch_number, lot_number, is_menu_available) VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10, $11, $12, $13) RETURNING id",
            connection);
        if (string.IsNullOrWhiteSpace(dto.Id)) dto.Id = Guid.NewGuid().ToString();
        command.Parameters.AddWithValue(dto.Id);
        command.Parameters.AddWithValue(vendorId);
        command.Parameters.AddWithValue(dto.Sku);
        command.Parameters.AddWithValue(dto.Name);
        command.Parameters.AddWithValue(dto.Price);
        command.Parameters.AddWithValue(dto.Stock);
        command.Parameters.AddWithValue(dto.BuyingPrice);
        command.Parameters.AddWithValue(dto.Category ?? (object)DBNull.Value);
        command.Parameters.AddWithValue(dto.Description ?? (object)DBNull.Value);
        command.Parameters.AddWithValue(dto.ReorderThreshold);
        command.Parameters.AddWithValue(dto.BatchNumber ?? (object)DBNull.Value);
        command.Parameters.AddWithValue(dto.LotNumber ?? (object)DBNull.Value);
        command.Parameters.AddWithValue(dto.IsMenuAvailable);
        await command.ExecuteScalarAsync(ct);
        return dto;
    }

    public async Task<bool> UpdateItemAsync(string vendorId, string id, ItemDto dto, CancellationToken ct = default)
    {
        using var connection = await GetConnectionAsync(ct);
        using var command = new NpgsqlCommand(
            "UPDATE inventory_items SET sku = $3, name = $4, price = $5, stock = $6, buying_price = $7, category = $8, description = $9, reorder_threshold = $10, batch_number = $11, lot_number = $12, is_menu_available = $13, updated_at = CURRENT_TIMESTAMP WHERE vendor_id = $1 AND id = $2",
            connection);
        command.Parameters.AddWithValue(vendorId);
        command.Parameters.AddWithValue(id);
        command.Parameters.AddWithValue(dto.Sku);
        command.Parameters.AddWithValue(dto.Name);
        command.Parameters.AddWithValue(dto.Price);
        command.Parameters.AddWithValue(dto.Stock);
        command.Parameters.AddWithValue(dto.BuyingPrice);
        command.Parameters.AddWithValue(dto.Category ?? (object)DBNull.Value);
        command.Parameters.AddWithValue(dto.Description ?? (object)DBNull.Value);
        command.Parameters.AddWithValue(dto.ReorderThreshold);
        command.Parameters.AddWithValue(dto.BatchNumber ?? (object)DBNull.Value);
        command.Parameters.AddWithValue(dto.LotNumber ?? (object)DBNull.Value);
        command.Parameters.AddWithValue(dto.IsMenuAvailable);
        var rowsAffected = await command.ExecuteNonQueryAsync(ct);
        return rowsAffected > 0;
    }

    public async Task<bool> DeleteItemAsync(string vendorId, string id, CancellationToken ct = default)
    {
        using var connection = await GetConnectionAsync(ct);
        using var command = new NpgsqlCommand("DELETE FROM inventory_items WHERE vendor_id = $1 AND id = $2", connection);
        command.Parameters.AddWithValue(vendorId);
        command.Parameters.AddWithValue(id);
        var rowsAffected = await command.ExecuteNonQueryAsync(ct);
        return rowsAffected > 0;
    }

    public async Task<bool> RecordInventoryTransactionAsync(string itemId, string vendorId, int quantityChange, string transactionType, string source, string userId, CancellationToken ct = default)
    {
        using var connection = await GetConnectionAsync(ct);
        using var command = new NpgsqlCommand(
            "INSERT INTO inventory_transactions (id, item_id, vendor_id, quantity_change, transaction_type, source, user_id) VALUES ($1, $2, $3, $4, $5, $6, $7)",
            connection);
        command.Parameters.AddWithValue(Guid.NewGuid().ToString());
        command.Parameters.AddWithValue(itemId);
        command.Parameters.AddWithValue(vendorId);
        command.Parameters.AddWithValue(quantityChange);
        command.Parameters.AddWithValue(transactionType);
        command.Parameters.AddWithValue(source);
        command.Parameters.AddWithValue(userId ?? (object)DBNull.Value);
        var rowsAffected = await command.ExecuteNonQueryAsync(ct);
        return rowsAffected > 0;
    }
}
