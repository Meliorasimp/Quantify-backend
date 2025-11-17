using EnterpriseGradeInventoryAPI.Data;
using EnterpriseGradeInventoryAPI.Models;
using HotChocolate.Types;
using HotChocolate;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace EnterpriseGradeInventoryAPI.GraphQL.Mutations
{
  public class WarehouseMutation
  {
    public class AddWarehouseInput
    {
      public string WarehouseName { get; set; } = string.Empty;
      public string WarehouseCode { get; set; } = string.Empty;
      public string Address { get; set; } = string.Empty;
      public string Manager { get; set; } = string.Empty;
      public string ContactEmail { get; set; } = string.Empty;
      public string Region { get; set; } = string.Empty;
      public string Status { get; set; } = string.Empty;
    }

    public async Task<WarehousePayload> addWarehouse(
        [Service] ApplicationDbContext context, 
        List<AddWarehouseInput> input,
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

        foreach (var item in input)
        {
          var newWarehouse = new Warehouse
          {
            WarehouseName = item.WarehouseName,
            WarehouseCode = item.WarehouseCode,
            Address = item.Address,
            Manager = item.Manager,
            ContactEmail = item.ContactEmail,
            Region = item.Region,
            Status = item.Status,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = user.Id, // Keep the user ID for reference
            CreatedByLastName = user.LastName,
            // Note: 
            // If you add CreatedByLastName property to Warehouse model, you can use:
            // CreatedByLastName = user.LastName
          };
          context.Warehouses.Add(newWarehouse);
          await context.SaveChangesAsync();

        }
        var lastWarehouse = context.Warehouses.OrderBy(w => w.Id).Last();
        return new WarehousePayload
        {
          Id = lastWarehouse.Id,
          Name = lastWarehouse.WarehouseName,
          Location = lastWarehouse.Address
        };
      }
      catch (Exception ex)
      {
        throw new GraphQLException(new Error("Failed to add warehouse(s): " + ex.Message, "WAREHOUSE_ADD_ERROR"));
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
      Console.WriteLine($"DEBUG: Extracted token: {token.Substring(0, Math.Min(20, token.Length))}...");
      
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
        Console.WriteLine("DEBUG: Token validation successful");
        
        // Get user ID from claims
        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        Console.WriteLine($"DEBUG: User ID claim: {userIdClaim}");
        
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
          Console.WriteLine("DEBUG: Invalid or missing user ID claim");
          return null;
        }

        // Get user from database
        var user = await context.Users.FindAsync(userId);
        Console.WriteLine($"DEBUG: Found user: {user?.Email}");
        return user;
      }
      catch (Exception ex)
      {
        Console.WriteLine($"DEBUG: Token validation failed: {ex.Message}");
        return null;
      }
    }

    public class WarehousePayload
    {
      public int Id { get; set; }
      public string Name { get; set; } = string.Empty;
      public string Location { get; set; } = string.Empty;
    }
  }
}