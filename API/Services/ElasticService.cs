using System;
using API.Configuration;
using API.Models;
using Domain;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core.GetScriptContext;
using Elastic.Clients.Elasticsearch.Inference;
using Elastic.Transport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Persistence;

namespace API.Services;

public class ElasticService : IElasticService
{
    private readonly ElasticsearchClient _client;
    private readonly ElasticSettings _elasticSettings;
    private readonly AppDbContext _context;
    private readonly ILogger<ElasticService> _logger;

    public ElasticService(IOptions<ElasticSettings> optionsMonitor, AppDbContext context, ILogger<ElasticService> logger)
    {
        _elasticSettings = optionsMonitor.Value;

        var settings = new ElasticsearchClientSettings(new Uri(_elasticSettings.Url))
        .DefaultIndex(_elasticSettings.DefaultIndex)
        .Authentication(new BasicAuthentication("elastic", "Password"))
        .ServerCertificateValidationCallback(CertificateValidations.AllowAll);

        _client = new ElasticsearchClient(settings);
        _context = context;
        _logger = logger;
    }
    public async Task CreateIndexIfNotExistsAsync()
    {
        var pingResponse = await _client.PingAsync();
        _logger.LogInformation("Ping response: Valid={IsValid}, Status={Status}",
        pingResponse.IsValidResponse, pingResponse.ApiCallDetails?.HttpStatusCode);

        var indexName = _elasticSettings.DefaultIndex;

        await _client.Indices.DeleteAsync(indexName);

        await _client.Indices.CreateAsync(indexName);

        var searchDocs = await _context.Events.Select(e => new EventSearchDoc
        {
            Id = e.Id,
            Title = e.Title,
            Location = e.Location,
            Municipality = e.Municipality,
            Category = e.Category
        })
        .ToListAsync();


        if (searchDocs.Any())
        {
            await _client.BulkAsync(b => b
                .Index(indexName)
                .IndexMany(searchDocs, (descriptor, doc) => descriptor.Id(doc.Id))
            );
        }




    }

}
