using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class BuggyController : ControllerBase
{
    [HttpGet("not-found")]
    public ActionResult GetNotFound() => NotFound();

    [HttpGet("bad-request")]
    public ActionResult GetBadRequest() => BadRequest("bad request");

    [HttpGet("server-error")]
    public ActionResult GetServerError() => throw new Exception("server error");
}
