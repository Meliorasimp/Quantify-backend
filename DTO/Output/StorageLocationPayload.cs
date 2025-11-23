namespace EnterpriseGradeInventoryAPI.DTO.Output
{
  public class StorageLocationPayload
  {
    public int Id { get; set; }
    public string LocationCode { get; set; } = string.Empty;
    public string SectionName { get; set; } = string.Empty;
    public string StorageType { get; set; } = string.Empty;
    public int MaxCapacity { get; set; }
    public string UnitType { get; set; } = string.Empty;
  }

  public class DeletedStorageLocationPayload
  {
    public int Id { get; set; }
    public string LocationCode { get; set; } = string.Empty;
  }
}