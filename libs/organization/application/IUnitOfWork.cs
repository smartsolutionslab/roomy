namespace SmartSolutionsLab.Roomy.Organization.Application;

// The commit seam the context owns (mirrors identity): use-case handlers persist through the
// repositories and commit the unit of work once, at the application edge.
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
