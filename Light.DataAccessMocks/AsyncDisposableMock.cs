using System;
using System.Threading.Tasks;

namespace Light.DataAccessMocks;

/// <summary>
/// Represents a base class for mocks that implement <see cref="IAsyncDisposable" /> and <see cref="IDisposable" />.
/// </summary>
/// <typeparam name="T">
/// The subtype that derives from this class.
/// It is used as the return type of the fluent API.
/// </typeparam>
public abstract class AsyncDisposableMock<T> : DisposableMock<T>, IAsyncDisposable
    where T : AsyncDisposableMock<T>
{
    /// <summary>
    /// Increments the DisposeCallCount.
    /// </summary>
    public ValueTask DisposeAsync()
    {
        checked
        {
            DisposeCallCount++;
        }

        return default;
    }
}

/// <summary>
/// Represents a base class for mocks that implement <see cref="IAsyncDisposable" /> and <see cref="IDisposable" />.
/// The return type of the fluent APIs is tied to this base class.
/// </summary>
public abstract class AsyncDisposableMock : AsyncDisposableMock<AsyncDisposableMock> { }