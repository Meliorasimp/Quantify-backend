using EnterpriseGradeInventoryAPI.Data;
using EnterpriseGradeInventoryAPI.Models;
using HotChocolate.Types;
using HotChocolate;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using EnterpriseGradeInventoryAPI.DTO.Output;

namespace EnterpriseGradeInventoryAPI.GraphQL.Queries
{
  [ExtendObjectType(typeof(Query))]
  public class StockMovementQuery
  {
    [GraphQLName("getAllStockMovements")]
    public IQueryable<StockMovementPayload> GetAllStockMovements([Service] ApplicationDbContext context)
    {
      try
      {
        var stockMovements = from sm in context.StockMovements
            join wh 
              in context.Warehouses
                on sm.WarehouseLocation 
                  equals wh.WarehouseName
            join u 
              in context.Users
                on sm.UserId 
                  equals u.Id

            select new StockMovementPayload
            {
                Id = sm.Id,
                ItemSku = sm.ItemSKU,
                ProductName = sm.ProductName,
                Quantity = sm.Quantity,
                Type = sm.Type,
                WarehouseName = wh.WarehouseName,
                User = u.FirstName + " " + u.LastName,
                Timestamp = sm.Timestamp
            };

        return stockMovements;

      } 
      catch (Exception ex)
      {
        throw new GraphQLException($"Error fetching stock movements: {ex.Message}");
      }
    }
  }
}