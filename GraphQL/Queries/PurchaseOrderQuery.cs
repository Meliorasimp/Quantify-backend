using EnterpriseGradeInventoryAPI.Data;
using EnterpriseGradeInventoryAPI.Models;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;
using HotChocolate;
using EnterpriseGradeInventoryAPI.DTO.Output;
using EnterpriseGradeInventoryAPI.GraphQL;
using System.Security.Claims;

[ExtendObjectType(typeof(Query))]
public class PurchaseOrderQuery
{
  public IQueryable<AllPurchaseOrdersPayload> GetAllPendingPurchaseOrders([Service] ApplicationDbContext context)
  {
    try
    {
      return context.PurchaseOrders.Where(po => po.Status == "Pending").Select(po => new AllPurchaseOrdersPayload
      {
        Id = po.Id,
        PurchaseOrderNumber = po.PurchaseOrderNumber,
        SupplierName =  po.SupplierName,
        OrderDate = po.OrderDate.ToString("dd/MM/yyyy"),
        TotalAmount = po.TotalAmount,
        Status = po.Status
      });
    }
    catch (Exception ex)
    {
      throw new GraphQLException($"Error fetching purchase orders: {ex.Message}");
    }
  }

  public IQueryable<DeliveredPurchaseOrdersPayload> GetAllDeliveredPurchasedOrders([Service] ApplicationDbContext context)
  {
    try
    {
      return context.PurchaseOrders.Where(po => po.Status == "Delivered").Select(po => new DeliveredPurchaseOrdersPayload
      {
        Id = po.Id,
        PurchaseOrderNumber = po.PurchaseOrderNumber,
        OrderDate = po.OrderDate.ToString("dd/MM/yyyy"),
        SupplierName = po.SupplierName,
        StaffResponsible = po.User.FirstName + " " + po.User.LastName
      });
    } 
    catch (Exception ex)
    {
      throw new GraphQLException($"Error fetching delivered purchase orders: {ex.Message}");
    }
  }

  public IQueryable<PurchaseOrderAuditPayload> GetPurchaseOrderAuditLogs([Service] ApplicationDbContext context)
  {
    try
    {
      var AuditLogs = 
        from AuditLog in context.AuditLogs 
          where AuditLog.Tablename == "PurchaseOrders"

        join po in context.PurchaseOrders 
          on AuditLog.RecordId equals po.Id
        
        join qty in context.PurchaseOrderItems
          .GroupBy(i => i.PurchaseOrderId)
          .Select(g => new
          {
            PurchaseOrderId = g.Key,
            TotalUnits = g.Sum(i => i.Quantity)
          })
          on po.Id equals qty.PurchaseOrderId

        select new PurchaseOrderAuditPayload
        {
          Id = AuditLog.Id,
          Action = AuditLog.Action,
          TotalUnits = qty.TotalUnits,
          SupplierName = po.SupplierName,
          Timestamp = AuditLog.Timestamp.ToString("dd/MM/yyyy HH:mm:ss")
        };
      return AuditLogs;
    }
    catch (Exception ex)
    {
      throw new GraphQLException($"Error fetching purchase order audit logs: {ex.Message}");
    }
  }
  public async Task<PurchaseOrderDetailsPayload?> GetPurchaseOrderById([Service] ApplicationDbContext context, int id, ClaimsPrincipal user)
  {
    try {
      Console.WriteLine($"[DEBUG] GetPurchaseOrderById called with ID: {id}");
      
      var order = await context.PurchaseOrders.Include(po =>po.Items).FirstOrDefaultAsync(po => po.Id == id);
      
      var items = await context.PurchaseOrderItems.Where(poi => poi.PurchaseOrderId == id).ToListAsync();
      
      if(order == null)
      {
        Console.WriteLine("[DEBUG] Returning null - order not found");
        return null;
      }

      var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
      
      if(userId == null)
        throw new GraphQLException("User not authenticated");

      if(!int.TryParse(userId, out int userIdInt))
        throw new GraphQLException("Invalid user ID format");

      var userName = await context.Users.Where(u => u.Id == int.Parse(userId)).Select(u => $"{u.FirstName} {u.LastName}").FirstOrDefaultAsync();
      
      var OrderDetails = new PurchaseOrderDetailsPayload
      {
        Id = order.Id,
        OrderDate = order.OrderDate.ToString("dd/MM/yyyy"),
        SupplierName = order.SupplierName,
        StaffResponsible = userName ?? "Unknown",
        DeliveryWarehouse = order.DeliveryWarehouse,
        ExpectedDeliveryDate = order.ExpectedDeliveryDate.ToString("dd/MM/yyyy"),
        Status = order.Status,
        Items = [.. items.Select(i => new PurchaseOrderItemPayload
        {
          Id = i.Id,
          ProductName = i.ProductName,
          Price = i.Price,
          Quantity = i.Quantity
        })]
      };
      
      Console.WriteLine($"[DEBUG] OrderDetails created successfully with {OrderDetails.Items.Count} items");
      return OrderDetails;
    }
    catch (Exception ex)
    {
      Console.WriteLine($"[ERROR] Exception in GetPurchaseOrderById: {ex.Message}");
      Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
      throw new GraphQLException($"Error fetching purchase order details: {ex.Message}");
    }
  }

  
}