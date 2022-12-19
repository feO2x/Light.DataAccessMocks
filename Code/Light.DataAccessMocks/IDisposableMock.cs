using System;

namespace Light.DataAccessMocks;

/// <summary>
/// Represents the abstraction of a mock that tracks
/// the number of calls to <see cref="IDisposable.Dispose" />
/// or <see cref="IAsyncDisposable.DisposeAsync" />.
/// </summary>
public interface IDisposableMock
{
    /// <summary>
    /// Gets the number of calls to Dispose or DisposeAsync.
    /// </summary>
    public int DisposeCallCount { get; }
}