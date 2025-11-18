namespace EnterpriseGradeInventoryAPI.DTO.Input
{
  public class InventoryInput
  {
    //Input type for GraphQL mutations
    public string ItemSKU { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string WarehouseLocation { get; set; } = string.Empty;
    public string RackLocation { get; set; } = string.Empty;
    public int QuantityInStock { get; set; }
    public int ReorderLevel { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public int CostPerUnit { get; set; }
    public int TotalValue { get; set; }
  }
}