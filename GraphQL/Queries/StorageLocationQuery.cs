using EnterpriseGradeInventoryAPI.Data;
using EnterpriseGradeInventoryAPI.Models;
using HotChocolate.Types;
using HotChocolate;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using HotChocolate.Types.Pagination;

namespace EnterpriseGradeInventoryAPI.GraphQL.Queries
{
  [ExtendObjectType(typeof(Query))]
  public class StorageLocationQuery
  {
    public async Task<List<StorageLocation>> GetAllStorageLocations([Service] ApplicationDbContext context)
    {
      try
      {
        var storageLocations = await context.StorageLocations
          .Include(sl => sl.Warehouse)
          .Include(sl => sl.User)
          .ToListAsync();
        return storageLocations;
      }
      catch (Exception ex)
      {
        // Log or throw a more specific error
        throw new GraphQLException($"Error fetching storage locations: {ex.Message}");
      }
    }

    // Get average utilization status across all storage locations
    public async Task<int> GetAverageUtilizationStatus([Service] ApplicationDbContext context)
    {
      var location = await context.StorageLocations.ToListAsync();
      if (location.Count == 0)
      {
        return 0;
      }

      int totalMaxCapacity = location.Sum(loc => loc.MaxCapacity);
      int totalCurrentCapacity = location.Sum(loc => loc.OccupiedCapacity);

      if(totalMaxCapacity == 0)
      {
        return 0;
      }
      int averageUtilization = (int)Math.Round((double)totalCurrentCapacity / totalMaxCapacity * 100);
      return averageUtilization;
    }

    // Get total number of storage locations
    public async Task<int> GetTotalLocations ([Service] ApplicationDbContext context)
    {
      int count = await context.StorageLocations.CountAsync();
      return count;
    } 

    // Get available space percentage across all storage locations
    public async Task<int> GetAvailableSpace([Service] ApplicationDbContext context)
    {
      var location = await context.StorageLocations.ToListAsync();

      int totalAvailableSpace = location.Sum(loc => loc.MaxCapacity - loc.OccupiedCapacity);

      int totalMaxCapacity = location.Sum(loc => loc.MaxCapacity);
      if(totalMaxCapacity == 0)
      {
        return 0;
      }

      double averageAvailableSpace = totalAvailableSpace / (double)totalMaxCapacity * 100;

      return (int)Math.Round(averageAvailableSpace);
    }

    // Get count of storage locations that have reached or exceeded capacity
    public async Task<int> GetCapacityAlert([Service] ApplicationDbContext context)
    {
      var location = await context.StorageLocations.ToListAsync();
      int alertCount = location.Count(loc => loc.OccupiedCapacity >= loc.MaxCapacity);

      return alertCount;
    }

    // Get total capacity across all storage locations
    public async Task<int> GetTotalCapacity([Service] ApplicationDbContext context)
    {
      var location = await context.StorageLocations.ToListAsync();
      int totalMaxCapacity = location.Sum(loc => loc.MaxCapacity);

      return totalMaxCapacity;
    }

    // Get total occupied capacity across all storage locations
    public async Task<int> GetTotalOccupiedCapacity([Service] ApplicationDbContext context)
    {
      var location = await context.StorageLocations.ToListAsync();
      int totalCurrentCapacity = location.Sum(loc => loc.OccupiedCapacity);
      return totalCurrentCapacity;
    }
    
    // Get total available space across all storage locations
    public async Task<int> GetTotalAvailableSpace([Service] ApplicationDbContext context)
    {
      var location = await context.StorageLocations.ToListAsync();
      int totalMaxCapacity = location.Sum(loc => loc.MaxCapacity);
      int totalCurrentCapacity = location.Sum(loc => loc.OccupiedCapacity);
      int totalAvailableSpace = totalMaxCapacity - totalCurrentCapacity;

      return totalAvailableSpace;
    }

    // Get storage location by warehouse name
    public async Task<List<StorageLocation>> GetStorageLocationWarehouse(
    [Service] ApplicationDbContext context, 
    string warehouseName)
    {
        var storageLocations = await context.StorageLocations
            .Include(sl => sl.Warehouse)
            .Include(sl => sl.User)
            .Where(sl => sl.Warehouse != null && sl.Warehouse.WarehouseName == warehouseName)
            .ToListAsync();

        if (storageLocations.Count == 0)
        {
            throw new GraphQLException($"No storage locations found for warehouse '{warehouseName}'.");
        }

        return storageLocations;
    }

    //Get Storage Location by Search Term
    public IQueryable<StorageLocation> GetStorageLocationSearch([Service] ApplicationDbContext context, string searchTerm)
    {
      var keyword = searchTerm.ToLower();
      return context.StorageLocations
        .Include(sl => sl.Warehouse)
        .Include(sl => sl.User)
        .Where(sl =>
          sl.LocationCode.ToLower().Contains(keyword) ||
          sl.Warehouse != null && sl.Warehouse.WarehouseName.ToLower().Contains(keyword));
    }

    public IQueryable<StorageLocation> GetStorageLocationByOrder([Service] ApplicationDbContext context, string orderBy)
    {
      if(orderBy.ToLower() == "utilization")
      {
        var query = context.StorageLocations
          .Include(sl => sl.Warehouse)
          .Include(sl => sl.User)
          .OrderByDescending(sl => (double)sl.OccupiedCapacity / sl.MaxCapacity);
        return query;
      }
      if(orderBy.ToLower() == "capacity")
      {
        var query = context.StorageLocations
          .Include(sl => sl.Warehouse)
          .Include(sl => sl.User)
          .OrderByDescending(sl => sl.MaxCapacity);
        return query;
      }
      else
      {
        throw new GraphQLException("Invalid orderBy parameter. Use 'utilization' or 'capacity'.");
      }
    }
  }
}