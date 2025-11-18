namespace EnterpriseGradeInventoryAPI.DTO.Input
{
  public class AddStorageLocationInput
    {
      public string LocationCode { get; set; } = string.Empty;
      public string SectionName { get; set; } = string.Empty;
      public string StorageType { get; set; } = string.Empty;
      public int MaxCapacity { get; set; }
      public string UnitType { get; set; } = string.Empty;
      public int WarehouseId { get; set; } // Frontend sends warehouse ID directly
    }
}