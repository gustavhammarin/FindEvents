using System;
using Application.Activities.Core;
using Application.Core;
using Application.Events.DTOs;
using Application.Interfaces;
using Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Persistence;

namespace Application.Events.Queries;

public class GetEventList
{
    private const int MaxPageSize = 50;
    public class Query : IRequest<Result<PagedList<EventDto, EventCursor?>>>
    {
        public required EventParams Params { get; set; }
    }
    public class Handler(AppDbContext context, IElasticService elasticService) : IRequestHandler<Query, Result<PagedList<EventDto, EventCursor?>>>
    {
        public async Task<Result<PagedList<EventDto, EventCursor?>>> Handle(Query request, CancellationToken cancellationToken)
        {
            var query = context.Events.AsQueryable();

            var startDate = request.Params.StartDate;
            query = query.Where(x => x.StartDate >= startDate);

            if (request.Params.Cursor != null)
            {
                var cursorDate = request.Params.Cursor.StartDate;
                var cursorId = request.Params.Cursor.Id;
                query = query.Where(e => e.StartDate > cursorDate);
            }


            var validCategories = new[]
            {
                "Musik & Konsert", "Teater & Show", "Konst & Utställning", "Föreläsning & Utbildning", "Workshop & Kurs",
                "Sport & Tävling", "Träning & Motion", "Natur & Friluftsliv", "Mat & Dryck", "Marknad & Loppis", "Familj & Barn",
                "Seniorer & Pensionärer", "Hälsa & Välmående", "Socialt & Träffpunkt", "Övrigt"
            };

            if (!string.IsNullOrEmpty(request.Params.Filter) &&
                validCategories.Contains(request.Params.Filter))
            {
                query = query.Where(x => x.Category == request.Params.Filter);
            }



            if (!string.IsNullOrWhiteSpace(request.Params.Search))
            {
                var search = request.Params.Search.Trim().ToLower();
                var eventIds = await elasticService.SearchQuery(search);

                if (eventIds.Count > 0)
                {
                    query = query.Where(e => eventIds.Contains(e.Id));
                }
                else
                {
                    query = query.Where(e =>
                        EF.Functions.Like(e.Title.ToLower(), $"%{search}%") ||
                        (e.Description != null && EF.Functions.Like(e.Description.ToLower(), $"%{search}%")) ||
                        EF.Functions.Like(e.Location.ToLower(), $"%{search}%"));
                }
            }


            var events = await query
            .OrderBy(x => x.StartDate)
            .ThenBy(x => x.Id)
            .Take(request.Params.PageSize + 1)
            .Select(x => new EventDto
            {
                Id = x.Id,
                Title = x.Title,
                ImageUrl = x.ImageUrl,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                StartTime = x.StartTime,
                EndTime = x.EndTime,
                Location = x.Location,
                Link = x.Link,
                Source = x.Source,
                Municipality = x.Municipality,
                Category = x.Category

            }).ToListAsync(cancellationToken);

            EventCursor? nextCursor = null;
            if (events.Count > request.Params.PageSize)
            {
                var lastEvent = events[events.Count - 1];
                nextCursor = new EventCursor
                {
                    StartDate = lastEvent.StartDate ?? DateOnly.MinValue,
                    Id = lastEvent.Id
                };
                events.RemoveAt(events.Count - 1);
            }

            return Result<PagedList<EventDto, EventCursor?>>.Success(
                new PagedList<EventDto, EventCursor?>
                {
                    Items = events,
                    NextCursor = nextCursor
                }
            );
        }
    }
}
