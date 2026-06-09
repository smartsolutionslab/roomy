namespace SmartSolutionsLab.Roomy.Organization.Domain.Employees;

// The role assigned when hiring: the base Employee, or Administrator (the elevation). A closed set that
// maps one-to-one to the published HiredRole on the EmployeeHired contract (ADR-0025).
public enum EmployeeRole
{
    Employee,
    Administrator,
}
