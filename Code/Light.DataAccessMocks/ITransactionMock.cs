namespace Light.DataAccessMocks;

/// <summary>
/// Represents the abstraction of a transaction mock that tracks the call count
/// of Commit calls.
/// </summary>
public interface ITransactionMock : IDisposableMock
{
    /// <summary>
    /// Gets the number of calls to Commit or CommitAsync
    /// </summary>
    public int CommitCallCount { get; }
}