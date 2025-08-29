using System;
using EventScraper.models;

namespace EventScraper.Interfaces;

public interface IEventRepository
{
    Task SaveEventsAsync(IEnumerable<EventInfo> events);
    Task<IEnumerable<EventInfo>> GetEventsAsync(DateOnly? from = null, DateOnly? to = null);
    Task<bool> EventExistsAsync(string title, DateOnly? startDate, string location);
}
