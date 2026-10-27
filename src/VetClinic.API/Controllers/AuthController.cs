using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using VetClinic.API.Auth;
using VetClinic.API.Common;

namespace VetClinic.API.Controllers;

[Route("api/auth")]
[AllowAnonymous]
public class AuthController : BaseApiController
{
    private readonly List<AuthUser> _users;
    private readonly TokenService _tokenService;

    public AuthController(IOptions<List<AuthUser>> users, TokenService tokenService)
    {
        _users = users.Value;
        _tokenService = tokenService;
    }

    /// <summary>Login com usuários fixos (admin/admin123 → Admin, user/user123 → User).</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        var user = _users.FirstOrDefault(u =>
            string.Equals(u.Username, request.Username, StringComparison.OrdinalIgnoreCase) &&
            u.Password == request.Password);

        if (user is null)
        {
            var problem = ProblemFactory.Create(HttpContext, StatusCodes.Status401Unauthorized,
                "Auth.CredenciaisInvalidas", "Usuário ou senha inválidos.");
            return Unauthorized(problem);
        }

        return Ok(_tokenService.Generate(user));
    }
}
