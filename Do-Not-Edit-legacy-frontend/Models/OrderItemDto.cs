namespace MagiDesk.Frontend.Models;

public record OrderItemDto(
    long OrderItemId,
    long? MenuItemId,
    long? ComboId,
    int Quantity,
    decimal LineDiscount,
    decimal BasePrice,
    decimal PriceDelta,
    decimal LineTotal,
    decimal Profit
);
