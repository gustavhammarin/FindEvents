using API.Features.Events;
using Domain;
using Microsoft.EntityFrameworkCore;
using Persistence;
using Xunit;

namespace Tests.Events;

public class GetEventsHandlerTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly GetEventsHandler _handler;

    public GetEventsHandlerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new AppDbContext(options);
        _handler = new GetEventsHandler(_db);
    }

    public void Dispose() => _db.Dispose();

    private async Task SeedAsync(IEnumerable<Event> events)
    {
        _db.Events.AddRange(events);
        await _db.SaveChangesAsync();
    }

    private static Event MakeEvent(string id, string title, DateOnly startDate,
        string municipality = "Jönköping", string category = "Övrigt", string location = "Centrum") =>
        new()
        {
            Id = id,
            Title = title,
            StartDate = startDate,
            Municipality = municipality,
            Category = category,
            Location = location,
            Link = $"https://example.com/{id}",
            Source = "test",
        };

    [Fact]
    public async Task Returns_only_future_events_by_default()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await SeedAsync([
            MakeEvent("1", "Past", today.AddDays(-1)),
            MakeEvent("2", "Future", today.AddDays(1)),
            MakeEvent("3", "Today", today),
        ]);

        var result = await _handler.HandleAsync(new GetEventsQuery { StartDate = today.ToString() });

        Assert.Equal(2, result.Items.Count);
        Assert.All(result.Items, e => Assert.True(e.StartDate >= today));
    }

    [Fact]
    public async Task Municipality_filter_case_insensitive()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await SeedAsync([
            MakeEvent("1", "Jkpg event", today.AddDays(1), municipality: "Jönköping"),
            MakeEvent("2", "Nassjo event", today.AddDays(1), municipality: "Nässjö"),
        ]);

        var result = await _handler.HandleAsync(new GetEventsQuery
        {
            Municipality = "jönköping",
            StartDate = today.ToString()
        });

        Assert.Single(result.Items);
        Assert.Equal("Jönköping", result.Items[0].Municipality);
    }

    [Fact]
    public async Task Category_filter_returns_matching_events()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await SeedAsync([
            MakeEvent("1", "Jazz", today.AddDays(1), category: "Musik & Konsert"),
            MakeEvent("2", "Run", today.AddDays(1), category: "Sport & Tävling"),
            MakeEvent("3", "Art", today.AddDays(1), category: "Musik & Konsert"),
        ]);

        var result = await _handler.HandleAsync(new GetEventsQuery
        {
            Category = "Musik & Konsert",
            StartDate = today.ToString()
        });

        Assert.Equal(2, result.Items.Count);
        Assert.All(result.Items, e => Assert.Equal("Musik & Konsert", e.Category));
    }

    [Fact]
    public async Task Invalid_category_ignored_returns_all()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await SeedAsync([
            MakeEvent("1", "A", today.AddDays(1), category: "Musik & Konsert"),
            MakeEvent("2", "B", today.AddDays(1), category: "Sport & Tävling"),
        ]);

        var result = await _handler.HandleAsync(new GetEventsQuery
        {
            Category = "Hacker Category",
            StartDate = today.ToString()
        });

        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task Pagination_returns_correct_page_size()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await SeedAsync(Enumerable.Range(1, 20).Select(i =>
            MakeEvent(i.ToString(), $"Event {i}", today.AddDays(i))));

        var result = await _handler.HandleAsync(new GetEventsQuery
        {
            PageSize = 5,
            StartDate = today.ToString()
        });

        Assert.Equal(5, result.Items.Count);
        Assert.NotNull(result.NextCursor);
    }

    [Fact]
    public async Task Pagination_last_page_has_no_cursor()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await SeedAsync(Enumerable.Range(1, 3).Select(i =>
            MakeEvent(i.ToString(), $"Event {i}", today.AddDays(i))));

        var result = await _handler.HandleAsync(new GetEventsQuery
        {
            PageSize = 10,
            StartDate = today.ToString()
        });

        Assert.Equal(3, result.Items.Count);
        Assert.Null(result.NextCursor);
    }

    [Fact]
    public async Task Cursor_returns_next_page()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await SeedAsync(Enumerable.Range(1, 10).Select(i =>
            MakeEvent(i.ToString().PadLeft(2, '0'), $"Event {i}", today.AddDays(i))));

        var page1 = await _handler.HandleAsync(new GetEventsQuery
        {
            PageSize = 4,
            StartDate = today.ToString()
        });

        Assert.Equal(4, page1.Items.Count);
        Assert.NotNull(page1.NextCursor);

        var page2 = await _handler.HandleAsync(new GetEventsQuery
        {
            PageSize = 4,
            StartDate = today.ToString(),
            CursorStartDate = page1.NextCursor!.StartDate.ToString(),
            CursorId = page1.NextCursor.Id
        });

        Assert.Equal(4, page2.Items.Count);
        Assert.DoesNotContain(page2.Items, e => page1.Items.Any(p => p.Id == e.Id));
    }

    [Fact]
    public async Task PageSize_clamped_to_max_50()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await SeedAsync(Enumerable.Range(1, 60).Select(i =>
            MakeEvent(i.ToString().PadLeft(3, '0'), $"Event {i}", today.AddDays(i))));

        var result = await _handler.HandleAsync(new GetEventsQuery
        {
            PageSize = 999,
            StartDate = today.ToString()
        });

        Assert.Equal(50, result.Items.Count);
        Assert.NotNull(result.NextCursor);
    }

    [Fact]
    public async Task Municipality_partial_match_works()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await SeedAsync([
            MakeEvent("1", "A", today.AddDays(1), municipality: "Jönköping"),
            MakeEvent("2", "B", today.AddDays(1), municipality: "Nässjö"),
        ]);

        var result = await _handler.HandleAsync(new GetEventsQuery
        {
            Municipality = "köping",
            StartDate = today.ToString()
        });

        Assert.Single(result.Items);
    }

    [Fact]
    public async Task Empty_db_returns_empty_list()
    {
        var result = await _handler.HandleAsync(new GetEventsQuery());

        Assert.Empty(result.Items);
        Assert.Null(result.NextCursor);
    }

    [Fact]
    public async Task Results_ordered_by_date_then_id()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var d = today.AddDays(1);
        await SeedAsync([
            MakeEvent("c", "C", d),
            MakeEvent("a", "A", d),
            MakeEvent("b", "B", d.AddDays(1)),
        ]);

        var result = await _handler.HandleAsync(new GetEventsQuery { StartDate = today.ToString() });

        Assert.Equal(["a", "c", "b"], result.Items.Select(e => e.Id).ToArray());
    }
}
