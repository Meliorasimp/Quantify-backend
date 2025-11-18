using EnterpriseGradeInventoryAPI.Data;
using EnterpriseGradeInventoryAPI.Models;
using HotChocolate;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseGradeInventoryAPI.GraphQL.Mutations
{
  public class AuditLogMutation
  {
    public async Task<AuditLog> CreateAuditLog(
      [Service] ApplicationDbContext context, 
      string action, int userId, string tablename, int RecordId, int? oldValue, int? newValue)
    {
      var log = new AuditLog
      {
        Action = action,
        Tablename = tablename,
        Timestamp = DateTime.UtcNow,
        RecordId = RecordId,
        OldValue = oldValue,
        NewValue = newValue,
        UserId = userId
      };
      context.AuditLogs.Add(log);
      await context.SaveChangesAsync();
      return log;
    }
  }
}