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
  
  public class AllPurchaseOrdersPayload
  {
    public int Id { get; set; }
    public int PurchaseOrderNumber { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string OrderDate { get; set; } = string.Empty;
    public int TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
  }

  public class PurchaseOrderItemPayload
  {
    public int Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Price { get; set; }
    public int Quantity { get; set; }
  }
  public class PurchaseOrderDetailsPayload
  {
    public int Id { get; set; }
    public string OrderDate { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string StaffResponsible { get; set; } = string.Empty;
    public string DeliveryWarehouse { get; set; } = string.Empty;
    public string ExpectedDeliveryDate { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public List<PurchaseOrderItemPayload> Items { get; set; } = new List<PurchaseOrderItemPayload>();
  }
  public class StatusChangePayload
  {
    public int Id { get; set; }
    public string Status { get; set; } = string.Empty;
  }

  public class DeliveredPurchaseOrdersPayload
  {
    public int Id { get; set; }
    public int PurchaseOrderNumber { get; set; }
    public string OrderDate { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string StaffResponsible { get; set; } = string.Empty;
  }

  public class PurchaseOrderAuditPayload
  {
    public int Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public int TotalUnits { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string Timestamp { get; set; } = string.Empty;
  }
}