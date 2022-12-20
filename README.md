# Light.DataAccessMocks

Provides mocks for the data access abstractions of [Light.SharedCore](https://github.com/feO2x/Light.SharedCore) that you can use in your unit tests.

![Light Logo](light-logo.png)

[![License](https://img.shields.io/badge/License-MIT-green.svg?style=for-the-badge)](https://github.com/feO2x/Light.DataAccessMocks/blob/main/LICENSE)
[![NuGet](https://img.shields.io/badge/NuGet-1.0.0-blue.svg?style=for-the-badge)](https://www.nuget.org/packages/Light.DataAccessMocks/)

# How to install

Light.DataAccessMocks is compiled against [.NET Standard 2.0 and 2.1](https://docs.microsoft.com/en-us/dotnet/standard/net-standard) and thus supports all major platforms like .NET and .NET Framework, Mono, Xamarin, UWP, or Unity.

Light.DataAccessMocks is available as a [NuGet package](https://www.nuget.org/packages/Light.DataAccessMocks/) and can be installed via:

- **Package Reference in csproj**: `<PackageReference Include="Light.DataAccessMocks" Version="1.0.0" />`
- **dotnet CLI**: `dotnet add package Light.DataAccessMocks`
- **Visual Studio Package Manager Console**: `Install-Package Light.DataAccessMocks`

# What does Light.DataAccessMocks offer you?

With Light.DataAccessMocks, you can easily create mock sessions for the data access abstractions of [Light.SharedCore](https://github.com/feO2x/Light.SharedCore). This library provides base classes that allow you to easily check that a session was correctly disposed, that changes were saved, that transactions were committed, or that a session was never opened.

## Mocking read-only sessions

Read-only sessions are those sessions that only read data from your source system, usually not requiring a transaction. You can derive from the `AsyncReadOnlySessionMock` or `ReadOnlySessionMock` to mock the `IAsyncReadOnlySession` or `IReadOnlySession` interfaces. The following example shows this for an asynchronous use case:

```csharp
public interface IGetContactSession : IAsyncReadOnlySession
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
    public GetContactController(IAsyncFactory<IGetContactSession> sessionFactory) =>
        SessionFactory = sessionFactory;

    private IAsyncFactory<IGetContactSession> SessionFactory { get; }

    [HttpGet("{id}")]
    public async Task<ActionResult<Contact>> GetContact(int id)
    {
        if (id < 1)
        {
            ModelState.AddModelError("id", "The id must at least be 1");
            return ValidationProblem();
        }

        await using var session = await SessionFactory.CreateAsync();
        var contact = await session.GetContactAsync(id);
        if (contact == null)
            return NotFound();
        return contact;
    }
}
```

You could then test your controller with the following code in xunit:

```csharp
public sealed class GetContactControllerTests
{
    public GetContactControllerTests()
    {
        Session = new GetContactSessionMock();
        SessionFactory = new AsyncFactoryMock<IGetContactSession>(Session);
        Controller = new GetContactController(SessionFactory);
    }

    private GetContactSessionMock Session { get; }
    private AsyncFactoryMock<IGetContactSession> SessionFactory { get; set; }
    private GetContactController Controller { get; }

    [Fact]
    public async Task MustReturnContactWhenIdIsValid()
    {
        Session.Contact = new Contact();

        var result = await Controller.GetContact(42);

        Assert.Equal(Session.Contact, result.Value);
        Session.MustBeDisposed(); // Use this to check if your controller properly disposed the session
    }

    [Fact]
    public async Task MustReturnNotFoundWhenIdIsNotExisting()
    {
        var result = await Controller.GetContact(13);

        Assert.IsType<NotFoundResult>(result.Result);
        Session.MustBeDisposed();
    }

    // AsyncReadOnlySessionMock automatically implements IAsyncReadOnlySession for you
    private sealed class GetContactSessionMock : AsyncReadOnlySessionMock, IGetContactSession
    {
        public Contact? Contact { get; set; }

        public Task<Contact?> GetContactAsync(int id) => Task.FromResult(Contact);
    }
}
```

In the above unit tests, the `GetContactSessionMock` derives from `AsyncReadOnlySessionMock` which automatically implements `IAsyncReadOnlySession` and tracks proper disposal of the session. You can use the `MustBeDisposed` method to check that the controller properly closed the session.

## Mocking sessions

If your session manipulates data and thus implements `IAsyncSession` or `ISession` for transactional support, you can derive your mocks from the `AsyncSessionMock` or `SessionMock` base classes. The following example for updating an existing contact shows an asynchronous use case:

```csharp
public interface IUpdateContactSession : IAsyncSession
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
    public UpdateContactController(IAsyncFactory<IUpdateContactSession> sessionFactory,
                                   UpdateContactDtoValidator validator)
    {
        SessionFactory = sessionFactory;
        Validator = validator;
    }

    private IAsyncFactory<IUpdateContactSession> SessionFactory { get; }
    private UpdateContactDtoValidator Validator { get; }

    [HttpPut]
    public async Task<IActionResult> UpdateContact(UpdateContactDto dto)
    {
        if (this.CheckForErrors(dto, Validator, out var badResult))
            return badResult;
        
        await using var session = await SessionFactory.CreateAsync();
        var contact = await session.GetContactAsync(dto.ContactId);
        if (contact == null)
            return NotFound();
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
    public UpdateContactControllerTests()
    {
        Session = new UpdateContactSessionMock();
        SessionFactory = new AsyncFactoryMock<IUpdateContactSession>(Session);
        Controller = new UpdateContactController(SessionFactory, new UpdateContactDtoValidator());
    }

    private UpdateContactSessionMock Session { get; }
    private AsyncFactoryMock<IUpdateContactSession> SessionFactory { get; }
    private UpdateContactController Controller { get; }

    [Fact]
    public async Task UpdateEntityWhenIdIsValid()
    {
        var contact = new Contact { Id = 1, Name = "John Doe" };
        Session.Contact = contact;
        var dto = new UpdateContactDto(1, "Jane Doe");

        var result = await Controller.UpdateContact(dto);

        Assert.Equal("Jane Doe", contact.Name);
        Assert.IsType<NoContentResult>(result.Result);
        Session.SaveChangesMustHaveBeenCalled() // Use this method to ensure SaveChangesAsync was called
               .MustBeDisposed();
    }

    [Fact]
    public async Task RefuseUpdateWhenIdIsNonExisting()
    {
        var dto = new UpdateContactDto(42, "Buzz Greenfield");

        var result = await Controller.UpdateContact(dto);

        Assert.IsType<NotFoundResult>(result.Result);
        Session.SaveChangesMustNotHaveBeenCalled() // Use this method to ensure that SaveChangesAsync was NOT called
               .MustBeDisposed();
    }

    private sealed class UpdateContactSessionMock : AsyncSessionMock, IUpdateContactSession
    {
        public Contact? Contact { get; set; }

        public Task<Contact?> GetContactAsync(int id) => Task.FromResult(Contact);
    }
}
```

In the above unit test, `UpdateContactSessionMock` derives from `AsyncSessionMock` which implements `IAsyncSession` and tracks calls to `SaveChangesAsync` and `DiposeAsync`. The methods `SaveChangesMustHaveBeenCalled` and `SaveChangesMustNotHaveBeenCalled` are used to ensure that `SaveChangesAsync` is properly called by the `UpdateContactController`.

By the way, you can throw an arbitrary exception during `SaveChanges` by setting the  `ExceptionOnSaveChanges` property.

## Tracking session creation

In the previous examples, you've already seen the use of `AsyncFactoryMock<T>`. This class implements `IAsyncFactory<T>` which allows you to create a session instance and initialize a connection to the target system asynchronously.

If we reuse the example from the previous section, we might write the following test code for DTO validation:

```csharp
public sealed class UpdateContactControllerTests
{
    public UpdateContactControllerTests()
    {
        Session = new UpdateContactSessionMock();
        SessionFactory = new AsyncFactoryMock<IUpdateContactSession>(Session);
        Controller = new UpdateContactController(SessionFactory, new UpdateContactDtoValidator());
    }

    private UpdateContactSessionMock Session { get; }
    private AsyncFactoryMock<IUpdateContactSession> SessionFactory { get; }
    private UpdateContactController Controller { get; }

    [Fact]
    public async Task InvalidDto()
    {
        var invalidDto = new UpdateContactDto(45, ""); // Name is empty

        var result = await Controller.UpdateContact(invalidDto);

        SessionFactory.CreateMustNotHaveBeenCalled(); // Use this call to ensure that the session was never opened
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateContactWhenIdIsValid()
    {
        var contact = new Contact { Id = 1, Name = "John Doe" };
        Session.Contact = contact
        var dto = new UpdateContactDto(1, "Jane Doe");

        var result = await Controller.UpdateContact(dto);

        SessionFactory.CreateMustHaveBeenCalled(); // Use this call to ensure that the session was opened exactly once
        Assert.Equal("Jane Doe", contact.Name);
        Assert.IsType<NoContentResult>(result.Result);
        Assert.Same(contact, Session.CapturedContact);
        Session.SaveChangesMustHaveBeenCalled()
               .MustBeDisposed();
    }

    private sealed class UpdateContactSessionMock : AsyncSessionMock, IUpdateContactSession
    {
        public Contact? Contact { get; set; }
        public Contact? CapturedContact { get; set; }

        public Task<Contact?> GetContactAsync(int id) => Task.FromResult(Contact);

        public Task UpdateContactAsync(Contact contact)
        {
            CapturedContact = contact;
            return Task.CompletedTask;
        }
    }
}
```

In the above unit tests, the `AsyncFactoryMock<IUpdateContactSession>` is injected into the controller to track calls to `CreateAsync`. When this method is called, the session is returned by the factory mock. You can use the `CreateMustNotHaveBeenCalled` method to ensure that the session was never opened, or the `CreateMustHaveBeenCalled` method to ensure that the session was opened exactly once.

## Mocking transactional sessions

If you want to handle individual transactions in your code, you usually derive from the `IAsyncTransactionalSession` or `ITransactionalSession` interfaces.

```csharp
public interface IUpdateProductsSession : IAsyncTransactionalSession
{
    Task<int> GetProductsCountAsync();

    Task<List<Product>> GetProductBatchAsync(int skip, int take);

    Task UpdateProductAsync(Product product);
}
```

This session might be used in a nightly job that update products for the next day:

```csharp
public sealed class UpdateAllProductsJob
{
    public UpdateAllProductsJob(IAsyncFactory<IUpdateProductsSession> sessionFactory, ILogger logger)
    {
        SessionFactory = sessionFactory;
        Logger = logger;
    }
    
    private IAsyncFactory<IUpdateProductsSession> SessionFactory { get; }
    private ILogger Logger { get; }

    public async Task UpdateProductsAsync()
    {
        await using var session = await SessionFactory.CreateAsync();
        var numberOfProducts = await session.GetProductsCountAsync();
        const int batchSize = 100;
        var skip = 0;
        while (skip < numberOfProducts)
        {
            IAsyncTransaction? transaction = null;
            try
            {
                transaction = session.BeginTransactionAsync();
                var products = session.GetProductBatchAsync(skip, batchSize);
                foreach (var product in products)
                {
                    if (product.TryPerformDailyUpdate(Logger))
                        await session.UpdateProductAsync(product);
                }

                await transaction.CommitAsync();
            }
            catch (Exception exception)
            {
                Logger.Error(exception, "Batch {From} to {To} could not be updated properly", skip + 1, batchSize + skip);
            }
            finally
            {
                if (transaction != null)
                    await transaction.DisposeAsync();
            }

            skip += batchSize;
        }
    }
}
```

You can mock the session using the `AsyncTransactionalSessionMock` (or `TransactionalSessionMock` for synchronous scenarios). It implements the `IAsyncTransactionalSession` interface (or the `ITransactionalSession` interface, respectively) for you and tracks the transactions that are created and used:

```csharp
public sealed class UpdateAllProductsJobTests
{
    public UpdateAllProductsJobTests(ITestOutputHelper output)
    {
        var logger = output.CreateTestLogger(); // This uses Serilog.Sinks.Xunit
        Session = new UpdateProductsSessionMock();
        var sessionFactory = new AsyncFactoryMock<IUpdateProductsSession>(Session);
        Job = new UpdateAllProductsJob(sessionFactory, logger);
    }

    private UpdateProductsSessionMock Session { get; }
    private UpdateAllProductsJob Job { get; }

    [Fact]
    public async Task AllTransactionsMustBeCommitted()
    {
        await Job.UpdateProductsAsync();

        Assert.Equal(5, Session.Transactions.Count);
        Session.AllTransactionsMustBeCommitted() // Use this method to ensure that all tracked transactions were committed
               .MustBeDisposed();
    }

    // Further tests are omitted

    private sealed class UpdateProductsSessionMock : AsyncTransactionalSessionMock, IUpdateProductsSession
    {
        public List<Product> Products { get; } = Generate500Products();

        public List<Product> UpdatedProducts { get; } = new ();

        public Task<int> GetProductsCountAsync() => Task.FromResult(Products.Count);

        public Task<List<Product>> GetProductBatchAsync(int skip, int take) =>
            Task.FromResult(
                Products.Skip(skip)
                        .Take(take)
                        .ToList()
            );

        public Task UpdateProductAsync(Product product)
        {
            UpdatedProducts.Add(product);
            return Task.CompletedTask;
        }

        private List<Product> Generate500Products() => /* Implementation is omitted for brevity's sake */;
    }
}
```

In the above unit test, the session is mocked by deriving from `AsyncTransactionalSessionMock`. The session is injected into the constructor of the job object using an async factory mock. Via the `Transactions` property, you can check which transactions were created. The base class also gives you the `AllTransactionsMustBeCommitted` method that checks that each captured transaction was committed exactly once.

The transactional session mocks provide you with these assertion methods:

- `AllTransactionsMustBeCommitted`: checks if all transactions were committed.
- `AllTransactionsExceptTheLastMustBeCommitted`: checks if all transactions are committed, except the last one which must be rolled back. Useful for scenarios where the first failing transaction should stop the whole job.
- `TransactionsWithIndexesMustBeCommitted`: allows you to specify the transactions that should be committed. Simply pass in the indexes of the corresponding transactions. Especially useful when combined with `TransactionsWithIndexesMustBeRolledBack`.
- `AllTransactionsMustBeRolledBack`: checks that no transaction was committed.
- `TransactionsWithIndexesMustBeRolledBack`: checks that the specified transactions were rolled back. Simply pass in the indexes of the corresponding transactions. Especially useful when combined with `TransactionsWithIndexesMustBeCommitted`.
- `MustBeDisposed`: checks if the specified session as well as all tracked transactions were disposed.

Please keep in mind: most ORMs as well as Light.SharedCore do not support nested transactions. This is why `AsyncTransactionalSessionMock` (and `TransactionalSessionMock`) checks that the previous transaction has been disposed before a new transaction is started. You can change this behavior by passing `false` to the `ensurePreviousTransactionIsClosed` constructor parameter.