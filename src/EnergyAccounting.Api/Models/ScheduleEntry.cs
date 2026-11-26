namespace EnergyAccounting.Api.Models;

public class ScheduleEntry
{
    public DayOfWeek Day { get; init; }
    public MealType Meal { get; init; }
    public Recipe Recipe { get; init; } = null!;
}