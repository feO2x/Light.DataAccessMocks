using System;

namespace Light.DataAccessMocks;

/// <summary>
/// Represents a base class for mocks that implement <see cref="IDisposable" />
/// </summary>
/// <typeparam name="T">
/// The subtype that derives from this class.
/// It is used as the return type of the fluent API.
/// </typeparam>
public abstract class DisposableMock<T> : IDisposable, IDisposableMock
    where T : DisposableMock<T>
{
    /// <summary>
    /// Increments the <see cref="DisposeCallCount" />.
    /// </summary>
    public void Dispose()
    {
        checked { DisposeCallCount++; }
    }

    /// <summary>
    /// Gets the number of times <see cref="Dispose" /> was called.
    /// </summary>
    public int DisposeCallCount { get; protected set; }

    /// <summary>
    /// Checks if this session was disposed (<see cref="DisposeCallCount" /> must be greater or equal to 1),
    /// or otherwise throws a <see cref="TestException" />.
    /// </summary>
    public virtual T MustBeDisposed()
    {
        if (DisposeCallCount < 1)
        {
            throw new TestException($"\"{GetType().Name}\" was not disposed.");
        }

        return (T) this;
    }
}

/// <summary>
/// Represents a base class for mocks that implement <see cref="IDisposable" />.
/// The return type of the fluent APIs is tied to this base class.
/// </summary>
public abstract class DisposableMock : DisposableMock<DisposableMock>;
