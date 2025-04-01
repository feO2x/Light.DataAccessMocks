using System;

namespace Light.DataAccessMocks;

/// <summary>
/// Represents a base class for mocks that implement <see cref="IDisposable" />.
/// </summary>
/// <typeparam name="T">
/// The subtype that derives from this class.
/// It is used as the return type of the fluent API.
/// </typeparam>
public abstract class ReadOnlySessionMock<T> : DisposableMock<T>
    where T : ReadOnlySessionMock<T>;

/// <summary>
/// Represents a base class for mocks that implement <see cref="IDisposable" />.
/// The return type of the fluent APIs is tied to this base class.
/// </summary>
public abstract class ReadOnlySessionMock : ReadOnlySessionMock<ReadOnlySessionMock> { }