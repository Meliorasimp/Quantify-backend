using EnterpriseGradeInventoryAPI.Data;
using EnterpriseGradeInventoryAPI.Models;

namespace EnterpriseGradeInventoryAPI
{
  //Services are Reusable components that encapsulates specific business logic
  public class StockMovementService
  {
    private readonly ApplicationDbContext _context;
    public StockMovementService(ApplicationDbContext context)
    {
      _context = context;
    }
    public async Task<StockMovement> RecordStockMovement(
      string itemSKU,
      string productName,
      int quantity,
      string type,
      string warehouseLocation,
      int userId
    )
    {
      var stockMovement = new StockMovement
      {
        ItemSKU = itemSKU,
        ProductName = productName,
        Quantity = quantity,
        Type = type,
        WarehouseLocation = warehouseLocation,
        UserId = userId,
        Timestamp = DateTime.UtcNow
      };
      await _context.StockMovements.AddAsync(stockMovement);
      return stockMovement;
    }
  }
}