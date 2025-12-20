using EnterpriseGradeInventoryAPI.Data;
using EnterpriseGradeInventoryAPI.Models;
using EnterpriseGradeInventoryAPI.DTO.Input;
using HotChocolate.Types;
using HotChocolate;
using System.Linq;
using EnterpriseGradeInventoryAPI.DTO.Output;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EnterpriseGradeInventoryAPI.GraphQL.Queries
{
  [ExtendObjectType(typeof(Query))]
  public class WarehouseQuery
  {

    public IQueryable<Warehouse> GetAllWarehouses([Service] ApplicationDbContext context) 
        => context.Warehouses;
    public async Task<SelectedWarehousePayload> GetWarehouse(
    [Service] ApplicationDbContext context,
    int id) 
      {
          int warehouseId = id;

          var totalProducts = await context.Inventories
              .Include(i => i.StorageLocation)
              .Where(i => i.StorageLocation != null && i.StorageLocation.WarehouseId == warehouseId)
              .CountAsync();

          var availableSectors = await context.StorageLocations
              .Where(sl => sl.WarehouseId == warehouseId && sl.OccupiedCapacity < sl.MaxCapacity)
              .CountAsync();

          var maxCapacity = await context.StorageLocations
              .Where(sl => sl.WarehouseId == warehouseId)
              .SumAsync(sl => sl.MaxCapacity);

          var occupiedCapacity = await context.StorageLocations
              .Where(sl => sl.WarehouseId == warehouseId)
              .SumAsync(sl => sl.OccupiedCapacity);

          int capacityUtilization = maxCapacity == 0 
              ? 0 
              : occupiedCapacity * 100 / maxCapacity;

          var warehouse = await context.Warehouses
              .Where(w => w.Id == warehouseId)
              .Select(w => new 
              {
                  w.WarehouseName,
                  w.WarehouseCode,
                  w.Address,
                  w.Manager,
                  w.Region,
                  w.Status,
                  w.ContactEmail
              })
              .FirstOrDefaultAsync();

          if (warehouse == null)
          {
              throw new GraphQLException($"Warehouse with ID {warehouseId} not found.");
          }

          return new SelectedWarehousePayload
          {
              WarehouseId = warehouseId,
              WarehouseName = warehouse.WarehouseName,
              WarehouseCode = warehouse.WarehouseCode,
              Address = warehouse.Address,
              Manager = warehouse.Manager,
              Region = warehouse.Region,
              Status = warehouse.Status,
              TotalProducts = totalProducts,
              AvailableSectors = availableSectors,
              CapacityUtilization = capacityUtilization,
              ContactEmail = warehouse.ContactEmail
          };
      }

      public IQueryable<StockMovementPayload> SearchStockMovementBySearchInput(
            [Service] ApplicationDbContext context,
            string? searchInput
        )
    {
        IQueryable<StockMovement> query = context.StockMovements
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchInput))
        {
            var term = searchInput.Trim()
                .Replace("[", "[[]")
                .Replace("%", "[%]")
                .Replace("_", "[_]");

            query = query.Where(sm =>
                EF.Functions.Like(sm.ItemSKU, $"%{term}%") ||
                EF.Functions.Like(sm.ProductName, $"%{term}%") ||
                EF.Functions.Like(sm.Type, $"%{term}%") ||
                EF.Functions.Like(sm.WarehouseLocation, $"%{term}%")
            );
        }

        return query.Select(sm => new StockMovementPayload
        {
            Id = sm.Id,
            ItemSku = sm.ItemSKU,
            ProductName = sm.ProductName,
            Quantity = sm.Quantity,
            Type = sm.Type,
            WarehouseName = sm.WarehouseLocation,
            Timestamp = sm.Timestamp
        });
    }

    public IQueryable<StockMovementPayload> SearchStockMovementByType(
        [Service] ApplicationDbContext context,
        string? type
    ) {
        IQueryable<StockMovement> query = context.StockMovements.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(type))
        {
            var term = type.Trim();

            query = query.Where(sm =>
                EF.Functions.Like(sm.Type.ToLower(), term.ToLower())
            );
        }
        return query.Select(sm => new StockMovementPayload
        {
            Id = sm.Id,
            ItemSku = sm.ItemSKU,
            ProductName = sm.ProductName,
            Quantity = sm.Quantity,
            Type = sm.Type,
            WarehouseName = sm.WarehouseLocation,
            Timestamp = sm.Timestamp
        });
    }

    public IQueryable<StockMovementPayload> SearchStockMovementByWarehouseName(
        [Service] ApplicationDbContext context,
        string? warehouseName
    ) {
        IQueryable<StockMovement> query = context.StockMovements.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(warehouseName))
        {
            var term = warehouseName.Trim()
                .Replace("[", "[[]")
                .Replace("%", "[%]")
                .Replace("_", "[_]");

            query = query.Where(sm =>
                EF.Functions.Like(sm.WarehouseLocation, $"%{term}%")
            );
        }
        return query.Select(sm => new StockMovementPayload
        {
            Id = sm.Id,
            ItemSku = sm.ItemSKU,
            ProductName = sm.ProductName,
            Quantity = sm.Quantity,
            Type = sm.Type,
            WarehouseName = sm.WarehouseLocation,
            Timestamp = sm.Timestamp
        });
    }
  }
}