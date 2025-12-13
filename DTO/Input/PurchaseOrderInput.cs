namespace EnterpriseGradeInventoryAPI.DTO.Input
{
  public class PurchaseOrderItemInput
  {
    public int Id { get; set; }
    public string ProductName { get; set; } = null!;
    public float Price { get; set; }
    public int Quantity { get; set; }
  }
  
}