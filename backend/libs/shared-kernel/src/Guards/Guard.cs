namespace SmartSolutionsLab.Roomy.SharedKernel.Guards;

public readonly struct Guard<T>(T value, string name)
{
    public T Value { get; } = value;
    public string Name { get; } = name;
}
