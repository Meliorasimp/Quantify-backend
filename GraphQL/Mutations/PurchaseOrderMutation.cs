using EnterpriseGradeInventoryAPI.Data;
using EnterpriseGradeInventoryAPI.Models;
using HotChocolate.Types;
using EnterpriseGradeInventoryAPI.DTO.Input;
using EnterpriseGradeInventoryAPI.DTO.Output;
using System.Security.Claims;
using System.Linq.Expressions;
using EnterpriseGradeInventoryAPI;
using EnterpriseGradeInventoryAPI.GraphQL;

[ExtendObjectType(typeof(Mutation))]
public class PurchaseOrderMutation
{
  public async Task<PurchaseOrderPayload> AddPurchaseOrder(
    [Service] ApplicationDbContext context, 
    [Service] AuditLogService auditLogService,
    ClaimsPrincipal user,
    int id,
    string supplierName,
    string deliveryWarehouse,
    string expectedDeliveryDate,
    List<PurchaseOrderItemInput> items,
    string? notes,
    string orderDate,
    int supplierID,
    float totalAmount)
  {
    var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    try
    {
      var PurchaseOrderItems = items.Select(item => new PurchaseOrderItems
      {
        ProductName = item.ProductName,
        Price = (int)item.Price,
        Quantity = item.Quantity
      }).ToList();

      var purchaseOrder = new PurchaseOrder
      {
        SupplierName = supplierName,
        DeliveryWarehouse = deliveryWarehouse,
        ExpectedDeliveryDate = DateTime.SpecifyKind(DateTime.Parse(expectedDeliveryDate), DateTimeKind.Utc),
        PurchaseOrderNumber = id,
        SupplierId = supplierID,
        Notes = notes ?? "",
        OrderDate = DateTime.SpecifyKind(DateTime.Parse(orderDate), DateTimeKind.Utc),
        TotalAmount = (int)totalAmount,
        Items = PurchaseOrderItems,
        UserId = userId
      };
      context.PurchaseOrders.Add(purchaseOrder);
      context.PurchaseOrderItems.AddRange(PurchaseOrderItems);  
      await context.SaveChangesAsync();
      await auditLogService.CreateAuditLog("Create", userId, "PurchaseOrders", purchaseOrder.Id);
      await context.SaveChangesAsync();

      return new PurchaseOrderPayload
      {
        Id = purchaseOrder.Id,
        OrderNumber = purchaseOrder.PurchaseOrderNumber,
        DeliveryWarehouse = purchaseOrder.DeliveryWarehouse,
        ExpectedDeliveryDate = purchaseOrder.ExpectedDeliveryDate,
        SupplierId = purchaseOrder.SupplierId,
        OrderDate = purchaseOrder.OrderDate,
      };
    }
    catch(Exception ex)
    {
      var innerMessage = ex.InnerException?.Message ?? "No inner exception";
      Console.WriteLine($"Purchase order creation failed: {ex.Message}");
      Console.WriteLine($"Inner exception: {innerMessage}");
      Console.WriteLine($"Stack trace: {ex.StackTrace}");
      throw new GraphQLException(new Error($"Error creating purchase order: {ex.Message} | Inner: {innerMessage}", "PURCHASE_ORDER_CREATION_FAILED"));
    }
  }
}