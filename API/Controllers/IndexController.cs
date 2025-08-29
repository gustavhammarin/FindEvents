using System;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class IndexController : ControllerBase
{
    private readonly IElasticService _elasticService;

    public IndexController(IElasticService elasticService)
    {
        _elasticService = elasticService;
    }

    [HttpPost("create")]
    [AllowAnonymous]
    public async Task<IActionResult> CreateIndex()
    {
        await _elasticService.CreateIndexIfNotExistsAsync();
        return Ok("Index Created successfully");
    }
}
