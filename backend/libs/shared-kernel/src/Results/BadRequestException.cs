namespace SmartSolutionsLab.Roomy.SharedKernel.Results;

public sealed class BadRequestException(Error error) : Exception(error.Message)
{
    public Error Error { get; } = error;
}
