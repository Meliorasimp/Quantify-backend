using EnterpriseGradeInventoryAPI.Data;
using EnterpriseGradeInventoryAPI.Models;
using HotChocolate.Types;
using HotChocolate;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseGradeInventoryAPI.GraphQL.Mutations
{
  public class StorageLocationMutation
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

    public async Task<StorageLocationPayload> addStorageLocation(
        [Service] ApplicationDbContext context, 
        List<AddStorageLocationInput> storageLocation,
        [Service] IHttpContextAccessor httpContextAccessor)
    {
      try
      {
        // Get the current user information from JWT token
        var user = await GetCurrentUserFromToken(httpContextAccessor, context);
        
        if (user == null)
        {
          throw new GraphQLException(new Error("User must be authenticated", "UNAUTHORIZED"));
        }
        

        foreach (var item in storageLocation)
        {
          // Validate that the warehouse exists using ID (more efficient)
          var warehouse = await context.Warehouses.FindAsync(item.WarehouseId);
              
          if (warehouse == null)
          {
            throw new GraphQLException(new Error($"Warehouse with ID {item.WarehouseId} not found", "WAREHOUSE_NOT_FOUND"));
          }

          var newStorageLocation = new StorageLocation
          {
            LocationCode = item.LocationCode,
            SectionName = item.SectionName,
            StorageType = item.StorageType,
            MaxCapacity = item.MaxCapacity,
            UnitType = item.UnitType,
            WarehouseId = item.WarehouseId, // Use the provided warehouse ID directly
            CreatedAt = DateTime.UtcNow,
            UserId = user.Id // Automatically assign the authenticated user
            
          };
          context.StorageLocations.Add(newStorageLocation);
          await context.SaveChangesAsync();
        }
        
        var lastStorageLocation = context.StorageLocations.OrderBy(sl => sl.Id).Last();
        return new StorageLocationPayload
        {
          Id = lastStorageLocation.Id,
          LocationCode = lastStorageLocation.LocationCode,
          SectionName = lastStorageLocation.SectionName,
          StorageType = lastStorageLocation.StorageType,
          MaxCapacity = lastStorageLocation.MaxCapacity,
          UnitType = lastStorageLocation.UnitType
        };
      }
      catch (Exception ex)
      {
        throw new GraphQLException(new Error("Failed to add storage location(s): " + ex.Message, "STORAGE_LOCATION_ADD_ERROR"));
      }
    }

    // Helper method to get current user from JWT token
    private async Task<User?> GetCurrentUserFromToken(IHttpContextAccessor httpContextAccessor, ApplicationDbContext context)
    {
      var httpContext = httpContextAccessor.HttpContext;
      if (httpContext == null) 
      {
        return null;
      }

      // Get Authorization header
      var authHeader = httpContext.Request.Headers["Authorization"].FirstOrDefault();
      
      if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
      {
        return null;
      }

      // Extract token
      var token = authHeader.Substring("Bearer ".Length).Trim();
      
      try
      {
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY") ?? "SuperSecretKeyForDev12345";
        var key = Encoding.UTF8.GetBytes(jwtKey);

        // Validate token
        var validationParameters = new TokenValidationParameters
        {
          ValidateIssuerSigningKey = true,
          IssuerSigningKey = new SymmetricSecurityKey(key),
          ValidateIssuer = false,
          ValidateAudience = false,
          ClockSkew = TimeSpan.Zero
        };

        var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
        
        // Get user ID from claims
        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
          return null;
        }

        // Get user from database
        var user = await context.Users.FindAsync(userId);
        return user;
      }
      catch (Exception ex)
      {
        Console.WriteLine($"DEBUG: Token validation failed: {ex.Message}");
        return null;
      }
    }

    public class StorageLocationPayload
    {
      public int Id { get; set; }
      public string LocationCode { get; set; } = string.Empty;
      public string SectionName { get; set; } = string.Empty;
      public string StorageType { get; set; } = string.Empty;
      public int MaxCapacity { get; set; }
      public string UnitType { get; set; } = string.Empty;
    }
  }
}