using EnterpriseGradeInventoryAPI.Models;


public class PurchaseOrderItems
{
  public int Id { get; set; }
  public string ProductName { get; set; } = null!;
  public int Price { get; set; }
  public int Quantity { get; set; }
  public PurchaseOrder PurchaseOrder { get; set; } = null!;
  public int PurchaseOrderId { get; set; }  
}