namespace SmartSolutionsLab.Roomy.SharedKernel.Guards;

public static class GuardExtensions
{
    public static Guard<T> IsNotNull<T>(this Guard<T?> guard)
        where T : class
    {
        if (guard.Value is null)
        {
            throw new ArgumentNullException(guard.Name);
        }

        return new Guard<T>(guard.Value, guard.Name);
    }

    public static Guard<T> IsNotNull<T>(this Guard<T?> guard)
        where T : struct
    {
        if (guard.Value is null)
        {
            throw new ArgumentNullException(guard.Name);
        }

        return new Guard<T>(guard.Value.Value, guard.Name);
    }

    public static Guard<string> IsNotEmpty(this Guard<string?> guard)
    {
        if (string.IsNullOrEmpty(guard.Value))
        {
            throw new ArgumentException("Value must not be empty.", guard.Name);
        }

        return new Guard<string>(guard.Value, guard.Name);
    }

    public static Guard<string> IsNotNullOrWhiteSpace(this Guard<string?> guard)
    {
        if (string.IsNullOrWhiteSpace(guard.Value))
        {
            throw new ArgumentException("Value must not be null or whitespace.", guard.Name);
        }

        return new Guard<string>(guard.Value, guard.Name);
    }

    public static Guard<TEnum> IsEnum<TEnum>(this Guard<string?> guard)
        where TEnum : struct, Enum
    {
        if (!Enum.TryParse<TEnum>(guard.Value, ignoreCase: true, out var parsed) || !Enum.IsDefined(parsed))
        {
            throw new ArgumentException($"Value must be one of: {string.Join(", ", Enum.GetNames<TEnum>())}.", guard.Name);
        }

        return new Guard<TEnum>(parsed, guard.Name);
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
