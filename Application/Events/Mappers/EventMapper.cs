using System;
using Application.Events.DTOs;
using Domain;
using Microsoft.AspNetCore.Components.Forms;

namespace Application.Events.Mappers;

public static class EventMapper
{
    public static Event MapToEventFromImport(EventDto dto)
    {
        return new Event
        {
            Id = dto.Id,
            Title = dto.Title,
            ImageUrl = dto.ImageUrl,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            Location = dto.Location,
            Municipality = dto.Municipality,
            Link = dto.Link,
            Source = dto.Source,
            Category = dto.Category,
            Description = dto.Description
        };
    }
}
