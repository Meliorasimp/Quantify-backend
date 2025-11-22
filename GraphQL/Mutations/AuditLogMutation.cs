using EnterpriseGradeInventoryAPI.Models;
using EnterpriseGradeInventoryAPI;

public class AuditLogMutation
{
  public async Task<AuditLog> CreateAuditLogMutation(
    [Service] AuditLogService auditLogService,
    string action,
    int userId,
    string tableName,
    int recordId,
    int? oldValue,
    int? newValue)
    {
        return await auditLogService.CreateAuditLog(action, userId, tableName, recordId, oldValue, newValue);
    }
}