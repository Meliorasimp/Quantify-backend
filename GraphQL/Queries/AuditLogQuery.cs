using EnterpriseGradeInventoryAPI.Data;
using EnterpriseGradeInventoryAPI.Models;
using HotChocolate.Types;
using HotChocolate;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using EnterpriseGradeInventoryAPI.DTO.Output;

namespace EnterpriseGradeInventoryAPI.GraphQL.Queries
{
  [ExtendObjectType(typeof(Query))]
  public class AuditLogQuery
  {
    public IQueryable<AuditLogPayload> GetAllAuditLogs([Service] ApplicationDbContext context)
    {
      try
      {
        return context.AuditLogs.Select(a => new AuditLogPayload
        {
          Id = a.Id,
          Action = a.Action,
          TableName = a.TableName,
          Timestamp = a.Timestamp,
          RecordId = a.RecordId,
          OldValue = a.OldValue,
          NewValue = a.NewValue,
          DeletedValue = a.DeletedValue,
          UserId = a.UserId,
          UserName = a.User!.FirstName + " " + a.User!.LastName
        });
      }
      catch (Exception ex)
      {
        throw new GraphQLException($"Error fetching audit logs: {ex.Message}");
      }
    }
  }
}