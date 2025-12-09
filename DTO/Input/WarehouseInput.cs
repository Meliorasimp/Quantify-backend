namespace EnterpriseGradeInventoryAPI.DTO.Input
{
  public class AddWarehouseInput
  {
    public string WarehouseName { get; set; } = string.Empty;
    public string WarehouseCode { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Manager { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
  }

}