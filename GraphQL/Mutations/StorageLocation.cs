using EnterpriseGradeInventoryAPI.Data;
using EnterpriseGradeInventoryAPI.Models;
using HotChocolate.Types;
using HotChocolate;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using EnterpriseGradeInventoryAPI.DTO.Input;
using EnterpriseGradeInventoryAPI.DTO.Output;

namespace EnterpriseGradeInventoryAPI.GraphQL.Mutations
{
  [Authorize]
  public class StorageLocationMutation
  {
    public async Task<StorageLocationPayload> addStorageLocation(
        [Service] ApplicationDbContext context, 
        List<AddStorageLocationInput> storageLocation,
        ClaimsPrincipal user)
    {
      try
      {
        
        if (user == null)
        {
          throw new GraphQLException(new Error("User must be authenticated", "UNAUTHORIZED"));
        }

        if(!int.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userIdInt))
        {
          throw new GraphQLException(new Error("Invalid user ID format", "INVALID_USER_ID"));
        }
        

        foreach (var item in storageLocation)
        {
          // Validate that the warehouse exists using ID (more efficient)
          var warehouse = await context.Warehouses.FindAsync(item.WarehouseId);
              
          if (warehouse == null)
          {
            throw new GraphQLException(new Error($"Warehouse with ID {item.WarehouseId} not found", "WAREHOUSE_NOT_FOUND"));
          }

          var newStorageLocation = new StorageLocation
          {
            LocationCode = item.LocationCode,
            SectionName = item.SectionName,
            StorageType = item.StorageType,
            MaxCapacity = item.MaxCapacity,
            UnitType = item.UnitType,
            WarehouseId = item.WarehouseId,
            CreatedAt = DateTime.UtcNow,
            UserId = userIdInt
            
          };
          context.StorageLocations.Add(newStorageLocation);
          await context.SaveChangesAsync();
        }
        
        var lastStorageLocation = context.StorageLocations.OrderBy(sl => sl.Id).Last();
        return new StorageLocationPayload
        {
          Id = lastStorageLocation.Id,
          LocationCode = lastStorageLocation.LocationCode,
          SectionName = lastStorageLocation.SectionName,
          StorageType = lastStorageLocation.StorageType,
          MaxCapacity = lastStorageLocation.MaxCapacity,
          UnitType = lastStorageLocation.UnitType
        };
      }
      catch (Exception ex)
      {
        throw new GraphQLException(new Error("Failed to add storage location(s): " + ex.Message, "STORAGE_LOCATION_ADD_ERROR"));
      }
    }
  }
}