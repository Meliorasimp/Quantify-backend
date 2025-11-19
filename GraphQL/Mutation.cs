using HotChocolate.Types;
using EnterpriseGradeInventoryAPI.GraphQL.Mutations;

namespace EnterpriseGradeInventoryAPI.GraphQL
{
  public class Mutation : ObjectType
  {
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
      descriptor.Name("Mutation");

      descriptor.Field<UserMutation>(t => t.registerUser(default!, default!, default!, default!, default!))
        .Name("registerUser")
        .Description("Register a new User");
      descriptor.Field<LoginMutation>(t => t.loginUser(default!, default!, default!))
        .Name("loginUser")
        .Description("Login an existing User");
      descriptor.Field<InventoryMutation>(t => t.addInventory(default!, default!, default!))
        .Name("addInventory")
        .Description("Add a new Inventory Item");
      descriptor.Field<InventoryMutation>(t => t.DeleteInventory(default!, default!, default!))
        .Name("deleteInventory")
        .Description("Delete an Inventory Item");
      descriptor.Field<WarehouseMutation>(t => t.addWarehouse(default!, default!, default!))
        .Name("addWarehouse")
        .Description("Add a new Warehouse");
      descriptor.Field<StorageLocationMutation>(t => t.addStorageLocation(default!,default!, default!))
        .Name("addStorageLocation")
        .Description("Add a new Storage Location");
      descriptor.Field<AuditLogMutation>(t => t.CreateAuditLog(default!, default!, default!, default!, default!, default!, default!))
        .Name("createAuditLog")
        .Description("Create a new Audit Log entry");
    }
  }
}