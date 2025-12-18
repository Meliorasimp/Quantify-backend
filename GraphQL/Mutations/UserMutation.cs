using EnterpriseGradeInventoryAPI.Data;
using EnterpriseGradeInventoryAPI.Models;
using Microsoft.AspNetCore.Identity;
using HotChocolate;
using HotChocolate.Authorization;

namespace EnterpriseGradeInventoryAPI.GraphQL.Mutations
{
  [ExtendObjectType(typeof(Mutation))]
  public class UserMutation
  {
    // Register a new user
    [AllowAnonymous]
    public async Task<UserPayload> registerUser([Service] ApplicationDbContext context, string firstname, string lastname, string email, string password)
    {
      try
      {
        // Check if user already exists
        var existingUser = context.Users.FirstOrDefault(u => u.Email == email);
        if (existingUser != null)
        {
          throw new GraphQLException("User with this email already exists");
        }

        var passwordHasher = new PasswordHasher<User>();
        var user = new User
        {
          FirstName = firstname,
          LastName = lastname,
          Email = email,
          PasswordHash = passwordHasher.HashPassword(null!, password)
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();
        return new UserPayload
        {
          Id = user.Id,
          FirstName = user.FirstName,
          LastName = user.LastName,
          Email = user.Email
        };
      }
      catch (Exception ex)
      {
        throw new GraphQLException($"Registration failed: {ex.Message}");
      }
    }
  }
  // DTO for GraphQL response
  public class UserPayload
  {
    [GraphQLName("id")]
    public int Id { get; set; }
    
    [GraphQLName("firstName")]
    public string FirstName { get; set; } = string.Empty;
    
    [GraphQLName("lastName")]
    public string LastName { get; set; } = string.Empty;

    [GraphQLName("email")]
    public string Email { get; set; } = string.Empty;
    
  }

}