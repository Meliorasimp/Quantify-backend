using EnterpriseGradeInventoryAPI.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

[Route("api/export")]
[ApiController]
public class ExportController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    public ExportController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet] // URL: https://localhost:7009/api/export
    public async Task<IActionResult> ExportInventory()
    {
        var data = await _context.Inventories.ToListAsync();
        var csv = new StringBuilder();

        // CSV headers
        csv.AppendLine("ID,ItemSKU,ProductName,Category,WarehouseLocation,RackLocation,QuantityInStock,ReorderLevel,UnitOfMeasure,CostPerUnit,TotalValue,LastRestocked");

        foreach (var item in data)
        {
            csv.AppendLine($"\"{item.Id}\",\"{item.ItemSKU}\",\"{item.ProductName}\",\"{item.Category}\",\"{item.WarehouseLocation}\",\"{item.RackLocation}\",\"{item.QuantityInStock}\",\"{item.ReorderLevel}\",\"{item.UnitOfMeasure}\",\"{item.CostPerUnit}\",\"{item.TotalValue}\",\"{item.LastRestocked:yyyy-MM-dd}\"");
        }

        var bytes = Encoding.UTF8.GetBytes(csv.ToString());
        var fileName = $"Inventory_Export_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
        return File(bytes, "text/csv", fileName);
    }
}
