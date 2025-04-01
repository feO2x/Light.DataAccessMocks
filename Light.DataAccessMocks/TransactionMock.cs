using Light.SharedCore.DatabaseAccessAbstractions;

namespace Light.DataAccessMocks;

/// <summary>
/// Represents a mock that implements <see cref="ITransaction" />.
/// </summary>
public sealed class TransactionMock : DisposableMock<TransactionMock>, ITransaction, ITransactionMock
{
    /// <summary>
    /// Increments the <see cref="CommitCallCount" />.
    /// </summary>
    public void Commit()
    {
        unchecked { CommitCallCount++; }
    }

    /// <summary>
    /// Gets the number of times <see cref="Commit" /> was called.
    /// </summary>
    public int CommitCallCount { get; private set; }

    /// <summary>
    /// Checks if the transaction was committed exactly once, or otherwise
    /// throws a <see cref="TestException" />.
    /// </summary>
    public TransactionMock MustBeCommitted()
    {
        if (CommitCallCount != 1)
        {
            throw new TestException(
                $"Commit must have been called exactly once, but it was called {CommitCallCount} times."
            );
        }

        return this;
    }
}