using Matrix.Population.Api.Controllers;
using Matrix.Population.Application.UseCases.Person.KillPerson;
using Matrix.Population.Application.UseCases.Person.ResurrectPerson;
using Matrix.Population.Contracts.Models;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using static Matrix.Population.Api.Tests.TestSupport.PopulationApiTestSupport;

namespace Matrix.Population.Api.Tests.Controllers;

public sealed class PersonControllerTests
{
    [Fact]
    public async Task KillPerson_ReturnsOkAndSendsCommand()
    {
        Guid personId = Guid.Parse("45a3226d-02b4-4202-a27e-9a9c4118797a");
        var sender = new FakeSender();
        sender.Handle<KillPersonCommand, PersonDto>(_ => CreatePersonDto(personId));
        var controller = new PersonController(sender);

        IActionResult result = await controller.KillPerson(personId, CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        PersonDto response = Assert.IsType<PersonDto>(ok.Value);
        KillPersonCommand command = Assert.IsType<KillPersonCommand>(sender.Requests.Single());

        Assert.Equal(personId, command.Id);
        Assert.Equal(personId, response.Id);
    }

    [Fact]
    public async Task ResurrectPerson_ReturnsOkAndSendsCommand()
    {
        Guid personId = Guid.Parse("709f3636-5197-4eb7-9df0-7969d9d5ee8e");
        var sender = new FakeSender();
        sender.Handle<ResurrectPersonCommand, PersonDto>(_ => CreatePersonDto(personId, "Neo"));
        var controller = new PersonController(sender);

        IActionResult result = await controller.ResurrectPerson(personId, CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        PersonDto response = Assert.IsType<PersonDto>(ok.Value);
        ResurrectPersonCommand command = Assert.IsType<ResurrectPersonCommand>(sender.Requests.Single());

        Assert.Equal(personId, command.Id);
        Assert.Equal("Neo", response.FullName);
    }
}
