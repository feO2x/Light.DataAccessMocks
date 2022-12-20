using System;
using System.Threading;
using System.Threading.Tasks;
using Light.GuardClauses;
using Light.SharedCore.Initialization;

namespace Light.DataAccessMocks;

/// <summary>
/// Represents a mock that implements <see cref="IAsyncFactory{T}" />.
/// </summary>
/// <typeparam name="T">The type of the object to be created.</typeparam>
public sealed class AsyncFactoryMock<T> : IAsyncFactory<T>
{
    /// <summary>
    /// Initializes a new instance of <see cref="AsyncFactoryMock{T}" />.
    /// </summary>
    /// <param name="instance">The instance that will be returned when <see cref="CreateAsync" /> is called.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="instance" /> is null.</exception>
    public AsyncFactoryMock(T instance) => Instance = instance.MustNotBeNullReference();

    /// <summary>
    /// Returns the instance to the caller and increments the <see cref="CreateCallCount" />.
    /// </summary>
    public ValueTask<T> CreateAsync(CancellationToken cancellationToken = default)
    {
        IncrementCreateCallCount();
        return new ValueTask<T>(Instance);
    }
    
    /// <summary>
    /// Gets the instance that will be returned when <see cref="CreateAsync" /> is called.
    /// </summary>
    public T Instance { get; }

    /// <summary>
    /// Gets the number of times <see cref="CreateAsync" /> was called.
    /// </summary>
    public int CreateCallCount { get; private set; }

    /// <summary>
    /// Increments the <see cref="CreateCallCount" /> and protects it from overflows.
    /// </summary>
    private void IncrementCreateCallCount()
    {
        checked { CreateCallCount++; }
    }

    /// <summary>
    /// Checks if <see cref="CreateAsync" /> has never been called, or otherwise throws a <see cref="TestException" />.
    /// </summary>
    public AsyncFactoryMock<T> CreateMustNotHaveBeenCalled()
    {
        if (CreateCallCount != 0)
            throw new TestException($"CreateAsync must not have been called, but it was actually called {CreateCallCount} {(CreateCallCount == 1 ? "time" : "times")}.");
        return this;
    }

    /// <summary>
    /// Checks if <see cref="CreateAsync" /> was called exactly once, or otherwise throws a <see cref="TestException" />.
    /// </summary>
    public AsyncFactoryMock<T> CreateMustHaveBeenCalled()
    {
        if (CreateCallCount == 0)
            throw new TestException("CreateAsync must have been called exactly once, but it was actually never called.");
        if (CreateCallCount > 1)
            throw new TestException($"CreateAsync must have been called exactly once, but it was actually called {CreateCallCount} times.");
        return this;
    }
}