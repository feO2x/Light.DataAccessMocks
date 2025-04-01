using Light.SharedCore.DatabaseAccessAbstractions;

namespace Light.DataAccessMocks;

/// <summary>
/// Represents a base class for mocks that implements <see cref="IAsyncReadOnlySession" />.
/// </summary>
/// <typeparam name="T">
/// The subtype that derives from this class.
/// It is used as the return type of the fluent API.
/// </typeparam>
public abstract class AsyncReadOnlySessionMock<T> : AsyncDisposableMock<T>, IAsyncReadOnlySession
    where T : AsyncReadOnlySessionMock<T> { }

/// <summary>
/// Represents a base class for mocks that implements <see cref="IAsyncReadOnlySession" />.
/// The return type of the fluent APIs is tied to this base class.
/// </summary>
public abstract class AsyncReadOnlySessionMock : AsyncReadOnlySessionMock<AsyncReadOnlySessionMock>;
