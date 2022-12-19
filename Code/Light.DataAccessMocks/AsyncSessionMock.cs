using System.Threading;
using System.Threading.Tasks;
using Light.SharedCore.DataAccessAbstractions;

namespace Light.DataAccessMocks;

/// <summary>
/// Represents a base class for mocks that implements <see cref="IAsyncSession" />.
/// </summary>
/// <typeparam name="T">
/// The subtype that derives from this class.
/// It is used as the return type of the fluent API.
/// </typeparam>
public abstract class AsyncSessionMock<T> : BaseSessionMock<T>, IAsyncSession
    where T : AsyncSessionMock<T>
{
    /// <summary>
    /// Initializes a new instance of <see cref="AsyncSessionMock{T}" />
    /// </summary>
    protected AsyncSessionMock() : base("SaveChangesAsync") { }

    /// <summary>
    /// Increments the SaveChangesCallCount and potentially throws
    /// an exception if ExceptionOnSaveChanges is not null.
    /// </summary>
    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveChangesInternal();
        return Task.CompletedTask;
    }
}

/// <summary>
/// Represents a base class for mocks that implements <see cref="IAsyncSession" />.
/// The return type of the fluent APIs is tied to this base class.
/// </summary>
public abstract class AsyncSessionMock : AsyncSessionMock<AsyncSessionMock> { }