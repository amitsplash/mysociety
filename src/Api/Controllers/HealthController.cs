using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MySociety.Infrastructure.Persistence;

namespace MySociety.Api.Controllers;

[AllowAnonymous]
[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public HealthController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);
        return Ok(new
        {
            status = canConnect ? "healthy" : "degraded",
            database = canConnect ? "connected" : "unavailable"
        });
    }
}
