using System;
using System.Collections.Generic;
using Humanizer;
using Light.GuardClauses;
using Light.GuardClauses.Exceptions;

namespace Light.DataAccessMocks;

/// <summary>
/// Represents the base implementation for <see cref="AsyncTransactionalSessionMock{T}" />
/// </summary>
/// <typeparam name="TTransactionMock">The type that is used to mock transactions.</typeparam>
/// <typeparam name="TSubClass">Your subclass that derives from this type.</typeparam>
public abstract class BaseTransactionalSessionMock<TTransactionMock, TSubClass> : AsyncDisposableMock<TSubClass>
    where TTransactionMock : ITransactionMock, new()
    where TSubClass : BaseTransactionalSessionMock<TTransactionMock, TSubClass>
{
    /// <summary>
    /// Initializes a new instance of <see cref="BaseTransactionalSessionMock{TTransactionMock,TSubClass}" />.
    /// </summary>
    /// <param name="ensurePreviousTransactionIsClosed">
    /// The value indicating whether this mock checks if a previous transaction
    /// was disposed when <see cref="CreateTransaction" /> is called.
    /// The default value is true.
    /// </param>
    protected BaseTransactionalSessionMock(bool ensurePreviousTransactionIsClosed = true)
    {
        EnsurePreviousTransactionIsClosed = ensurePreviousTransactionIsClosed;
        Transactions = new List<TTransactionMock>();
    }

    /// <summary>
    /// Gets the transactions that were started.
    /// </summary>
    public List<TTransactionMock> Transactions { get; }

    /// <summary>
    /// Gets the value indicating whether this mock checks if a previous transaction
    /// was disposed when a new transaction is started. The default value is true.
    /// </summary>
    public bool EnsurePreviousTransactionIsClosed { get; }

    /// <summary>
    /// Creates a new transaction mock instance, adds it to the list of
    /// <see cref="Transactions" /> and returns it. If <see cref="EnsurePreviousTransactionIsClosed" /> is
    /// set to true (which is the default), this mock will also ensure that the previous transaction
    /// was disposed beforehand.
    /// </summary>
    protected TTransactionMock CreateTransaction()
    {
        EnsurePreviousTransactionIsClosedIfNecessary();

        var transaction = new TTransactionMock();
        Transactions.Add(transaction);
        return transaction;
    }

    /// <summary>
    /// Checks if all transactions were committed. This method also checks if at least one transaction was started.
    /// </summary>
    public TSubClass AllTransactionsMustBeCommitted() => CheckThatTransactionsAreCommitted(Transactions.Count);

    /// <summary>
    /// Checks if all but the last transactions were committed. This method also checks if at least one transaction was started.
    /// </summary>
    public TSubClass AllTransactionsExceptTheLastMustBeCommitted()
    {
        var lastIndex = Transactions.Count - 1;
        CheckThatTransactionsAreCommitted(lastIndex);
        CheckIfTransactionWasRolledBack(lastIndex);
        return (TSubClass) this;
    }

    /// <summary>
    /// Checks if the specified transactions are committed. This method also checks if at least one transaction was started.
    /// </summary>
    /// <param name="indexes">The indexes of the transactions that should be committed.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="indexes" /> is null.</exception>
    /// <exception cref="EmptyCollectionException">Thrown when <paramref name="indexes" /> is an empty array.</exception>
    /// <exception cref="IndexOutOfRangeException">
    /// Thrown when any of the specified indexes is less than zero or greater or equal to the count of <see cref="Transactions" />.
    /// </exception>
    public TSubClass TransactionsWithIndexesMustBeCommitted(params int[] indexes)
    {
        indexes.MustNotBeNullOrEmpty(nameof(indexes));

        EnsureTransactionsWereStarted();

        foreach (var index in indexes)
        {
            CheckIfIndexIsValid(index);
            CheckIfTransactionWasCommitted(index);
        }

        return (TSubClass) this;
    }

    /// <summary>
    /// Checks if all transactions were rolled-back. This method also checks if at least one transaction was started.
    /// </summary>
    public TSubClass AllTransactionsMustBeRolledBack()
    {
        EnsureTransactionsWereStarted();

        for (var i = 0; i < Transactions.Count; i++)
        {
            CheckIfTransactionWasRolledBack(i);
        }

        return (TSubClass) this;
    }

    /// <summary>
    /// Checks if the specified transactions are rolled back. This method also checks if at least one transaction was started.
    /// </summary>
    /// <param name="indexes">The indexes of the transactions that should be rolled back.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="indexes" /> is null.</exception>
    /// <exception cref="EmptyCollectionException">Thrown when <paramref name="indexes" /> is an empty array.</exception>
    public TSubClass TransactionsWithIndexesMustBeRolledBack(params int[] indexes)
    {
        indexes.MustNotBeNullOrEmpty(nameof(indexes));

        EnsureTransactionsWereStarted();

        foreach (var index in indexes)
        {
            CheckIfIndexIsValid(index);
            CheckIfTransactionWasRolledBack(index);
        }

        return (TSubClass) this;
    }

    private TSubClass CheckThatTransactionsAreCommitted(int upperLimit)
    {
        EnsureTransactionsWereStarted();

        for (var i = 0; i < upperLimit; i++)
        {
            CheckIfTransactionWasCommitted(i);
        }

        return (TSubClass) this;
    }

    private void EnsurePreviousTransactionIsClosedIfNecessary()
    {
        if (!EnsurePreviousTransactionIsClosed || Transactions.Count == 0)
        {
            return;
        }

        var lastIndex = Transactions.Count - 1;
        var lastTransaction = Transactions[lastIndex];
        if (lastTransaction.DisposeCallCount == 0)
        {
            throw new TestException(
                $"The {(lastIndex + 1).Ordinalize()} transaction was not disposed before the {(lastIndex + 2).Ordinalize()} transaction was started."
            );
        }
    }

    private void EnsureTransactionsWereStarted()
    {
        if (Transactions.Count == 0)
        {
            throw new TestException("No transactions were started.");
        }
    }

    private void CheckIfTransactionWasCommitted(int i)
    {
        var transaction = Transactions[i];
        switch (transaction.CommitCallCount)
        {
            case 0:
                throw new TestException($"The {(i + 1).Ordinalize()} transaction was not committed.");
            case > 1:
                throw new TestException(
                    $"The {(i + 1).Ordinalize()} transaction was committed too often ({transaction.CommitCallCount} times)."
                );
        }
    }

    private void CheckIfTransactionWasRolledBack(int i)
    {
        var transaction = Transactions[i];
        if (transaction.CommitCallCount != 0)
        {
            throw new TestException(
                $"The {(i + 1).Ordinalize()} transaction was committed, although it should be rolled back."
            );
        }

        if (transaction.DisposeCallCount == 0)
        {
            throw new TestException($"The {(i + 1).Ordinalize()} transaction was not rolled back.");
        }
    }

    private void CheckIfIndexIsValid(int index)
    {
        if (index < 0 || index >= Transactions.Count)
        {
            throw new IndexOutOfRangeException($"There is no transaction that corresponds to index {index}.");
        }
    }

    /// <summary>
    /// Checks if this session and all transactions that were created with it were disposed.
    /// </summary>
    public override TSubClass MustBeDisposed()
    {
        for (var i = 0; i < Transactions.Count; i++)
        {
            var transaction = Transactions[i];
            if (transaction.DisposeCallCount == 0)
            {
                throw new TestException($"The {(i + 1).Ordinalize()} transaction was not disposed.");
            }
        }

        return base.MustBeDisposed();
    }
}
