using HotChocolate;

namespace EnterpriseGradeInventoryAPI.DTO.Output
{
  public class StockMovementPayload
  {
    [GraphQLName("id")]
    public int Id { get; set; }
    [GraphQLName("itemSku")]
    public string ItemSku { get; set; } = string.Empty;
    [GraphQLName("productName")]
    public string ProductName { get; set; } = string.Empty;
    [GraphQLName("quantity")]
    public int Quantity { get; set; }
    [GraphQLName("type")]
    public string Type { get; set; } = string.Empty;
    [GraphQLName("warehouseName")]
    public string WarehouseName { get; set; } = string.Empty;
    [GraphQLName("timestamp")]
    public DateTimeOffset Timestamp { get; set; }
  }
}