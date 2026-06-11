using System.Security.Claims;
using Shouldly;
using SmartSolutionsLab.Roomy.SharedKernel.Results;
using SmartSolutionsLab.Roomy.Web.Http;

namespace SmartSolutionsLab.Roomy.Web.Http.Tests;

public class CurrentUserTests
{
    [Fact]
    public void Subject_reads_the_name_identifier_claim()
    {
        var id = Guid.CreateVersion7();

        var subject = PrincipalWith(new Claim(ClaimTypes.NameIdentifier, id.ToString())).Subject();

        subject.IsSuccess.ShouldBeTrue();
        subject.Value.ShouldBe(id);
    }

    [Fact]
    public void Subject_falls_back_to_the_sub_claim()
    {
        var id = Guid.CreateVersion7();

        PrincipalWith(new Claim("sub", id.ToString())).Subject().Value.ShouldBe(id);
    }

    [Fact]
    public void Subject_is_unauthorized_when_no_subject_claim_is_present()
    {
        var subject = PrincipalWith().Subject();

        subject.IsFailure.ShouldBeTrue();
        subject.Error.Type.ShouldBe(ErrorType.Unauthorized);
    }

    [Fact]
    public void Subject_is_unauthorized_when_the_subject_claim_is_not_a_guid()
    {
        var subject = PrincipalWith(new Claim("sub", "not-a-guid")).Subject();

        subject.IsFailure.ShouldBeTrue();
        subject.Error.Type.ShouldBe(ErrorType.Unauthorized);
    }

    [Fact]
    public void IsAdministrator_is_true_when_the_administrator_role_is_present()
    {
        PrincipalWith(new Claim(ClaimTypes.Role, RoomyRoles.Administrator)).IsAdministrator().ShouldBeTrue();
    }

    [Fact]
    public void IsAdministrator_is_false_without_the_administrator_role()
    {
        PrincipalWith(new Claim(ClaimTypes.Role, "employee")).IsAdministrator().ShouldBeFalse();
    }

    private static ClaimsPrincipal PrincipalWith(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, authenticationType: "test"));
}
