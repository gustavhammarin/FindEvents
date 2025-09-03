using System;
using System.Net.Http;
using System.Threading.Tasks;
using EventScraper.Scrapers;
using EventScraper.Utils;
using EventScraper.Interfaces;
using EventScraper.Scrapers.Tests;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using EventScraper;
using EventScraper.Services;
using Microsoft.EntityFrameworkCore;

namespace EventScraperApp
{
      public class Program
    {
         public static async Task Main(string[] args)
        {
            using var db = new ScraperDbContext();
            db.Database.EnsureCreated();

            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    // Registrera services
                    services.AddHttpClient<IHttpLoader, HttpLoader>(client =>
                    {
                        client.Timeout = TimeSpan.FromSeconds(30);
                        client.DefaultRequestHeaders.UserAgent.ParseAdd(
                            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
                            "(KHTML, like Gecko) Chrome/115.0.0.0 Safari/537.36"
                        );
                        client.DefaultRequestHeaders.Accept.ParseAdd("application/json");

                    });

                    services.AddDbContext<ScraperDbContext>();


                    services.AddSingleton<SitemapService>();
                    services.AddScoped<IFileEventRepository, FileEventRepository>();
                    services.AddScoped<IEventRepository, EventService>();

                    // Registrera alla scrapers automatiskt
                    RegisterAllScrapers(services);

                    // Pipeline
                    services.AddScoped<ScraperPipeline>();

                    // Hosted service för schemalagd körning
                    services.AddHostedService<ScraperHostedService>();
                })
                .Build();

            await host.RunAsync();
        }

        private static void RegisterAllScrapers(IServiceCollection services)
        {
            var scraperTypes = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type =>
                    type.IsSubclassOf(typeof(BaseScraper)) &&
                    !type.IsAbstract &&
                    !(type.Namespace?.Contains("Tests") ?? false) &&   // ⬅ Hoppa över testklasser
                    !(type.Name.Contains("Tests")));                   // ⬅ Extra skydd

            foreach (var scraperType in scraperTypes)
            {
                services.AddScoped(scraperType);
            }
        } 


    }  
/*      internal class Program
    {
        private static async Task Main(string[] args)
        {
            // 1) Bygg upp beroenden
            var httpClient = HttpClientProvider.Instance;
            IHttpLoader loader = new HttpLoader(httpClient);
            var sitemap = new SitemapService(loader);
            //var sitemapService = new SitemapService(loader);
            //
            //// 2) Skapa scraper-instans
            //var eksjoScraper = new EksjoScraper(loader, sitemapService);
            //
            //// 3) Kör asynkront
            //await eksjoScraper.RunAsync();
            var scraper = new EksjoScraper(loader, sitemap);
            await scraper.RunStandaloneAsync();
    
    
            Console.WriteLine("Körningen är klar!");
            Console.WriteLine("Tryck på valfri tangent för att avsluta.");
            Console.ReadKey();
        }
    }  */
}