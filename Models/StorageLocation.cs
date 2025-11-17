namespace EnterpriseGradeInventoryAPI.Models
{
  public class StorageLocation
  {
    public int Id { get; set; }
    public string LocationCode { get; set; } = null!;
    public int MaxCapacity { get; set; }
    public int OccupiedCapacity { get; set; } = 0;
    public string UnitType { get; set; } = null!;
    public string StorageType { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int WarehouseId { get; set; }
    public int UserId { get; set; }
    public string SectionName { get; set; } = null!;
    
    // Navigation properties
    public Warehouse? Warehouse { get; set; }
    public User? User { get; set; }
  }
}