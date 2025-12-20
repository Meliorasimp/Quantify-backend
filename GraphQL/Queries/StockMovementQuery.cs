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
        return context.StockMovements.Select(sm => new StockMovementPayload
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
      catch (Exception ex)
      {
        throw new GraphQLException($"Error fetching stock movements: {ex.Message}");
      }
    }
  }
}