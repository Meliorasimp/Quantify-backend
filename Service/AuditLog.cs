using EnterpriseGradeInventoryAPI.Data;
using EnterpriseGradeInventoryAPI.Models;

namespace EnterpriseGradeInventoryAPI
{
  //Services are Reusable components that encapsulates specific business logic
  public class AuditLogService
  {
    private readonly ApplicationDbContext _context;
    public AuditLogService(ApplicationDbContext context)
    {
      _context = context;
    }
    public async Task<AuditLog> CreateAuditLog(
    string action,
    int userId,
    string tableName,
    int recordId,
    string? oldValue = null,
    string? newValue = null
    )
    {
      var log = new AuditLog
      {
          Action = action,
          Tablename = tableName,
          RecordId = recordId,
          OldValue = oldValue,
          NewValue = newValue,
          UserId = userId,
          Timestamp = DateTime.UtcNow
      };
      await _context.AuditLogs.AddAsync(log);
      return log;
    }
  }
}