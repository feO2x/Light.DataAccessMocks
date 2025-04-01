using System.Threading;
using System.Threading.Tasks;
using Light.SharedCore.DatabaseAccessAbstractions;

namespace Light.DataAccessMocks;

/// <summary>
/// Represents a base class for mocks that implements <see cref="IAsyncTransactionalSession" />.
/// </summary>
/// <typeparam name="T">
/// The subtype that derives from this class.
/// It is used as the return type of the fluent API.
/// </typeparam>
public abstract class AsyncTransactionalSessionMock<T> : BaseTransactionalSessionMock<AsyncTransactionMock, T>,
                                                         IAsyncTransactionalSession
    where T : AsyncTransactionalSessionMock<T>
{
    /// <summary>
    /// Initializes a new instance of <see cref="AsyncReadOnlySessionMock{T}" />.
    /// </summary>
    /// <param name="ensurePreviousTransactionIsClosed">
    /// The value indicating whether this mock checks if a previous transaction
    /// was disposed when <see cref="BeginTransactionAsync" /> is called.
    /// The default value is true.
    /// </param>
    protected AsyncTransactionalSessionMock(bool ensurePreviousTransactionIsClosed = true)
        : base(ensurePreviousTransactionIsClosed) { }

    /// <summary>
    /// Creates a new <see cref="AsyncTransactionMock" /> instance, adds it to the list of
    /// transactions and returns it. If EnsurePreviousTransactionIsClosed is set to true
    /// (which is the default), this mock will also ensure that the previous transaction was disposed beforehand.
    /// </summary>
    public ValueTask<IAsyncTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        var transaction = CreateTransaction();
        return new ValueTask<IAsyncTransaction>(transaction);
    }
}

/// <summary>
/// Represents a base class for mocks that implements <see cref="IAsyncTransactionalSession" />.
/// The return type of the fluent APIs is tied to this base class.
/// </summary>
public abstract class AsyncTransactionalSessionMock : AsyncTransactionalSessionMock<AsyncTransactionalSessionMock>;