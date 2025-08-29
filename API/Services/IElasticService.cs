using System;
using API.Models;
using Domain;

namespace API.Services;

public interface IElasticService
{
    //create index

    Task CreateIndexIfNotExistsAsync();

    

}
