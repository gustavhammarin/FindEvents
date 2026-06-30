using App.Services;

namespace App.Web.Pages;

public record EventCardsViewModel(
    List<MinimalEventDto> Events,
    EventCursor? NextCursor,
    int Take);
