namespace EnergyAccounting.Api.Models;

public class WeeklySchedule
{
    private readonly List<ScheduleEntry> _entries = new();

    public Guid UserId { get; init; }
    public IReadOnlyList<ScheduleEntry> Entries => _entries;

    public bool IsSlotAvailable(DayOfWeek day, MealType meal) =>
        !_entries.Any(e => e.Day == day && e.Meal == meal);

    public void AddEntry(DayOfWeek day, MealType meal, Recipe recipe) =>
        _entries.Add(new ScheduleEntry { Day = day, Meal = meal, Recipe = recipe });
}