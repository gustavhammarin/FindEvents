using System;
using API.Configuration;
using API.Models;
using Application.Interfaces;
using Domain;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core.GetScriptContext;
using Elastic.Clients.Elasticsearch.Inference;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Elastic.Transport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Persistence;

namespace API.Services;

public class ElasticService : IElasticService
{
    private readonly ElasticsearchClient? _client;
    private readonly ElasticSettings _elasticSettings;
    private readonly AppDbContext _context;
    private readonly ILogger<ElasticService> _logger;

    public ElasticService(IOptions<ElasticSettings> optionsMonitor, AppDbContext context, ILogger<ElasticService> logger)
    {
        _elasticSettings = optionsMonitor.Value;
        _context = context;
        _logger = logger;

        if (string.IsNullOrWhiteSpace(_elasticSettings.Url)) return;

        var settings = new ElasticsearchClientSettings(new Uri(_elasticSettings.Url))
        .DefaultIndex(_elasticSettings.DefaultIndex)
        .Authentication(new BasicAuthentication("elastic", _elasticSettings.Password))
        .ServerCertificateValidationCallback(CertificateValidations.AllowAll)
        .DisableDirectStreaming();

        _client = new ElasticsearchClient(settings);
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
            Title = e.Title.ToLower(),
            Location = e.Location.ToLower(),
            Municipality = e.Municipality.ToLower(),
            Category = e.Category.ToLower(),
            Description = e.Description.ToLower(),
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

public async Task<List<string>> SearchQuery(string search)
{
        if (_client is null) return [];

        var response = await _client.SearchAsync<EventSearchDoc>(s => s
            .Indices(_elasticSettings.DefaultIndex)
            .Size(100)
            .Query(q => q
                .MultiMatch(m => m
                    .Query(search)
                    .Fields("*")
                    .Type(TextQueryType.BestFields)
                    .Operator(Operator.Or)
            )
        )

    );

    if (!response.IsValidResponse)
    {
        _logger.LogError("Elasticsearch query failed. Term: {SearchTerm}, Error: {Error}",
            search, response.ElasticsearchServerError?.Error?.Reason ?? "Unknown error");
        return new List<string>();
    }

    _logger.LogInformation("Found {Count} results for search term: {SearchTerm}", 
        response.Documents.Count, search);
    
    return response.Documents.Select(p => p.Id).ToList();
}
}
