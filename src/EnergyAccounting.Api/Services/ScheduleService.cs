using EnergyAccounting.Api.Contracts;
using EnergyAccounting.Api.Models;

namespace EnergyAccounting.Api.Services;

public class ScheduleService(RecipeService recipes)
{
    private readonly Dictionary<Guid, WeeklySchedule> _schedules = new();
    private readonly object _lock = new();

    public ScheduleResult Add(
        Guid userId, string? dayText, string? mealText, Guid? recipeId)
    {
        if (!TryParseName(dayText, out DayOfWeek day))
        {
            return Fail("invalid_day", "Оберіть коректний день тижня.");
        }

        if (!TryParseName(mealText, out MealType meal))
        {
            return Fail("invalid_meal", "Оберіть коректний прийом їжі.");
        }

        if (recipeId is null || recipeId == Guid.Empty)
        {
            return Fail("invalid_recipe", "Оберіть рецепт.");
        }

        var recipe = recipes.FindOwned(userId, recipeId.Value);

        if (recipe is null)
        {
            return Fail(
                "recipe_not_found",
                "Рецепт не знайдено у вашому довіднику.");
        }

        lock (_lock)
        {
            var schedule = GetOrCreate(userId);

            if (!schedule.IsSlotAvailable(day, meal))
            {
                return Fail(
                    "slot_taken",
                    "Цей прийом їжі вже заплановано.");
            }

            schedule.AddEntry(day, meal, recipe);

            return new ScheduleResult(
                true,
                null,
                "Запис додано до розкладу.",
                ToDto(schedule.Entries[^1]));
        }
    }

    public IReadOnlyList<ScheduleEntryDto> GetForUser(Guid userId)
    {
        lock (_lock)
        {
            if (!_schedules.TryGetValue(userId, out var schedule))
            {
                return Array.Empty<ScheduleEntryDto>();
            }

            return schedule.Entries
                .OrderBy(e => ((int)e.Day + 6) % 7)   
                .ThenBy(e => e.Meal)
                .Select(ToDto)
                .ToList();
        }
    }

    private WeeklySchedule GetOrCreate(Guid userId)
    {
        if (!_schedules.TryGetValue(userId, out var schedule))
        {
            schedule = new WeeklySchedule { UserId = userId };
            _schedules[userId] = schedule;
        }

        return schedule;
    }

    private static bool TryParseName<T>(string? text, out T value)
        where T : struct, Enum
    {
        value = default;
        var name = text?.Trim();

        return !string.IsNullOrEmpty(name)
            && Enum.GetNames<T>().Any(n =>
                string.Equals(n, name, StringComparison.OrdinalIgnoreCase))
            && Enum.TryParse(name, ignoreCase: true, out value);
    }

    private static ScheduleEntryDto ToDto(ScheduleEntry entry)
    {
        return new ScheduleEntryDto(
            entry.Day.ToString(),
            entry.Meal.ToString(),
            entry.Recipe.Id,
            entry.Recipe.Name);
    }

    private static ScheduleResult Fail(string code, string message)
    {
        return new ScheduleResult(false, code, message, null);
    }
}