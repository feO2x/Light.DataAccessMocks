using System.Threading;
using System.Threading.Tasks;
using Light.SharedCore.DataAccessAbstractions;

namespace Light.DataAccessMocks;

/// <summary>
/// Represents a mock that implements <see cref="IAsyncTransaction" />.
/// </summary>
public sealed class AsyncTransactionMock : AsyncDisposableMock<AsyncTransactionMock>, IAsyncTransaction, ITransactionMock
{
    /// <summary>
    /// Gets the number of times <see cref="CommitAsync" /> was called.
    /// </summary>
    public int CommitCallCount { get; private set; }

    /// <summary>
    /// Increments the <see cref="CommitCallCount" />.
    /// </summary>
    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        checked { CommitCallCount++; }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Checks if the transaction was committed exactly once, or otherwise
    /// throws a <see cref="TestException" />.
    /// </summary>
    public AsyncTransactionMock MustBeCommitted()
    {
        if (CommitCallCount != 1)
            throw new TestException($"CommitAsync must have been called exactly once, but it was called {CommitCallCount} times.");
        return this;
    }
}