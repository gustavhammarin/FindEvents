using System;
using Domain;

namespace Application.Interfaces;

public interface IElasticService
{
    //create index

    Task CreateIndexIfNotExistsAsync();

    Task<List<string>> SearchQuery(string search);

    

}
