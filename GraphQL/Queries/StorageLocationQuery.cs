using EnterpriseGradeInventoryAPI.Data;
using EnterpriseGradeInventoryAPI.Models;
using HotChocolate.Types;
using HotChocolate;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using HotChocolate.Types.Pagination;
using StackExchange.Redis;

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
    public async Task<int> GetAverageUtilizationStatus([Service] ApplicationDbContext context, [Service] IConnectionMultiplexer redis)
    {
      var db = redis.GetDatabase();

      var cached = await db.StringGetAsync("AverageUtilizationStatus");

      if(!cached.IsNullOrEmpty)
      {
        return (int)cached;
      }
      var location = context.StorageLocations.ToList();
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
      await db.StringSetAsync("AverageUtilizationStatus", averageUtilization, TimeSpan.FromSeconds(10));
      return averageUtilization;
    }

    // Get total number of storage locations
    public async Task<int> GetTotalLocations ([Service] ApplicationDbContext context, [Service] IConnectionMultiplexer redis)
    {
      var db = redis.GetDatabase();
      var cached = await db.StringGetAsync("TotalLocations");

      if(!cached.IsNullOrEmpty)
      {
        return (int)cached;
      }
      int count = context.StorageLocations.Count();
      await db.StringSetAsync("TotalLocations", count, TimeSpan.FromSeconds(10));
      return count;
    } 

    // Get available space percentage across all storage locations
    public async Task<int> GetAvailableSpace([Service] ApplicationDbContext context, [Service] IConnectionMultiplexer redis)
    {
      var db = redis.GetDatabase();
      var cached = await db.StringGetAsync("AvailableSpace");

      if(!cached.IsNullOrEmpty)
      {
        return (int)cached;
      }
      var location = context.StorageLocations.ToList();

      int totalAvailableSpace = location.Sum(loc => loc.MaxCapacity - loc.OccupiedCapacity);

      int totalMaxCapacity = location.Sum(loc => loc.MaxCapacity);
      if(totalMaxCapacity == 0)
      {
        return 0;
      }

      double averageAvailableSpace = totalAvailableSpace / (double)totalMaxCapacity * 100;

      await db.StringSetAsync("AvailableSpace", (int)Math.Round(averageAvailableSpace), TimeSpan.FromSeconds(10));

      return (int)Math.Round(averageAvailableSpace);
    }

    // Get count of storage locations that have reached or exceeded capacity
    public async Task<int> GetCapacityAlert([Service] ApplicationDbContext context, [Service] IConnectionMultiplexer redis)
    {
      var db = redis.GetDatabase();

      var cached = await db.StringGetAsync("CapacityAlert");
      if(!cached.IsNullOrEmpty)
      {
        return (int)cached;
      }
      var location = context.StorageLocations.ToList();
      int alertCount = location.Count(loc => loc.OccupiedCapacity >= loc.MaxCapacity);

      await db.StringSetAsync("CapacityAlert", alertCount, TimeSpan.FromSeconds(10));
      return alertCount;
    }

    // Get total capacity across all storage locations
    public async Task<int> GetTotalCapacity([Service] ApplicationDbContext context, [Service] IConnectionMultiplexer redis)
    {
      var db = redis.GetDatabase();

      var cached = await db.StringGetAsync("TotalCapacity");

      if(!cached.IsNullOrEmpty)
      {
        return (int)cached;
      }
      var location = context.StorageLocations.ToList();
      int totalMaxCapacity = location.Sum(loc => loc.MaxCapacity);

      await db.StringSetAsync("TotalCapacity", totalMaxCapacity, TimeSpan.FromSeconds(10));
      return totalMaxCapacity;
    }

    // Get total occupied capacity across all storage locations
    public async Task<int> GetTotalOccupiedCapacity([Service] ApplicationDbContext context, [Service] IConnectionMultiplexer redis)
    {
      var db = redis.GetDatabase();

      var cached = await db.StringGetAsync("TotalOccupiedCapacity");

      if(!cached.IsNullOrEmpty)
      {
        return (int)cached;
      }
      var location = context.StorageLocations.ToList();
      int totalCurrentCapacity = location.Sum(loc => loc.OccupiedCapacity);
      await db.StringSetAsync("TotalOccupiedCapacity", totalCurrentCapacity, TimeSpan.FromSeconds(10));
      return totalCurrentCapacity;
    }
    
    // Get total available space across all storage locations
    public async Task<int> GetTotalAvailableSpace([Service] ApplicationDbContext context, [Service] IConnectionMultiplexer redis)
    {
        var db = redis.GetDatabase();

        var cached = await db.StringGetAsync("TotalAvailableSpace");

        if(!cached.IsNullOrEmpty)
        {
          return (int)cached;
        }
      {
        var location = context.StorageLocations.ToList();
        int totalMaxCapacity = location.Sum(loc => loc.MaxCapacity);
        int totalCurrentCapacity = location.Sum(loc => loc.OccupiedCapacity);
        int totalAvailableSpace = totalMaxCapacity - totalCurrentCapacity;

        await db.StringSetAsync("TotalAvailableSpace", totalAvailableSpace, TimeSpan.FromSeconds(10));
        return totalAvailableSpace;
      }
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