using System.Runtime.CompilerServices;

namespace SmartSolutionsLab.Roomy.SharedKernel.Guards;

public static class Ensure
{
    public static Guard<string?> That(
        string? value,
        [CallerArgumentExpression(nameof(value))] string? name = null)
        => new(value, name ?? "value");

    public static Guard<T> That<T>(
        T value,
        [CallerArgumentExpression(nameof(value))] string? name = null)
        => new(value, name ?? "value");
}
