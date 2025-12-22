using NetArchTest.Rules;
using Xunit;

namespace MagiDesk.Client.ArchTests;

public class ArchitectureTests
{
    private const string ClientAssembly = "MagiDesk.Client";

    [Fact]
    public void Frontend_Should_Not_Reference_System_Data()
    {
        var result = Types.InAssembly(System.Reflection.Assembly.Load(ClientAssembly))
            .ShouldNot()
            .HaveDependencyOn("System.Data")
            .GetResult();

        Assert.True(result.IsSuccessful, "Frontend project has a reference to System.Data, which is forbidden.");
    }

    [Fact]
    public void ViewModels_Should_End_With_ViewModel()
    {
        var result = Types.InAssembly(System.Reflection.Assembly.Load(ClientAssembly))
            .That()
            .ResideInNamespace("MagiDesk.Client.ViewModels")
            .Should()
            .HaveNameEndingWith("ViewModel")
            .GetResult();

        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void ViewModels_Should_Not_Depend_On_HttpClient_Directly()
    {
        var result = Types.InAssembly(System.Reflection.Assembly.Load(ClientAssembly))
            .That()
            .ResideInNamespace("MagiDesk.Client.ViewModels")
            .ShouldNot()
            .HaveDependencyOn("System.Net.Http.HttpClient")
            .GetResult();

        Assert.True(result.IsSuccessful);
    }
}
