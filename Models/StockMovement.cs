namespace EnterpriseGradeInventoryAPI.Models
{
  public class StockMovement
  {
    public int Id { get; set; }
    public string ItemSKU { get; set; } = null!;
    public string ProductName { get; set; } = null!;
    public int Quantity { get; set; }
    public string Type { get; set; } = null!;
    public string WarehouseLocation { get; set; } = null!;
    public int UserId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
  }
}