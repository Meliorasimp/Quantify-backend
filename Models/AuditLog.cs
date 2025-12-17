namespace EnterpriseGradeInventoryAPI.Models
{
  public class AuditLog
  {
    public int Id { get; set; }
    public string Action { get; set; } = null!;
    public string TableName { get; set; } = null!;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public int RecordId { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? DeletedValue { get; set; }
    public int UserId { get; set; }
    // Navigation property
    public User? User { get; set; }
  }
}