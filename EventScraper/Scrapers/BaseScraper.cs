using EventScraper.Interfaces;
using EventScraper.models;

public abstract class BaseScraper
{
    protected readonly IHttpLoader Loader;
    
    protected BaseScraper(IHttpLoader loader)
    {
        Loader = loader;
    }
    
    // Ny metod som returnerar events istället för att printa
    public async Task<IEnumerable<EventInfo>> RunAsync(CancellationToken cancellationToken = default)
    {
        var events = new List<EventInfo>();
        var pages = await GetPageUrlsAsync();
        
        foreach (var url in pages.Take(MaxPages))
        {
            if (cancellationToken.IsCancellationRequested)
                break;
                
            var doc = await Loader.LoadHtmlAsync(url);
            if (doc == null) continue;
            
            var ev = ParseEvent(doc, url);
            events.Add(ev);
            
            // Behåll för debugging
            if (IsDebugMode)
                PrintEvent(ev, url);
        }
        
        return events;
    }
    
    // Gammal metod för bakåtkompatibilitet
    public async Task RunStandaloneAsync()
    {
        var events = await RunAsync();
        foreach (var ev in events)
        {
            PrintEvent(ev, ev.Link);
        }
    }
    
    protected abstract Task<IEnumerable<string>> GetPageUrlsAsync();
    protected abstract EventInfo ParseEvent(HtmlAgilityPack.HtmlDocument doc, string url);
    
    protected virtual void PrintEvent(EventInfo ev, string url)
    {
        Console.WriteLine($"Title: {ev.Title}");
        Console.WriteLine($"ImageUrl: {ev.ImageUrl}");
        Console.WriteLine($"StartDate: {ev.StartDate}");
        Console.WriteLine($"EndDate: {ev.EndDate}");
        Console.WriteLine($"StartTime: {ev.StartTime}");
        Console.WriteLine($"EndTime: {ev.EndTime}");
        Console.WriteLine($"Location: {ev.Location}");
        Console.WriteLine($"Description: {ev.Description}");
        Console.WriteLine($"Source: {ev.Source}");
        Console.WriteLine($"Municipality: {ev.Municipality}");
        Console.WriteLine($"Link: {ev.Link}");
    }
    
    protected virtual int MaxPages => 1000;
    protected virtual bool IsDebugMode => false;
}