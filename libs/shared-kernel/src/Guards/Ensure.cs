using System.Runtime.CompilerServices;

namespace SmartSolutionsLab.Roomy.SharedKernel.Guards;

public static class Ensure
{
    // The parameter name is captured from the call-site expression, so callers write
    // Ensure.That(customerName).IsNotEmpty() without repeating the name as a literal.
    public static Guard<T> That<T>(
        T value,
        [CallerArgumentExpression(nameof(value))] string? name = null)
        => new(value, name ?? "value");
}

public readonly struct Guard<T>(T value, string name)
{
    public T Value { get; } = value;
    public string Name { get; } = name;
}

public static class GuardExtensions
{
    public static Guard<T> IsNotNull<T>(this Guard<T> guard)
    {
        if (guard.Value is null)
        {
            throw new ArgumentNullException(guard.Name);
        }

        return guard;
    }

    public static Guard<string> IsNotEmpty(this Guard<string> guard)
    {
        if (string.IsNullOrEmpty(guard.Value))
        {
            throw new ArgumentException("Value must not be empty.", guard.Name);
        }

        return guard;
    }

    public static Guard<string> IsNotNullOrWhiteSpace(this Guard<string> guard)
    {
        if (string.IsNullOrWhiteSpace(guard.Value))
        {
            throw new ArgumentException("Value must not be null or whitespace.", guard.Name);
        }

        return guard;
    }

    public static Guard<int> IsPositive(this Guard<int> guard)
    {
        if (guard.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(guard.Name, guard.Value, "Value must be positive.");
        }

        return guard;
    }

    public static Guard<T> Satisfies<T>(this Guard<T> guard, Func<T, bool> predicate, string message)
    {
        if (!predicate(guard.Value))
        {
            throw new ArgumentException(message, guard.Name);
        }

        return guard;
    }
}
