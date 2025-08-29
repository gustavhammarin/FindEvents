using Application.Activities.Core;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BaseApiController : ControllerBase
    {
        private IMediator? _mediator;

        protected IMediator Mediator =>
            _mediator ??= HttpContext.RequestServices.GetService<IMediator>()
                                                      ?? throw new InvalidOperationException(
                                                          "IMediator service i unavailable");

        protected ActionResult HandleResult<T>(Result<T> result)
        {
        if (!result.IsSuccess && result.Code == 404) return NotFound();

        if (result.IsSuccess && result.Value != null) return Ok(result.Value);

        return BadRequest();
        }
    }
}
