namespace EnergyAccounting.Api.Contracts;

public record AddScheduleEntryRequest(
    string? Day,
    string? Meal,
    Guid? RecipeId
);

public record ScheduleEntryDto(
    string Day,
    string Meal,
    Guid RecipeId,
    string RecipeName
);

public record ScheduleResult(
    bool Success,
    string? Code,
    string Message,
    ScheduleEntryDto? Entry
);