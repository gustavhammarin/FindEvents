using System;
using Application.Activities.Core;
using Application.Events.DTOs;
using Application.Events.Queries;
using Domain;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

public class EventsController() : BaseApiController
{
    
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<PagedList<EventDto, DateTime?>>> GetEventsAsync([FromQuery] EventParams eventParams)
    {
        return HandleResult(await Mediator.Send(new GetEventList.Query { Params = eventParams }));
    }

}
