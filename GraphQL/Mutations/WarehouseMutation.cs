using EnterpriseGradeInventoryAPI.Data;
using EnterpriseGradeInventoryAPI.Models;
using HotChocolate.Types;
using HotChocolate;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using EnterpriseGradeInventoryAPI.DTO.Input;
using EnterpriseGradeInventoryAPI.DTO.Output;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseGradeInventoryAPI.GraphQL.Mutations
{
  [ExtendObjectType(typeof(Mutation))]
  public class WarehouseMutation
  {
    
    public async Task<WarehousePayload> AddWarehouse(
        [Service] ApplicationDbContext context, 
        List<AddWarehouseInput> input,
        ClaimsPrincipal user
        )
    {
      try
      {
        if (user == null)
          throw new GraphQLException(new Error("User must be authenticated", "UNAUTHORIZED"));

        var userIdString = user.FindFirst(ClaimTypes.NameIdentifier)?.Value 
          ?? throw new GraphQLException(new Error("User ID not found in token", "INVALID_TOKEN"));

        if(!int.TryParse(userIdString, out int userId))
          throw new GraphQLException(new Error("Invalid user ID format", "INVALID_USER_ID"));
        
        foreach (var item in input)
        {
          var newWarehouse = new Warehouse
          {
            WarehouseName = item.WarehouseName,
            WarehouseCode = item.WarehouseCode,
            Address = item.Address,
            Manager = item.Manager,
            ContactEmail = item.ContactEmail,
            Region = item.Region,
            Status = item.Status,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = userId,
            CreatedByLastName = user.FindFirst(ClaimTypes.Surname)?.Value ?? "Unknown",
          };
          context.Warehouses.Add(newWarehouse);
          await context.SaveChangesAsync();

        }
        var lastWarehouse = context.Warehouses.OrderBy(w => w.Id).Last();
        return new WarehousePayload
        {
          Id = lastWarehouse.Id,
          Name = lastWarehouse.WarehouseName,
          Location = lastWarehouse.Address
        };
      }
      catch (Exception ex)
      {
        throw new GraphQLException(new Error("Failed to add warehouse(s): " + ex.Message, "WAREHOUSE_ADD_ERROR"));
      }
    }

    public async Task<SelectedWarehousePayload> UpdateWarehouse(
      [Service] ApplicationDbContext context, 
      [Service] AuditLogService auditService,
      ClaimsPrincipal user,
      int id,
      string? warehouseName,
      string? warehouseCode,
      string? location,
      string? manager,
      string? contactEmail,
      string? region,
      string? status
      )
    {
      if (user == null)
        throw new GraphQLException(new Error("User must be authenticated", "UNAUTHORIZED"));

      int userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value 
        ?? throw new GraphQLException(new Error("User ID not found in token", "INVALID_TOKEN")));

      var warehouse = await context.Warehouses.FindAsync(id);
      
      if (warehouse == null)
        throw new GraphQLException(new Error("Warehouse not found", "WAREHOUSE_NOT_FOUND"));

      warehouse.WarehouseName = warehouseName ?? warehouse.WarehouseName;
      warehouse.WarehouseCode = warehouseCode ?? warehouse.WarehouseCode;
      warehouse.Address = location ?? warehouse.Address;
      warehouse.Manager = manager ?? warehouse.Manager;
      warehouse.ContactEmail = contactEmail ?? warehouse.ContactEmail;
      warehouse.Region = region ?? warehouse.Region;
      warehouse.Status = status ?? warehouse.Status;

      await auditService.CreateAuditLog("Update", userId, "Warehouse", id, null, null, null);
      await context.SaveChangesAsync();
      
      return new SelectedWarehousePayload
      {
        WarehouseId = warehouse.Id,
        WarehouseName = warehouse.WarehouseName,
        WarehouseCode = warehouse.WarehouseCode,
        Address = warehouse.Address,
        Manager = warehouse.Manager,
        ContactEmail = warehouse.ContactEmail,
        Region = warehouse.Region,
        Status = warehouse.Status
      };
    }

    public async Task<DeletedWarehousePayload> DeleteWarehouse(
      [Service] ApplicationDbContext context, 
      [Service] AuditLogService auditService,
      ClaimsPrincipal user,
      int id
    )
    {
      if (user == null)
        throw new GraphQLException(new Error("User must be authenticated", "UNAUTHORIZED"));

      int userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value 
        ?? throw new GraphQLException(new Error("User ID not found in token", "INVALID_TOKEN")));

      var warehouse = await context.Warehouses.FirstOrDefaultAsync(w => w.Id == id)
        ?? throw new GraphQLException(new Error("Warehouse not found", "WAREHOUSE_NOT_FOUND"));

      string warehouseName = warehouse.WarehouseName;

      try
      {
        // Delete inventory items first (they reference storage locations)
        var inventoryItems = await context.Inventories
          .Where(i => i.StorageLocation != null && i.StorageLocation.WarehouseId == id)
          .ExecuteDeleteAsync();

        if(inventoryItems > 0)
          await auditService.CreateAuditLog("Delete", userId, "Inventory", id, null, null, $"Inventory items for Warehouse ID {id}");

        // Delete storage locations second (they reference warehouse)
        var storageLocation = await context.StorageLocations
          .Where(sl => sl.WarehouseId == id)
          .ExecuteDeleteAsync();

        if(storageLocation > 0)
          await auditService.CreateAuditLog("Delete", userId, "StorageLocation", id, null, null, $"Storage locations for Warehouse ID {id}");

        // Delete warehouse last
        await context.Warehouses.Where(w => w.Id == id).ExecuteDeleteAsync();
        await auditService.CreateAuditLog("Delete", userId, "Warehouse", id, null, null, warehouseName);

        return new DeletedWarehousePayload
        {
          WarehouseId = id,
          WarehouseName = warehouseName
        };
      }
      catch (Exception ex)
      {
        throw new GraphQLException(new Error("Failed to delete warehouse: " + ex.Message, "WAREHOUSE_DELETE_ERROR"));
      }
    }
  }
}