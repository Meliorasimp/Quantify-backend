using EnterpriseGradeInventoryAPI.Data;
using EnterpriseGradeInventoryAPI.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using HotChocolate;
using HotChocolate.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using static EnterpriseGradeInventoryAPI.GraphQL.Mutations.UserMutation;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace EnterpriseGradeInventoryAPI.GraphQL.Mutations
{
  [ExtendObjectType(typeof(Mutation))]
  public class LoginMutation
  {
    [GraphQLName("loginUser")]
    [AllowAnonymous]
    public async Task<LoginPayload> LoginUser(
      [Service] ApplicationDbContext context, 
      [Service] AuditLogService auditService, 
      string loginemail, 
      string loginpassword)
    {
      try
      {
        // Use environment variable or fallback to a default key for development
        var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY") ?? "SuperSecretKeyForDev12345";
        
        var passwordHasher = new PasswordHasher<User>();
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == loginemail);
        if (user == null)
        {
          throw new GraphQLException("Invalid email or password");
        }
        var passwordVerificationResult = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, loginpassword);
        if (passwordVerificationResult == PasswordVerificationResult.Failed)
        {
          throw new GraphQLException("Invalid email or password");
        }

        var claims = new[]
        {
          //Stores user information in the token
          new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
          new Claim(ClaimTypes.Email, user.Email),
        };

        //Creates a secret key that will be used to digitally sign the token
        //The key ensures that no one can tamper with the token once it's generated.
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

        //Tells the JWT Generator which algorithm to use to sign the token
        //It uses HMAC with SHA-256 hashing algorithm
        //It pairs your secret key with the hashing algorithm to prevent forgery.
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        //Created an in-memory representation of the JWT
        var token = new JwtSecurityToken(
          claims: claims,
          expires: DateTime.Now.AddHours(3), //Token expiration time
          signingCredentials: creds
        );

        //Convert the Token into a String Format that can be sent to the Client
        var jwt = new JwtSecurityTokenHandler().WriteToken(token);
        await auditService.CreateAuditLog("Login", user.Id, "Users", user.Id, null, null, null);
        await context.SaveChangesAsync();
        return new LoginPayload
        {
          Id = user.Id,
          FirstName = user.FirstName,
          LastName = user.LastName,
          Email = user.Email,
          Token = jwt
        };
      }
      catch (Exception ex) when (!(ex is GraphQLException))
      {
        throw new GraphQLException($"Login failed: {ex.Message}");
      }
    }
    public class LoginPayload
    {
      [GraphQLName("id")]
      public int Id { get; set; }
      [GraphQLName("firstName")]
      public string FirstName { get; set; } = string.Empty;
      [GraphQLName("lastName")]
      public string LastName { get; set; } = string.Empty;
      [GraphQLName("email")]
      public string Email { get; set; } = string.Empty;
      [GraphQLName("token")]
      public string Token { get; set; } = string.Empty;
    }
  }
}