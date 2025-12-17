using EnterpriseGradeInventoryAPI.Data;
using EnterpriseGradeInventoryAPI.Models;
using HotChocolate;
using HotChocolate.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using EnterpriseGradeInventoryAPI.DTO.Input;
using EnterpriseGradeInventoryAPI.DTO.Output;

namespace EnterpriseGradeInventoryAPI.GraphQL.Mutations
{
  [ExtendObjectType(typeof(Mutation))]
  [Authorize] 
  public class InventoryMutation
  {
    //Add Inventory to the Database
      public async Task<List<InventoryPayload>> AddInventory(
      [Service] ApplicationDbContext context,
      [Service] AuditLogService auditService,
      [Service] StockMovementService stockMovementService,
      ClaimsPrincipal user,
      List<InventoryInput> inventory)
  {
      if (!int.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userIdInt))
          throw new GraphQLException("Invalid user ID format");

      var strategy = context.Database.CreateExecutionStrategy();

      return await strategy.ExecuteAsync(async () =>
      {
          await using var transaction = await context.Database.BeginTransactionAsync();
          var addedInventories = new List<Inventory>();

          try
          {
              foreach (var item in inventory)
              {
                  // basic validation to ensure required fields are present
                  if (string.IsNullOrWhiteSpace(item.ItemSKU) || string.IsNullOrWhiteSpace(item.ProductName))
                      throw new GraphQLException("ItemSKU and ProductName are required");
                  if (item.QuantityInStock < 0 || item.CostPerUnit < 0)
                      throw new GraphQLException("Quantity and Cost must be non-negative");

                  // check duplicates in DB + batch
                  var existingItem = await context.Inventories
                      .FirstOrDefaultAsync(i => i.ItemSKU == item.ItemSKU && i.UserId == userIdInt);
                  if (existingItem != null || addedInventories.Any(i => i.ItemSKU == item.ItemSKU))
                      throw new GraphQLException($"Item with SKU '{item.ItemSKU}' already exists");

                  var newInventory = new Inventory
                  {
                      ItemSKU = item.ItemSKU,
                      ProductName = item.ProductName,
                      Category = item.Category,
                      WarehouseLocation = item.WarehouseLocation,
                      RackLocation = item.RackLocation,
                      QuantityInStock = item.QuantityInStock,
                      ReorderLevel = item.ReorderLevel,
                      UnitOfMeasure = item.UnitOfMeasure,
                      CostPerUnit = item.CostPerUnit,
                      TotalValue = item.QuantityInStock * item.CostPerUnit,
                      UserId = userIdInt,
                      LastRestocked = DateTime.UtcNow
                  };

                  var storageLocation = await context.StorageLocations
                      .FirstOrDefaultAsync(sl => sl.SectionName == item.RackLocation && sl.UserId == userIdInt);

                  if (storageLocation != null)
                  {
                      var alreadyPlanned = addedInventories
                          .Where(i => i.StorageLocationId == storageLocation.Id)
                          .Sum(i => i.QuantityInStock);

                      if (storageLocation.OccupiedCapacity + alreadyPlanned + item.QuantityInStock > storageLocation.MaxCapacity)
                          throw new GraphQLException(
                              $"Adding {item.QuantityInStock} items would exceed storage capacity for location '{item.RackLocation}'");

                      newInventory.StorageLocationId = storageLocation.Id;
                      storageLocation.OccupiedCapacity += item.QuantityInStock;
                  }

                  context.Inventories.Add(newInventory);
                  addedInventories.Add(newInventory);
              }

              await context.SaveChangesAsync();

              foreach (var inv in addedInventories)
                  await auditService.CreateAuditLog("Add", userIdInt, "Inventories", inv.Id, null, inv.QuantityInStock.ToString());
              
              foreach (var inv in addedInventories)
                await stockMovementService.RecordStockMovement(inv.ItemSKU, inv.ProductName, inv.QuantityInStock, "Inbound", inv.WarehouseLocation, userIdInt);
              
              await context.SaveChangesAsync();
              await transaction.CommitAsync();

              return addedInventories.Select(inv => new InventoryPayload
              {
                  Id = inv.Id,
                  ItemName = inv.ProductName,
                  Quantity = inv.QuantityInStock,
                  UserId = inv.UserId
              }).ToList();
          }
          catch(Exception ex)
          {
              await transaction.RollbackAsync();
              Console.WriteLine($"AddInventory error: {ex.Message}\n{ex.StackTrace}");
              throw;
          }
      });
  }



    public async Task<DeletedInventoryPayload> DeleteInventory(
      [Service] ApplicationDbContext context, 
      [Service] AuditLogService auditService, 
      ClaimsPrincipal user, 
      int inventoryId)
    {
      if(!int.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userIdInt))
        throw new GraphQLException("Invalid user ID format");

      if(inventoryId <= 0)
        throw new GraphQLException("Invalid inventory ID");

      var item = await context.Inventories.FindAsync(inventoryId);
      if(item == null || item.UserId != userIdInt)
        throw new GraphQLException("Inventory item not found or access denied");
      
      // Update storage location occupied capacity if item is linked to a storage location
      if(item.StorageLocationId.HasValue)
      {
        var storageLocation = await context.StorageLocations.FindAsync(item.StorageLocationId.Value);
        if(storageLocation != null)
        {
          storageLocation.OccupiedCapacity -= item.QuantityInStock;
          // Ensure occupied capacity doesn't go below 0
          if(storageLocation.OccupiedCapacity < 0)
          {
            storageLocation.OccupiedCapacity = 0;
          }
        }
      }
      
      context.Inventories.Remove(item);
      await auditService.CreateAuditLog("Delete", userIdInt, "Inventories", item.Id, null, null, item.QuantityInStock.ToString());
      await context.SaveChangesAsync();
      return new DeletedInventoryPayload
      {
        Id = item.Id,
        ItemSKU = item.ItemSKU
      };
    }

    public async Task<UpdatedInventoryPayload> UpdateInventory(
      [Service] ApplicationDbContext context,
      [Service] AuditLogService auditService,
      ClaimsPrincipal user, 
      int inventoryId, 
      string? itemSKU = null, 
      string? category = null, 
      string? productName = null, 
      int? quantityInStock = null, 
      int? reorderLevel = null)
    {     
      if(!int.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userIdInt))
      {
        throw new GraphQLException("Invalid user ID format");
      }

      var item = await context.Inventories.FindAsync(inventoryId);
      if(item == null || item.UserId != userIdInt)
      {
        throw new GraphQLException("Inventory item not found or access denied");
      }

      // Store old quantity for storage location capacity update
      int oldQuantity = item.QuantityInStock;

      // Update fields only if provided
      if(!string.IsNullOrWhiteSpace(itemSKU))
        item.ItemSKU = itemSKU;
      if(!string.IsNullOrWhiteSpace(category))
        item.Category = category;
      if(!string.IsNullOrWhiteSpace(productName))
        item.ProductName = productName;
      if(quantityInStock.HasValue)
        item.QuantityInStock = quantityInStock.Value;
      if(reorderLevel.HasValue)
        item.ReorderLevel = reorderLevel.Value;
      item.TotalValue = item.QuantityInStock * item.CostPerUnit;

      // Update storage location occupied capacity if quantity changed
      if(item.StorageLocationId.HasValue && oldQuantity != item.QuantityInStock)
      {
        var storageLocation = await context.StorageLocations.FindAsync(item.StorageLocationId.Value);
        if(storageLocation != null)
        {
          // Calculate the difference in quantity
          int quantityDifference = item.QuantityInStock - oldQuantity;
          
          // Check if adding more items would exceed capacity
          if(quantityDifference > 0)
          {
            if(storageLocation.OccupiedCapacity + quantityDifference > storageLocation.MaxCapacity)
            {
              throw new GraphQLException($"Updating to {item.QuantityInStock} items would exceed storage capacity. Available space: {storageLocation.MaxCapacity - storageLocation.OccupiedCapacity}");
            }
          }
          
          // Update occupied capacity
          storageLocation.OccupiedCapacity += quantityDifference;
          
          // Ensure occupied capacity doesn't go below 0
          if(storageLocation.OccupiedCapacity < 0)
          {
            storageLocation.OccupiedCapacity = 0;
          }
        }
      }

      await auditService.CreateAuditLog(
        "Update", 
        userIdInt, 
        "Inventories", 
        item.Id, 
        oldQuantity.ToString(), 
        item.QuantityInStock.ToString(),
        null
      );
      await context.SaveChangesAsync();

      return new UpdatedInventoryPayload
      {
        Id = item.Id,
        ItemSKU = item.ItemSKU,
        ProductName = item.ProductName,
        QuantityInStock = item.QuantityInStock,
        ReorderLevel = item.ReorderLevel,
        Category = item.Category
      };
    }
  }
}