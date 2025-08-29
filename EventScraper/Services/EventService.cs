using System;
using System.Transactions;
using EventScraper.Interfaces;
using EventScraper.models;

namespace EventScraper.Services;

public class EventService : IEventRepository
{
    private readonly ScraperDbContext _context;
    public EventService(ScraperDbContext context)
    {
        _context = context;
    }
    public Task<bool> EventExistsAsync(string title, DateOnly? startDate, string location)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<EventInfo>> GetEventsAsync(DateOnly? from = null, DateOnly? to = null)
    {
        throw new NotImplementedException();
    }

    public async Task SaveEventsAsync(IEnumerable<EventInfo> events)
    {
        using var transaction = _context.Database.BeginTransaction();
        _context.Events.RemoveRange(_context.Events);
        _context.SaveChanges();


        await _context.Events.AddRangeAsync(events);
        await _context.SaveChangesAsync();
        transaction.Commit();
    }
}
