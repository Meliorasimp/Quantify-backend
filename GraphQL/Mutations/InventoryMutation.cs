using EnterpriseGradeInventoryAPI.Data;
using EnterpriseGradeInventoryAPI.Models;
using HotChocolate;
using HotChocolate.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using EnterpriseGradeInventoryAPI.DTO.Input;
using EnterpriseGradeInventoryAPI.DTO.Output;
using EnterpriseGradeInventoryAPI.GraphQL.Mutations;


namespace EnterpriseGradeInventoryAPI.GraphQL.Mutations
{
  [Authorize] 
  public class InventoryMutation
  {
    private readonly AuditLogService _auditService;

    public InventoryMutation(AuditLogService auditService)
    {
      _auditService = auditService;
    }

    //Add Inventory to the Database
    public async Task<InventoryPayload> addInventory([Service] ApplicationDbContext context, ClaimsPrincipal user, List<InventoryInput> inventory)
    {
      try
      {
        // Validate userId format
        if (!int.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userIdInt))
        {
          throw new GraphQLException("Invalid user ID format");
        }

        foreach (var item in inventory)
        {
          // Input validation
          if (string.IsNullOrWhiteSpace(item.ItemSKU) || string.IsNullOrWhiteSpace(item.ProductName))
          {
            throw new GraphQLException("ItemSKU and ProductName are required");
          }

          if (item.QuantityInStock < 0 || item.CostPerUnit < 0)
          {
            throw new GraphQLException("Quantity and Cost must be non-negative");
          }

          // Check for duplicate SKU
          var existingItem = await context.Inventories.FirstOrDefaultAsync(i => i.ItemSKU == item.ItemSKU && i.UserId == userIdInt);
          if (existingItem != null)
          {
            throw new GraphQLException($"Item with SKU '{item.ItemSKU}' already exists");
          }

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

          // Handle storage location association with validation
          var storageLocation = await context.StorageLocations
            .FirstOrDefaultAsync(sl => sl.SectionName == item.RackLocation && sl.UserId == userIdInt);
          
          if (storageLocation != null)
          {
            // Check capacity constraints
            if (storageLocation.OccupiedCapacity + item.QuantityInStock > storageLocation.MaxCapacity)
            {
              throw new GraphQLException($"Adding {item.QuantityInStock} items would exceed storage capacity for location '{item.RackLocation}'");
            }

            newInventory.StorageLocationId = storageLocation.Id;
            storageLocation.OccupiedCapacity += item.QuantityInStock;
          }
        

          context.Inventories.Add(newInventory);
          await context.SaveChangesAsync();
          await _auditService.CreateAuditLog(
              "Add",       
              userIdInt,    
              "Inventories",    
              newInventory.Id,  
              null,     
              newInventory.QuantityInStock
          );
        }
        
        // Get the last added inventory for response
        var lastInventory = await context.Inventories
          .Where(i => i.UserId == userIdInt)
          .OrderByDescending(i => i.Id)
          .FirstAsync();
        
        return new InventoryPayload
        {
          Id = lastInventory.Id,
          ItemName = lastInventory.ProductName,
          Quantity = lastInventory.QuantityInStock,
          UserId = userIdInt
        };
      }
      catch (Exception ex)
      {
        // Handle exceptions
        Console.WriteLine(ex.Message);
        throw new GraphQLException("Error adding inventory", ex);
      }
    }

    public async Task<DeletedInventoryPayload> DeleteInventory([Service] ApplicationDbContext context, ClaimsPrincipal user, int inventoryId)
    {
      if(!int.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userIdInt))
      {
        throw new GraphQLException("Invalid user ID format");
      }

      if(inventoryId <= 0)
      {
        throw new GraphQLException("Invalid inventory ID");
      }

      var item = await context.Inventories.FindAsync(inventoryId);
      if(item == null || item.UserId != userIdInt)
      {
        throw new GraphQLException("Inventory item not found or access denied");
      }
      
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
      await context.SaveChangesAsync();
      
      return new DeletedInventoryPayload
      {
        Id = item.Id,
        ItemSKU = item.ItemSKU
      };
    }

    public async Task<UpdatedInventoryPayload> UpdateInventory([Service] ApplicationDbContext context, ClaimsPrincipal user, int inventoryId, string itemSKU, string category, string productName, int quantityInStock, int reorderLevel)
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

      // Update fields
      item.ItemSKU = itemSKU;
      item.Category = category;
      item.ProductName = productName;
      item.QuantityInStock = quantityInStock;
      item.ReorderLevel = reorderLevel;
      item.TotalValue = item.QuantityInStock * item.CostPerUnit;

      // Update storage location occupied capacity if quantity changed
      if(item.StorageLocationId.HasValue && oldQuantity != quantityInStock)
      {
        var storageLocation = await context.StorageLocations.FindAsync(item.StorageLocationId.Value);
        if(storageLocation != null)
        {
          // Calculate the difference in quantity
          int quantityDifference = quantityInStock - oldQuantity;
          
          // Check if adding more items would exceed capacity
          if(quantityDifference > 0)
          {
            if(storageLocation.OccupiedCapacity + quantityDifference > storageLocation.MaxCapacity)
            {
              throw new GraphQLException($"Updating to {quantityInStock} items would exceed storage capacity. Available space: {storageLocation.MaxCapacity - storageLocation.OccupiedCapacity}");
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

      await context.SaveChangesAsync();
      await _auditService.CreateAuditLog(
          "Update",       
          userIdInt,    
          "Inventories",    
          item.Id
      );

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