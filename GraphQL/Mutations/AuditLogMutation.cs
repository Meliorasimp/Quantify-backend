using EnterpriseGradeInventoryAPI.Models;
using EnterpriseGradeInventoryAPI;
using EnterpriseGradeInventoryAPI.GraphQL;

[ExtendObjectType(typeof(Mutation))]
public class AuditLogMutation
{
  
  public async Task<AuditLog> CreateAuditLogMutation(
    [Service] AuditLogService auditLogService,
    string action,
    int userId,
    string tableName,
    int recordId,
    string? oldValue,
    string? newValue)
    {
        return await auditLogService.CreateAuditLog(action, userId, tableName, recordId, oldValue, newValue);
    }
}