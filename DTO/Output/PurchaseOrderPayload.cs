namespace EnterpriseGradeInventoryAPI.DTO.Output
{
  public class PurchaseOrderPayload
  {
    public int Id { get; set; }
    public int OrderNumber { get; set; }
    public int SupplierId { get; set; }
    public string DeliveryWarehouse { get; set; } = string.Empty; 
    public DateTime OrderDate { get; set; }
    public DateTime ExpectedDeliveryDate { get; set; }
  }   
}