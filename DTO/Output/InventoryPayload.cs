namespace EnterpriseGradeInventoryAPI.DTO.Output
{
  public class InventoryPayload
  {
    public int Id { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int UserId { get; set; }
  }

  public class DeletedInventoryPayload
  {
    public int Id { get; set; }
    public string ItemSKU { get; set; } = string.Empty;
  }
}