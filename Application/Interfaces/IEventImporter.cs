using System;
using EventScraper.models;

namespace Application.Interfaces;

public interface IEventImporter
{
    Task ImportAsync();
    Task<List<EventInfo>> Deduplicate();
}
