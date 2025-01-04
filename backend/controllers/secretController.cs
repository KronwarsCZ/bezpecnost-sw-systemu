using backend.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using backend.service;

namespace backend.controllers;

[Authorize]
[ApiController]
[Route("api/data")]
public class SecretController(IUserRepository userRepository) : ControllerBase
{
    private IUserRepository _userRepository = userRepository;

    [HttpGet]
    [Route("secret")]
    public async Task<ActionResult<string>> GetSecret(CancellationToken cancellationToken)
    {
        var username = User.Claims.First(claim => claim.Type == ClaimConstants.UserName).Value!;
        var user = await _userRepository.GetUserAsync(username, cancellationToken);
        if (user is null)
        {
            return BadRequest("User not found");
        }

        return Ok(user.SuperSecret);
    }
}