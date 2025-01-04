using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using backend.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;


namespace backend.service;

public class ClaimConstants
{
    public const string UserId = nameof(UserId);
    public const string UserName = nameof(UserName);
}


public class TokenService(IUserRepository userRepository, IConfiguration configuration)
{
    public async Task<string> GenerateTokenAsync(string username, CancellationToken cancellationToken)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));
        var expiry = int.Parse(configuration["Jwt:ExpiryInSeconds"]!);
        var user = await userRepository.GetUserAsync(username, cancellationToken);
        if (user is null)
        {
            throw new ArgumentException("User not found in db for token service");
        }
        
        var claims = new Claim[]
        {
            new(ClaimConstants.UserId, user.Id.ToString()),
            new(ClaimConstants.UserName, user.Name)
        };
        
        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            claims: claims,
            audience: configuration["Jwt:Audience"],
            expires: DateTime.Now.Add(TimeSpan.FromSeconds(expiry)),
            signingCredentials: new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}