using System;
using EventScraper.models;
using Microsoft.EntityFrameworkCore;

namespace EventScraper;

public class ScraperDbContext : DbContext
{
    public DbSet<EventInfo> Events { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var dbPath = @"C:\Users\gusta\Development\Reactivities\EventScraper\events.db";

        optionsBuilder.UseSqlite($"Data Source={dbPath}");
        Console.WriteLine($"📂 Ansluter till databas: {dbPath}");
    }

}
