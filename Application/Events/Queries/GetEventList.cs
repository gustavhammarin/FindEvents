using System;
using Application.Activities.Core;
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
    public class Query : IRequest<Result<PagedList<EventDto, DateTime?>>>
    {
        public required EventParams Params { get; set; }
    }
    public class Handler(AppDbContext context, IElasticService elasticService) : IRequestHandler<Query, Result<PagedList<EventDto, DateTime?>>>
    {
        public async Task<Result<PagedList<EventDto, DateTime?>>> Handle(Query request, CancellationToken cancellationToken)
        {
            var query = context.Events
                .OrderBy(x => x.StartDate)
                .Where(x => x.StartDate >= DateOnly.FromDateTime(request.Params.Cursor ?? request.Params.StartDate))
                .AsQueryable();



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

                if (eventIds.Count == 0)
                    return Result<PagedList<EventDto, DateTime?>>.Success(
                        new PagedList<EventDto, DateTime?>
                        {
                            Items = [],
                            NextCursor = null
                        }
                    );

                query = query.Where(e => eventIds.Contains(e.Id));

            }


            var events = await query
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

            DateTime? nextCursor = null;
            if (events.Count > request.Params.PageSize)
            {
                nextCursor = events.Last().StartDate.Value.ToDateTime(TimeOnly.MinValue);
                events.RemoveAt(events.Count - 1);
            }

            return Result<PagedList<EventDto, DateTime?>>.Success(
                new PagedList<EventDto, DateTime?>
                {
                    Items = events,
                    NextCursor = nextCursor
                }
            );
        }
    }
}
