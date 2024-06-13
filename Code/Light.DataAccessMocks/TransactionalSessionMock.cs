using Light.SharedCore.DatabaseAccessAbstractions;

namespace Light.DataAccessMocks;

/// <summary>
/// Represents a base class for mocks that implements <see cref="ITransactionalSession" />.
/// </summary>
/// <typeparam name="T">
/// The subtype that derives from this class.
/// It is used as the return type of the fluent API.
/// </typeparam>
public abstract class TransactionalSessionMock<T> : BaseTransactionalSessionMock<TransactionMock, T>,
                                                    ITransactionalSession
    where T : TransactionalSessionMock<T>
{
    /// <summary>
    /// Initializes a new instance of <see cref="TransactionalSessionMock{T}" />.
    /// </summary>
    /// <param name="ensurePreviousTransactionIsClosed">
    /// The value indicating whether this mock checks if a previous transaction
    /// was disposed when <see cref="BeginTransaction" /> is called.
    /// The default value is true.
    /// </param>
    protected TransactionalSessionMock(bool ensurePreviousTransactionIsClosed = true)
        : base(ensurePreviousTransactionIsClosed) { }

    /// <summary>
    /// Creates a new <see cref="TransactionMock" /> instance, adds it to the list of
    /// transactions and returns it. If EnsurePreviousTransactionIsClosed is set to true
    /// (which is the default), this mock will also ensure that the previous transaction was disposed beforehand.
    /// </summary>
    public ITransaction BeginTransaction() => CreateTransaction();
}

/// <summary>
/// Represents a base class for mocks that implements <see cref="ITransactionalSession" />.
/// The return type of the fluent APIs is tied to this base class.
/// </summary>
public abstract class TransactionalSessionMock : TransactionalSessionMock<TransactionalSessionMock> { }