# Light.DataAccessMocks

This library provides mocks for the data access abstractions of [Light.SharedCore](https://github.com/feO2x/Light.SharedCore) that you can use in your unit tests.

![Light Logo](light-logo.png)

[![License](https://img.shields.io/badge/License-MIT-green.svg?style=for-the-badge)](https://github.com/feO2x/Light.DataAccessMocks/blob/main/LICENSE)
[![NuGet](https://img.shields.io/badge/NuGet-3.0.0-blue.svg?style=for-the-badge)](https://www.nuget.org/packages/Light.DataAccessMocks/)

# How to install

Light.DataAccessMocks is compiled against [.NET Standard 2.0 and 2.1](https://docs.microsoft.com/en-us/dotnet/standard/net-standard) and thus supports all major platforms like .NET and .NET Framework, Mono, Xamarin, UWP, or Unity.

Light.DataAccessMocks is available as a [NuGet package](https://www.nuget.org/packages/Light.DataAccessMocks/) and can be installed via:

- **Package Reference in csproj**: `<PackageReference Include="Light.DataAccessMocks" Version="3.0.0" />`
- **dotnet CLI**: `dotnet add package Light.DataAccessMocks`
- **Visual Studio Package Manager Console**: `Install-Package Light.DataAccessMocks`

# What does Light.DataAccessMocks offer you?

With Light.DataAccessMocks, you can easily create mock clients and sessions for the data access abstractions of [Light.SharedCore](https://github.com/feO2x/Light.SharedCore). This library provides base classes that allow you to easily check that a session was correctly disposed, that changes were saved, or that a session was never opened.

## Mocking clients

Clients or read-only sessions are those humble objects that do not explicitly interact with transactions or change tracking, typically to  only read data from your source system. Calling code usually simply disposes a client when the connection to the third-party system is no longer needed. You can derive from the `AsyncDisposableMock` or `DisposableMock` to mock the `IAsyncDisposable` or `IDisposable` interfaces. The following example shows this for an asynchronous use case:

```csharp
public interface IGetContactSession : IAsyncDisposable
{
    Task<Contact?> GetContactAsync(int id);
}
```

Consider the following ASP.NET Core controller that uses this session:

```csharp
[ApiController]
[Route("api/contacts")]
public sealed class GetContactController : ControllerBase
{
    private readonly IAsyncFactory<IGetContactSession> _sessionFactory;

    public GetContactController(IAsyncFactory<IGetContactSession> sessionFactory) =>
        _sessionFactory = sessionFactory;

    [HttpGet("{id}")]
    public async Task<ActionResult<Contact>> GetContact(int id)
    {
        if (id < 1)
        {
            ModelState.AddModelError("id", "The id must at least be 1");
            return ValidationProblem();
        }

        await using var session = await _sessionFactory.CreateAsync();
        var contact = await session.GetContactAsync(id);
        if (contact == null)
        {
            return NotFound();
        }

        return contact;
    }
}
```

You could then test your controller with the following code in xunit:

```csharp
public sealed class GetContactControllerTests
{
    private readonly GetContactSessionMock _session;
    private readonly GetContactController _controller;

    public GetContactControllerTests()
    {
        _session = new GetContactSessionMock();
        var sessionFactory = new AsyncFactoryMock<IGetContactSession>(_session);
        _controller = new GetContactController(sessionFactory);
    }

    [Fact]
    public async Task MustReturnContactWhenIdIsValid()
    {
        _session.Contact = new Contact();

        var result = await _controller.GetContact(42);

        Assert.Equal(_session.Contact, result.Value);
        _session.MustBeDisposed(); // Use this to check if your controller properly disposed the session
    }

    [Fact]
    public async Task MustReturnNotFoundWhenIdIsNotExisting()
    {
        var result = await _controller.GetContact(13);

        Assert.IsType<NotFoundResult>(result.Result);
        _session.MustBeDisposed();
    }

    // AsyncReadOnlySessionMock automatically implements IAsyncDisposable for you
    private sealed class GetContactSessionMock : AsyncDisposableMock, IGetContactSession
    {
        public Contact? Contact { get; set; }

        public Task<Contact?> GetContactAsync(int id) => Task.FromResult(Contact);
    }


}

public sealed class AsyncFactoryMock<T> : IAsyncFactory<T>
        where T : IAsyncDisposable
{
    private readonly T _session;

    public AsyncFactoryMock(T session) => _session = session;

    public Task<T> CreateAsync() => Task.FromResult(_session);
}
```

In the above unit tests, the `GetContactSessionMock` derives from `AsyncDisposableMock` which automatically implements `IAsyncDisposable` and tracks proper disposal of the session. You can use the `MustBeDisposed` method to check that the controller properly closed the session.

## Mocking sessions

If your session manipulates data and thus implements `ISession` for transactional support, you can derive your mocks from the `SessionMock` base class. The following example for updating an existing contact shows this in action:

```csharp
public interface IUpdateContactSession : ISession
{
    Task<Contact?> GetContactAsync(int id);
}
```

The controller that uses this session might look like this:

```csharp
[ApiController]
[Route("api/contacts/update")]
public sealed class UpdateContactController : ControllerBase
{
    private readonly IAsyncFactory<IUpdateContactSession> _sessionFactory;
    private readonly UpdateContactDtoValidator _validator;

    public UpdateContactController(IAsyncFactory<IUpdateContactSession> sessionFactory,
                                   UpdateContactDtoValidator validator)
    {
        _sessionFactory = sessionFactory;
        _validator = validator;
    }

    [HttpPut]
    public async Task<IActionResult> UpdateContact(UpdateContactDto dto)
    {
        if (this.CheckForErrors(dto, _validator, out var badResult))
        {
            return badResult;
        }

        await using var session = await _sessionFactory.CreateAsync();
        var contact = await session.GetContactAsync(dto.ContactId);
        if (contact == null)
        {
            return NotFound();
        }

        dto.UpdateContact(contact);
        await session.SaveChangesAsync();
        return NoContent();
    }
}
```

To test this controller, we might write the following unit tests in xunit:

```csharp
public sealed class UpdateContactControllerTests
{
    private readonly UpdateContactSessionMock _session;
    private readonly UpdateContactController _controller;

    public UpdateContactControllerTests()
    {
        _session = new UpdateContactSessionMock();
        var sessionFactory = new AsyncFactoryMock<IUpdateContactSession>(_session);
        _controller = new UpdateContactController(sessionFactory, new UpdateContactDtoValidator());
    }

    [Fact]
    public async Task UpdateEntityWhenIdIsValid()
    {
        var contact = new Contact { Id = 1, Name = "John Doe" };
        _session.Contact = contact;
        var dto = new UpdateContactDto(1, "Jane Doe");

        var result = await _controller.UpdateContact(dto);

        Assert.Equal("Jane Doe", contact.Name);
        Assert.IsType<NoContentResult>(result.Result);
        _session.SaveChangesMustHaveBeenCalled() // Use this method to ensure SaveChangesAsync was called
                .MustBeDisposed();
    }

    [Fact]
    public async Task RefuseUpdateWhenIdIsNonExisting()
    {
        var dto = new UpdateContactDto(42, "Buzz Greenfield");

        var result = await _controller.UpdateContact(dto);

        Assert.IsType<NotFoundResult>(result.Result);
        _session.SaveChangesMustNotHaveBeenCalled() // Use this method to ensure that SaveChangesAsync was NOT called
                .MustBeDisposed();
    }

    private sealed class UpdateContactSessionMock : SessionMock, IUpdateContactSession
    {
        public Contact? Contact { get; set; }

        public Task<Contact?> GetContactAsync(int id) => Task.FromResult(Contact);
    }
}
```

In the above unit test, `UpdateContactSessionMock` derives from `SessionMock` which implements `ISession` and tracks calls to `SaveChangesAsync` and `DiposeAsync`. The methods `SaveChangesMustHaveBeenCalled` and `SaveChangesMustNotHaveBeenCalled` are used to ensure that `SaveChangesAsync` is properly called by the `UpdateContactController`.

By the way, you can throw an arbitrary exception during `SaveChanges` by setting the `ExceptionOnSaveChanges` property.
