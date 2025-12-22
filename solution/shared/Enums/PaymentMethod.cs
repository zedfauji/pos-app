using System.Text.Json.Serialization;

namespace MagiDesk.Shared.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum PaymentMethod
    {
        Cash,
        Card,
        Other
    }
}
