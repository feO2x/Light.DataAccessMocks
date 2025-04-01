using System.Threading;
using System.Threading.Tasks;
using Light.SharedCore.DatabaseAccessAbstractions;

namespace Light.DataAccessMocks;

/// <summary>
/// Represents a base class for mocks that implements <see cref="ISession" />.
/// </summary>
/// <typeparam name="T">
/// The subtype that derives from this class.
/// It is used as the return type of the fluent API.
/// </typeparam>
public abstract class SessionMock<T> : BaseSessionMock<T>, ISession
    where T : SessionMock<T>
{
    /// <summary>
    /// Initializes a new instance of <see cref="SessionMock{T}" />
    /// </summary>
    protected SessionMock() : base("SaveChangesAsync") { }

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
/// Represents a base class for mocks that implements <see cref="ISession" />.
/// The return type of the fluent APIs is tied to this base class.
/// </summary>
public abstract class SessionMock : SessionMock<SessionMock>;
