using System;
using Application.Activities.Core;
using Application.Core;
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
    public async Task<ActionResult<PagedList<EventDto, EventCursor?>>> GetEventsAsync([FromQuery] string? cursorStartDate = null,
        [FromQuery] string? cursorId = null,
        [FromQuery] int pageSize = 16,
        [FromQuery] string? filter = null,
        [FromQuery] string? search = null)
    {

        EventCursor? cursor = null;
        if (!string.IsNullOrEmpty(cursorStartDate) && !string.IsNullOrEmpty(cursorId))
        {
            if (DateOnly.TryParse(cursorStartDate, out var parsedDate))
            {
                cursor = new EventCursor
                {
                    StartDate = parsedDate,
                    Id = cursorId
                };
            }
        }

        // Skapa EventParams
        var eventParams = new EventParams
        {
            Cursor = cursor,
            PageSize = pageSize,
            Filter = filter,
            Search = search,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        return HandleResult(await Mediator.Send(new GetEventList.Query { Params = eventParams }));
    }

}
