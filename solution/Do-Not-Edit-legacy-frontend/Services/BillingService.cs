using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using MagiDesk.Shared.DTOs.Billing;
using MagiDesk.Shared.DTOs.Tables;
using MagiDesk.Frontend.Models;

namespace MagiDesk.Frontend.Services;

/// <summary>
/// Service for managing billing operations with support for session movement and order aggregation
/// </summary>
public class BillingService
{
    private readonly HttpClient _http;
    private readonly IConfiguration? _configuration;
    private readonly ILogger<BillingService>? _logger;
    private readonly MenuApiService _menuApiService;
    private bool _useDatabase;
    private string _connString = string.Empty;
    private string? _apiBaseUrl;

    public BillingService(ILogger<BillingService>? logger = null)
    {
        _logger = logger;
        
        // Read configuration
        var userCfgPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MagiDesk", "appsettings.user.json");
        _configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile(userCfgPath, optional: true)
            .Build();

        // Initialize HTTP client
        var inner = new HttpClientHandler();
        inner.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        var logging = new HttpLoggingHandler(inner);
        _http = new HttpClient(logging);
        
        // Initialize MenuApiService
        var menuApiUrl = _configuration["MenuApi:BaseUrl"];
        var menuHttpClient = new HttpClient(inner);
        if (!string.IsNullOrEmpty(menuApiUrl))
        {
            menuHttpClient.BaseAddress = new Uri(menuApiUrl);
        }
        _menuApiService = new MenuApiService(menuHttpClient);
        
        // Get the database preference
        _useDatabase = bool.Parse(_configuration["UseDatabase"] ?? "false");
        _connString = _configuration["Db:Postgres:ConnectionString"] ?? string.Empty;
        _apiBaseUrl = _configuration["TablesApi:BaseUrl"];
        _useDatabase = !string.IsNullOrWhiteSpace(_connString);
    }

    /// <summary>
    /// Creates a new billing entity with an initial session
    /// </summary>
    public async Task<CreateBillingResponse> CreateBillingAsync(CreateBillingRequest request, CancellationToken ct = default)
    {
        try
        {
            _logger?.LogInformation("Creating new billing for table {TableId} with server {ServerName}", request.TableId, request.ServerName);

            if (!_useDatabase)
            {
                return new CreateBillingResponse
                {
                    Success = false,
                    Message = "Database not available",
                    ErrorCode = "DB_UNAVAILABLE"
                };
            }

            var billingId = Guid.NewGuid();
            var sessionId = Guid.NewGuid();

            using var conn = new NpgsqlConnection(_connString);
            await conn.OpenAsync(ct);

            using var transaction = await conn.BeginTransactionAsync(ct);
            try
            {
                // Create billing record
                var billingQuery = @"
                    INSERT INTO public.billings (billing_id, customer_name, customer_contact, status, created_at, updated_at)
                    VALUES (@billingId, @customerName, @customerContact, 'open', NOW(), NOW())";

                using var billingCmd = new NpgsqlCommand(billingQuery, conn, transaction);
                billingCmd.Parameters.AddWithValue("billingId", billingId);
                billingCmd.Parameters.AddWithValue("customerName", request.CustomerName ?? (object)DBNull.Value);
                billingCmd.Parameters.AddWithValue("customerContact", request.CustomerContact ?? (object)DBNull.Value);
                await billingCmd.ExecuteNonQueryAsync(ct);

                // Create session record
                var sessionQuery = @"
                    INSERT INTO public.table_sessions (session_id, table_label, server_id, server_name, start_time, status, billing_id, original_table_id)
                    VALUES (@sessionId, @tableLabel, @serverId, @serverName, NOW(), 'active', @billingId, @originalTableId)";

                using var sessionCmd = new NpgsqlCommand(sessionQuery, conn, transaction);
                sessionCmd.Parameters.AddWithValue("sessionId", sessionId);
                sessionCmd.Parameters.AddWithValue("tableLabel", request.TableId);
                sessionCmd.Parameters.AddWithValue("serverId", request.ServerId);
                sessionCmd.Parameters.AddWithValue("serverName", request.ServerName);
                sessionCmd.Parameters.AddWithValue("billingId", billingId);
                sessionCmd.Parameters.AddWithValue("originalTableId", request.TableId);
                await sessionCmd.ExecuteNonQueryAsync(ct);

                // Link billing and session
                var linkQuery = @"
                    INSERT INTO public.billing_sessions (billing_id, session_id)
                    VALUES (@billingId, @sessionId)";

                using var linkCmd = new NpgsqlCommand(linkQuery, conn, transaction);
                linkCmd.Parameters.AddWithValue("billingId", billingId);
                linkCmd.Parameters.AddWithValue("sessionId", sessionId);
                await linkCmd.ExecuteNonQueryAsync(ct);

                await transaction.CommitAsync(ct);

                _logger?.LogInformation("Created billing {BillingId} with initial session {SessionId}", billingId, sessionId);

                return new CreateBillingResponse
                {
                    Success = true,
                    Message = "Billing created successfully",
                    BillingId = billingId,
                    SessionId = sessionId
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(ct);
                _logger?.LogError(ex, "Failed to create billing for table {TableId}", request.TableId);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error creating billing for table {TableId}", request.TableId);
            return new CreateBillingResponse
            {
                Success = false,
                Message = $"Failed to create billing: {ex.Message}",
                ErrorCode = "CREATE_FAILED"
            };
        }
    }

    /// <summary>
    /// Moves a session from one table to another, maintaining billing continuity
    /// </summary>
    public async Task<SessionMoveResponse> MoveSessionAsync(SessionMoveRequest request, CancellationToken ct = default)
    {
        try
        {
            _logger?.LogInformation("Moving session {SessionId} from table {FromTable} to {ToTable}", 
                request.SessionId, request.FromTableId, request.ToTableId);

            if (!_useDatabase)
            {
                return new SessionMoveResponse
                {
                    Success = false,
                    Message = "Database not available",
                    ErrorCode = "DB_UNAVAILABLE"
                };
            }

            using var conn = new NpgsqlConnection(_connString);
            await conn.OpenAsync(ct);

            using var transaction = await conn.BeginTransactionAsync(ct);
            try
            {
                // Get current session info and billing ID
                var getSessionQuery = @"
                    SELECT billing_id, table_label, status 
                    FROM public.table_sessions 
                    WHERE session_id = @sessionId";

                Guid? billingId = null;
                string? currentTable = null;
                string? currentStatus = null;

                using var getCmd = new NpgsqlCommand(getSessionQuery, conn, transaction);
                getCmd.Parameters.AddWithValue("sessionId", request.SessionId);
                using var reader = await getCmd.ExecuteReaderAsync(ct);
                
                if (await reader.ReadAsync(ct))
                {
                    billingId = (Guid)reader["billing_id"];
                    currentTable = (string)reader["table_label"];
                    currentStatus = (string)reader["status"];
                }
                await reader.CloseAsync();

                if (billingId == null)
                {
                    return new SessionMoveResponse
                    {
                        Success = false,
                        Message = "Session not found",
                        ErrorCode = "SESSION_NOT_FOUND"
                    };
                }

                if (currentStatus == "closed")
                {
                    return new SessionMoveResponse
                    {
                        Success = false,
                        Message = "Cannot move a closed session",
                        ErrorCode = "SESSION_CLOSED"
                    };
                }

                // Update current session as moved
                var updateSessionQuery = @"
                    UPDATE public.table_sessions 
                    SET status = 'moved', 
                        destination_table_id = @destinationTable, 
                        moved_at = NOW(),
                        end_time = NOW()
                    WHERE session_id = @sessionId";

                using var updateCmd = new NpgsqlCommand(updateSessionQuery, conn, transaction);
                updateCmd.Parameters.AddWithValue("sessionId", request.SessionId);
                updateCmd.Parameters.AddWithValue("destinationTable", request.ToTableId);
                await updateCmd.ExecuteNonQueryAsync(ct);

                // Record the move in table_session_moves
                var recordMoveQuery = @"
                    INSERT INTO public.table_session_moves (session_id, from_label, to_label, moved_at)
                    VALUES (@sessionId, @fromLabel, @toLabel, NOW())";

                using var moveCmd = new NpgsqlCommand(recordMoveQuery, conn, transaction);
                moveCmd.Parameters.AddWithValue("sessionId", request.SessionId);
                moveCmd.Parameters.AddWithValue("fromLabel", request.FromTableId);
                moveCmd.Parameters.AddWithValue("toLabel", request.ToTableId);
                await moveCmd.ExecuteNonQueryAsync(ct);

                // Create new session at destination table
                var newSessionId = Guid.NewGuid();
                var createNewSessionQuery = @"
                    INSERT INTO public.table_sessions (session_id, table_label, server_id, server_name, start_time, status, billing_id, original_table_id)
                    VALUES (@newSessionId, @tableLabel, @serverId, @serverName, NOW(), 'active', @billingId, @originalTableId)";

                using var newSessionCmd = new NpgsqlCommand(createNewSessionQuery, conn, transaction);
                newSessionCmd.Parameters.AddWithValue("newSessionId", newSessionId);
                newSessionCmd.Parameters.AddWithValue("tableLabel", request.ToTableId);
                newSessionCmd.Parameters.AddWithValue("serverId", request.ServerId);
                newSessionCmd.Parameters.AddWithValue("serverName", request.ServerName);
                newSessionCmd.Parameters.AddWithValue("billingId", billingId);
                newSessionCmd.Parameters.AddWithValue("originalTableId", currentTable ?? request.FromTableId);
                await newSessionCmd.ExecuteNonQueryAsync(ct);

                // Link new session to billing
                var linkNewSessionQuery = @"
                    INSERT INTO public.billing_sessions (billing_id, session_id)
                    VALUES (@billingId, @sessionId)";

                using var linkCmd = new NpgsqlCommand(linkNewSessionQuery, conn, transaction);
                linkCmd.Parameters.AddWithValue("billingId", billingId);
                linkCmd.Parameters.AddWithValue("sessionId", newSessionId);
                await linkCmd.ExecuteNonQueryAsync(ct);

                await transaction.CommitAsync(ct);

                _logger?.LogInformation("Successfully moved session {SessionId} to new session {NewSessionId} at table {ToTable}", 
                    request.SessionId, newSessionId, request.ToTableId);

                return new SessionMoveResponse
                {
                    Success = true,
                    Message = $"Session moved from {request.FromTableId} to {request.ToTableId}",
                    NewSessionId = newSessionId
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(ct);
                _logger?.LogError(ex, "Failed to move session {SessionId}", request.SessionId);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error moving session {SessionId}", request.SessionId);
            return new SessionMoveResponse
            {
                Success = false,
                Message = $"Failed to move session: {ex.Message}",
                ErrorCode = "MOVE_FAILED"
            };
        }
    }

    /// <summary>
    /// Gets comprehensive billing information aggregated across all sessions and orders
    /// </summary>
    public async Task<BillingDto?> GetBillingByIdAsync(Guid billingId, CancellationToken ct = default)
    {
        try
        {
            _logger?.LogInformation("Retrieving billing information for {BillingId}", billingId);

            if (!_useDatabase)
            {
                _logger?.LogWarning("Database not available for billing retrieval");
                return null;
            }

            using var conn = new NpgsqlConnection(_connString);
            await conn.OpenAsync(ct);

            // Get billing info
            var billingQuery = @"
                SELECT billing_id, customer_name, customer_contact, total_amount, subtotal, tax_amount, 
                       discount_amount, status, created_at, updated_at, closed_at, paid_at
                FROM public.billings 
                WHERE billing_id = @billingId";

            BillingDto? billing = null;

            using var billingCmd = new NpgsqlCommand(billingQuery, conn);
            billingCmd.Parameters.AddWithValue("billingId", billingId);
            using var reader = await billingCmd.ExecuteReaderAsync(ct);

            if (await reader.ReadAsync(ct))
            {
                billing = new BillingDto
                {
                    BillingId = (Guid)reader["billing_id"],
                    CustomerName = reader["customer_name"] == DBNull.Value ? null : (string?)reader["customer_name"],
                    CustomerContact = reader["customer_contact"] == DBNull.Value ? null : (string?)reader["customer_contact"],
                    TotalAmount = (decimal)reader["total_amount"],
                    Subtotal = (decimal)reader["subtotal"],
                    TaxAmount = (decimal)reader["tax_amount"],
                    DiscountAmount = (decimal)reader["discount_amount"],
                    Status = (string)reader["status"],
                    CreatedAt = (DateTime)reader["created_at"],
                    UpdatedAt = (DateTime)reader["updated_at"],
                    ClosedAt = reader["closed_at"] == DBNull.Value ? null : (DateTime?)reader["closed_at"],
                    PaidAt = reader["paid_at"] == DBNull.Value ? null : (DateTime?)reader["paid_at"]
                };
            }
            await reader.CloseAsync();

            if (billing == null)
            {
                _logger?.LogWarning("Billing {BillingId} not found", billingId);
                return null;
            }

            // Get sessions for this billing
            var sessionsQuery = @"
                SELECT ts.session_id, ts.table_label, ts.server_id, ts.server_name, ts.status,
                       ts.start_time, ts.end_time, ts.original_table_id, ts.destination_table_id, ts.moved_at
                FROM public.table_sessions ts
                INNER JOIN public.billing_sessions bs ON ts.session_id = bs.session_id
                WHERE bs.billing_id = @billingId
                ORDER BY ts.start_time";

            var sessions = new List<BillingSessionDto>();

            using var sessionsCmd = new NpgsqlCommand(sessionsQuery, conn);
            sessionsCmd.Parameters.AddWithValue("billingId", billingId);
            using var sessionsReader = await sessionsCmd.ExecuteReaderAsync(ct);

            while (await sessionsReader.ReadAsync(ct))
            {
                var session = new BillingSessionDto
                {
                    SessionId = (Guid)sessionsReader["session_id"],
                    BillingId = billingId,
                    TableLabel = (string)sessionsReader["table_label"],
                    ServerId = (string)sessionsReader["server_id"],
                    ServerName = (string)sessionsReader["server_name"],
                    Status = (string)sessionsReader["status"],
                    StartTime = (DateTime)sessionsReader["start_time"],
                    EndTime = sessionsReader["end_time"] == DBNull.Value ? null : (DateTime?)sessionsReader["end_time"],
                    OriginalTableId = sessionsReader["original_table_id"] == DBNull.Value ? null : (string?)sessionsReader["original_table_id"],
                    DestinationTableId = sessionsReader["destination_table_id"] == DBNull.Value ? null : (string?)sessionsReader["destination_table_id"],
                    MovedAt = sessionsReader["moved_at"] == DBNull.Value ? null : (DateTime?)sessionsReader["moved_at"]
                };
                sessions.Add(session);
            }
            await sessionsReader.CloseAsync();

            billing.Sessions = sessions;

            // Get orders and items for this billing
            var ordersQuery = @"
                SELECT o.order_id, o.session_id, o.billing_id, o.table_id, o.server_id, o.server_name,
                       o.status, o.subtotal, o.discount_total, o.tax_total, o.total, o.profit_total,
                       o.created_at, o.updated_at, o.closed_at, o.delivery_status,
                       oi.order_item_id, oi.menu_item_id, oi.combo_id, oi.quantity, oi.delivered_quantity,
                       oi.base_price, oi.vendor_price, oi.price_delta, oi.line_discount, oi.line_total, oi.profit,
                       oi.is_deleted, oi.notes, oi.snapshot_name, oi.snapshot_sku, oi.snapshot_category,
                       oi.snapshot_group, oi.snapshot_version, oi.snapshot_picture_url
                FROM ord.orders o
                LEFT JOIN ord.order_items oi ON o.order_id = oi.order_id
                WHERE o.billing_id = @billingId
                ORDER BY o.created_at, oi.order_item_id";

            var ordersDict = new Dictionary<long, BillingOrderDto>();

            using var ordersCmd = new NpgsqlCommand(ordersQuery, conn);
            ordersCmd.Parameters.AddWithValue("billingId", billingId);
            using var ordersReader = await ordersCmd.ExecuteReaderAsync(ct);

            while (await ordersReader.ReadAsync(ct))
            {
                var orderId = (long)ordersReader["order_id"];

                if (!ordersDict.ContainsKey(orderId))
                {
                    ordersDict[orderId] = new BillingOrderDto
                    {
                        OrderId = orderId,
                        SessionId = (Guid)ordersReader["session_id"],
                        BillingId = ordersReader["billing_id"] == DBNull.Value ? null : (Guid?)ordersReader["billing_id"],
                        TableId = (string)ordersReader["table_id"],
                        ServerId = (string)ordersReader["server_id"],
                        ServerName = ordersReader["server_name"] == DBNull.Value ? "" : (string)ordersReader["server_name"],
                        Status = (string)ordersReader["status"],
                        Subtotal = (decimal)ordersReader["subtotal"],
                        DiscountTotal = (decimal)ordersReader["discount_total"],
                        TaxTotal = (decimal)ordersReader["tax_total"],
                        Total = (decimal)ordersReader["total"],
                        ProfitTotal = (decimal)ordersReader["profit_total"],
                        CreatedAt = (DateTime)ordersReader["created_at"],
                        UpdatedAt = (DateTime)ordersReader["updated_at"],
                        ClosedAt = ordersReader["closed_at"] == DBNull.Value ? null : (DateTime?)ordersReader["closed_at"],
                        DeliveryStatus = (string)ordersReader["delivery_status"]
                    };
                }

                // Add order item if present
                if (ordersReader["order_item_id"] != DBNull.Value)
                {
                    var item = new BillingOrderItemDto
                    {
                        OrderItemId = (long)ordersReader["order_item_id"],
                        OrderId = orderId,
                        MenuItemId = ordersReader["menu_item_id"] == DBNull.Value ? null : (long?)ordersReader["menu_item_id"],
                        ComboId = ordersReader["combo_id"] == DBNull.Value ? null : (long?)ordersReader["combo_id"],
                        Quantity = (int)ordersReader["quantity"],
                        DeliveredQuantity = (int)ordersReader["delivered_quantity"],
                        BasePrice = (decimal)ordersReader["base_price"],
                        VendorPrice = (decimal)ordersReader["vendor_price"],
                        PriceDelta = (decimal)ordersReader["price_delta"],
                        LineDiscount = (decimal)ordersReader["line_discount"],
                        LineTotal = (decimal)ordersReader["line_total"],
                        Profit = (decimal)ordersReader["profit"],
                        IsDeleted = (bool)ordersReader["is_deleted"],
                        Notes = ordersReader["notes"] == DBNull.Value ? null : (string?)ordersReader["notes"],
                        SnapshotName = ordersReader["snapshot_name"] == DBNull.Value ? null : (string?)ordersReader["snapshot_name"],
                        SnapshotSku = ordersReader["snapshot_sku"] == DBNull.Value ? null : (string?)ordersReader["snapshot_sku"],
                        SnapshotCategory = ordersReader["snapshot_category"] == DBNull.Value ? null : (string?)ordersReader["snapshot_category"],
                        SnapshotGroup = ordersReader["snapshot_group"] == DBNull.Value ? null : (string?)ordersReader["snapshot_group"],
                        SnapshotVersion = ordersReader["snapshot_version"] == DBNull.Value ? null : (int?)ordersReader["snapshot_version"],
                        SnapshotPictureUrl = ordersReader["snapshot_picture_url"] == DBNull.Value ? null : (string?)ordersReader["snapshot_picture_url"],
                        CreatedAt = (DateTime)ordersReader["created_at"],
                        UpdatedAt = (DateTime)ordersReader["updated_at"]
                    };

                    ordersDict[orderId].Items.Add(item);
                }
            }
            await ordersReader.CloseAsync();

            billing.Orders = ordersDict.Values.ToList();

            // Assign orders to their respective sessions
            foreach (var session in billing.Sessions)
            {
                session.Orders = billing.Orders.Where(o => o.SessionId == session.SessionId).ToList();
                session.OrdersCount = session.Orders.Count;
                session.SessionTotal = session.Orders.Sum(o => o.Total);
            }

            _logger?.LogInformation("Retrieved billing {BillingId} with {SessionCount} sessions and {OrderCount} orders", 
                billingId, billing.Sessions.Count, billing.Orders.Count);

            return billing;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error retrieving billing {BillingId}", billingId);
            return null;
        }
    }

    /// <summary>
    /// Gets all order items for a billing ID (for receipt generation)
    /// </summary>
    public async Task<List<ItemLine>> GetOrderItemsByBillingIdAsync(Guid billingId, CancellationToken ct = default)
    {
        try
        {
            _logger?.LogInformation("Retrieving order items for billing {BillingId} via OrderApi", billingId);

            var orderApiUrl = _configuration?["OrderApi:BaseUrl"];
            if (string.IsNullOrEmpty(orderApiUrl))
            {
                _logger?.LogError("OrderApi BaseUrl not configured in appsettings");
                return new List<ItemLine>();
            }

            var url = $"{orderApiUrl}/api/orders/by-billing/{billingId:D}/items";
            _logger?.LogInformation("Making API call to: {Url}", url);
            
            var response = await _http.GetAsync(url, ct);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            _logger?.LogInformation("API Response Status: {StatusCode}, Content: {Content}", 
                response.StatusCode, responseContent);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger?.LogWarning("Failed to get order items for billing {BillingId}: {StatusCode} - {Content}", 
                    billingId, response.StatusCode, responseContent);
                return new List<ItemLine>();
            }

            var orderItems = await response.Content.ReadFromJsonAsync<List<OrderItemDto>>(cancellationToken: ct);
            if (orderItems == null)
            {
                _logger?.LogWarning("Deserialized orderItems is null for billing {BillingId}", billingId);
                return new List<ItemLine>();
            }

            _logger?.LogInformation("Deserialized {ItemCount} order items for billing {BillingId}", orderItems.Count, billingId);

            var items = new List<ItemLine>();
            
            foreach (var oi in orderItems)
            {
                string itemName = "Unknown Item";
                decimal itemPrice = 0m;
                
                // Fetch actual menu item name and price if available
                if (oi.MenuItemId.HasValue)
                {
                    try
                    {
                        var menuItem = await _menuApiService.GetItemByIdAsync(oi.MenuItemId.Value);
                        itemName = menuItem?.Name ?? $"Menu Item {oi.MenuItemId}";
                        // Use menu item price if BasePrice + PriceDelta is 0
                        if (oi.BasePrice + oi.PriceDelta == 0 && menuItem?.SellingPrice > 0)
                        {
                            itemPrice = menuItem.SellingPrice;
                            _logger?.LogInformation("Using menu item price {Price} for {ItemName} (BasePrice: {BasePrice}, PriceDelta: {PriceDelta})", 
                                itemPrice, itemName, oi.BasePrice, oi.PriceDelta);
                        }
                        else
                        {
                            itemPrice = oi.BasePrice + oi.PriceDelta;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Failed to fetch menu item name for ID {MenuItemId}", oi.MenuItemId);
                        itemName = $"Menu Item {oi.MenuItemId}";
                        itemPrice = oi.BasePrice + oi.PriceDelta;
                    }
                }
                else if (oi.ComboId.HasValue)
                {
                    itemName = $"Combo {oi.ComboId}";
                    itemPrice = oi.BasePrice + oi.PriceDelta;
                }
                else
                {
                    itemPrice = oi.BasePrice + oi.PriceDelta;
                }
                
                _logger?.LogInformation("Order item: ID={OrderItemId}, Name='{ItemName}', Qty={Quantity}, BasePrice={BasePrice}, PriceDelta={PriceDelta}, FinalPrice={FinalPrice}", 
                    oi.OrderItemId, itemName, oi.Quantity, oi.BasePrice, oi.PriceDelta, itemPrice);
                
                items.Add(new ItemLine
                {
                    itemId = oi.OrderItemId.ToString(),
                    name = itemName,
                    quantity = oi.Quantity,
                    price = itemPrice
                });
            }

            _logger?.LogInformation("Created {ItemCount} ItemLine objects for billing {BillingId}", items.Count, billingId);
            return items;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error retrieving order items for billing {BillingId}", billingId);
            return new List<ItemLine>();
        }
    }

    /// <summary>
    /// Gets the database connection string for external use
    /// </summary>
    public string GetConnectionString()
    {
        return _connString ?? "";
    }
}
