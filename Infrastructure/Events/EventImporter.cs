using System;
using Application.Events.DTOs;
using Application.Events.Mappers;
using Application.Interfaces;
using Domain;
using EventScraper;
using EventScraper.models;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Infrastructure.Events;

public class EventImporter : IEventImporter
{
    private readonly ScraperDbContext _scraperDbContext;
    private readonly AppDbContext _appDbContext;
    public EventImporter(ScraperDbContext scraperDbContext, AppDbContext appDbContext)
    {
        _appDbContext = appDbContext;
        _scraperDbContext = scraperDbContext;
    }

    public async Task<List<EventInfo>> Deduplicate()
    {
        var events = await _scraperDbContext.Events.ToListAsync();

        var uniqueEvents = events
            .Where(e => !string.IsNullOrWhiteSpace(e.Link))
            .GroupBy(e => e.Link, StringComparer.OrdinalIgnoreCase) 
            .Select(g => g.First())
            .ToList();

        return uniqueEvents;
    }

    public async Task ImportAsync()
    {
        using var tx = await _appDbContext.Database.BeginTransactionAsync();

        _appDbContext.Events.RemoveRange(_appDbContext.Events);
        await _appDbContext.SaveChangesAsync();

        var uniqueEvents = await Deduplicate();

        var scrapedDtos = uniqueEvents.Select(e => new EventDto
        {
            Id = e.Id,
            Title = e.Title,
            ImageUrl = e.ImageUrl,
            StartDate = e.StartDate,
            EndDate = e.EndDate,
            StartTime = e.StartTime,
            EndTime = e.EndTime,
            Location = e.Location,
            Municipality = e.Municipality,
            Link = e.Link,
            Source = e.Source,
            Category = e.Category,
            Description = e.Description

        })
        .ToList();

        var eventsToInsert = scrapedDtos.Select(EventMapper.MapToEventFromImport).ToList();

        await _appDbContext.Events.AddRangeAsync(eventsToInsert);
        await _appDbContext.SaveChangesAsync();

        await tx.CommitAsync();

    }
}
