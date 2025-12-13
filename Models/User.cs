namespace EnterpriseGradeInventoryAPI.Models
{
    public class User
    {
        public int Id { get; set; }
        required
        public string FirstName { get; set; }
        required
        public string LastName { get; set; }
        required
        public string Email { get; set; }
        required
        public string PasswordHash
        { get; set; }
        public string Role { get; set; } = "Pending"; // Default role is "Pending"
        //One User can Have Many Inventories (One to Many Relationship)
        public ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
        public ICollection<Warehouse> Warehouses { get; set; } = new List<Warehouse>();
        public ICollection<StorageLocation> StorageLocations { get; set; } = new List<StorageLocation>();
        public ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
    }
}