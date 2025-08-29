using System;
using Microsoft.EntityFrameworkCore.Design;

namespace EventScraper;

public class ScraperDbContextFactory : IDesignTimeDbContextFactory<ScraperDbContext>
{
    public ScraperDbContext CreateDbContext(string[] args)
    {
        return new ScraperDbContext(); // Kör OnConfiguring i din befintliga klass
    }
}

