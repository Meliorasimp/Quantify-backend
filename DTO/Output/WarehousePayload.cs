namespace EnterpriseGradeInventoryAPI.DTO.Output
{
  public class WarehousePayload
  {
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
  }

  public class SelectedWarehousePayload
  {
    public int WarehouseId { get; set; }
    public string? WarehouseName { get; set; }
    public string? WarehouseCode { get; set; }
    public string? Address { get; set; }
    public string? Manager { get; set; }
    public string? Region { get; set; }
    public string? Status { get; set; }
    public int TotalProducts { get; set; }
    public int AvailableSectors { get; set; }
    public int CapacityUtilization { get; set; }
    public string ContactEmail { get; set; } = string.Empty;
  }

  public class RecentStockMovementPayload
  {
    public int Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string User { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
  }
}