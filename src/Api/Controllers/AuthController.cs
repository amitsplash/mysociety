using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySociety.Application.Auth;
using MySociety.Application.Auth.Dtos;

namespace MySociety.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<LoginResponse>> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.RegisterAsync(request, cancellationToken);
        return Created(string.Empty, result);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(request, cancellationToken);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("activate/send-otp")]
    public async Task<ActionResult<SendActivationOtpResponse>> SendActivationOtp(
        [FromBody] SendActivationOtpRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.SendActivationOtpAsync(request, cancellationToken);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("activate")]
    public async Task<ActionResult<LoginResponse>> Activate(
        [FromBody] ActivateAccountRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.ActivateAccountAsync(request, cancellationToken);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("reset-password/send-code")]
    public async Task<ActionResult<SendPasswordResetCodeResponse>> SendPasswordResetCode(
        [FromBody] SendPasswordResetCodeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.SendPasswordResetCodeAsync(request, cancellationToken);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<ActionResult<LoginResponse>> ResetPassword(
        [FromBody] ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.ResetPasswordAsync(request, cancellationToken);
        return Ok(result);
    }
}
