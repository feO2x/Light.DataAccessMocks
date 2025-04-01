using System;
using System.Linq;
using FluentAssertions;
using Humanizer;
using Light.GuardClauses.Exceptions;
using Xunit;

namespace Light.DataAccessMocks.Tests;

public static class BaseTransactionalSessionMockTests
{
    [Fact]
    public static void MustDeriveFromAsyncDisposableMock() =>
        typeof(BaseTransactionalSessionMock<,>).Should().BeDerivedFrom(typeof(AsyncDisposableMock<>));

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public static void ExceptionWhenAPreviousTransactionWasNotDisposedOnBeginTransaction(int indexOfInvalidTransaction)
    {
        var session = new TransactionalSession();

        var act = () =>
        {
            for (var i = 0; i < 5; i++)
            {
                var transaction = session.BeginTransaction();
                transaction.Commit();
                if (i != indexOfInvalidTransaction)
                {
                    transaction.Dispose();
                }
            }
        };

        act.Should().Throw<TestException>()
           .And.Message.Should().Be(
                $"The {(indexOfInvalidTransaction + 1).Ordinalize()} transaction was not disposed before the {(indexOfInvalidTransaction + 2).Ordinalize()} transaction was started."
            );
    }

    private static void CommitMayBeTooOften(
        this TransactionMock transaction,
        int currentIndex,
        int indexOfInvalidTransaction,
        ref int numberOfCommits
    )
    {
        transaction.Commit();
        if (currentIndex != indexOfInvalidTransaction)
        {
            return;
        }

        var random = new Random(); // You may want to provide a seed here for debugging purposes
        numberOfCommits = random.Next(1, 10);
        for (var i = 0; i < numberOfCommits; i++)
        {
            transaction.Commit();
        }

        numberOfCommits++;
    }

    private static void ShouldThrowTransactionWasNotCommitted(this Action act, int indexOfInvalidTransaction) =>
        act.Should().Throw<TestException>()
           .And.Message.Should().Be(
                $"The {(indexOfInvalidTransaction + 1).Ordinalize()} transaction was not committed."
            );

    private static void ShouldThrowTransactionWasCommittedTooOften(
        this Action act,
        int indexOfInvalidTransaction,
        int numberOfCommits
    ) =>
        act.Should().Throw<TestException>()
           .And.Message.Should().Be(
                $"The {(indexOfInvalidTransaction + 1).Ordinalize()} transaction was committed too often ({numberOfCommits} times)."
            );

    private static void ShouldThrowTransactionWasNotRolledBack(this Action act, int indexOfInvalidTransaction) =>
        act.Should().Throw<TestException>()
           .And.Message.Should().Be(
                $"The {(indexOfInvalidTransaction + 1).Ordinalize()} transaction was committed, although it should be rolled back."
            );

    private static void ShouldThrowNoTransactionsStartedException(this Action act) =>
        act.Should().Throw<TestException>()
           .And.Message.Should().Be("No transactions were started.");

    public static class WhenMustBeDisposedIsCalled
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(4)]
        public static void ExceptionWhenAnyTransactionIsNotDisposed(int indexOfInvalidTransaction)
        {
            var session = new TransactionalSession(false);
            for (var i = 0; i < 5; i++)
            {
                var transaction = session.BeginTransaction();
                if (i != indexOfInvalidTransaction)
                {
                    transaction.Dispose();
                }
            }

            session.Dispose();

            Action act = () => session.MustBeDisposed();

            act.Should().Throw<TestException>()
               .And.Message.Should().Be(
                    $"The {(indexOfInvalidTransaction + 1).Ordinalize()} transaction was not disposed."
                );
        }

        [Theory]
        [InlineData(1)]
        [InlineData(6)]
        [InlineData(9)]
        public static void NoExceptionWhenAllTransactionsAreDisposed(int numberOfTransactions)
        {
            var session = new TransactionalSession(false);
            for (var i = 0; i < numberOfTransactions; i++)
            {
                var transaction = session.BeginTransaction();
                transaction.Dispose();
            }

            session.Dispose();

            session.MustBeDisposed().Should().BeSameAs(session);
        }

        [Fact]
        public static void ExceptionWhenSessionItselfIsNotDisposed()
        {
            var session = new TransactionalSession();

            Action act = () => session.MustBeDisposed();

            act.Should().Throw<TestException>()
               .And.Message.Should().Be("\"TransactionalSession\" was not disposed.");
        }
    }

    public static class WhenAllTransactionMustBeCommittedIsCalled
    {
        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(12)]
        public static void NoExceptionWhenAllTransactionsAreCommitted(int numberOfTransactions)
        {
            var session = new TransactionalSession();

            for (var i = 0; i < numberOfTransactions; i++)
            {
                var transaction = session.BeginTransaction();
                session.Transactions[i].Should().BeSameAs(transaction);
                transaction.Commit();
                transaction.Dispose();
            }

            session.AllTransactionsMustBeCommitted().Should().BeSameAs(session);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public static void ExceptionWhenAnyTransactionIsNotCommitted(int indexOfInvalidTransaction)
        {
            var session = new TransactionalSession();

            for (var i = 0; i < 3; i++)
            {
                var transaction = session.BeginTransaction();
                if (i != indexOfInvalidTransaction)
                {
                    transaction.Commit();
                }

                transaction.Dispose();
            }

            Action act = () => session.AllTransactionsMustBeCommitted();

            act.ShouldThrowTransactionWasNotCommitted(indexOfInvalidTransaction);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(2)]
        [InlineData(4)]
        public static void ExceptionWhenAnyTransactionIsCommittedMoreThanOnce(int indexOfInvalidTransaction)
        {
            var session = new TransactionalSession();
            var numberOfCommits = 0;
            for (var i = 0; i < 5; i++)
            {
                var transaction = session.BeginTransaction();
                transaction.CommitMayBeTooOften(i, indexOfInvalidTransaction, ref numberOfCommits);
                transaction.Dispose();
            }

            Action act = () => session.AllTransactionsMustBeCommitted();

            act.ShouldThrowTransactionWasCommittedTooOften(indexOfInvalidTransaction, numberOfCommits);
        }

        [Fact]
        public static void EnsureThatThereIsAtLeastOneTransaction()
        {
            var session = new TransactionalSession();

            Action act = () => session.AllTransactionsMustBeCommitted();

            act.ShouldThrowNoTransactionsStartedException();
        }
    }

    public static class WhenAllTransactionsExceptTheLastMustBeCommittedIsCalled
    {
        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(7)]
        public static void ExceptionWhenAllTransactionsAreCommitted(int numberOfTransactions)
        {
            var session = new TransactionalSession();

            for (var i = 0; i < numberOfTransactions; i++)
            {
                var transaction = session.BeginTransaction();
                transaction.Commit();
                transaction.Dispose();
            }

            Action act = () => session.AllTransactionsExceptTheLastMustBeCommitted();

            act.ShouldThrowTransactionWasNotRolledBack(numberOfTransactions - 1);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(21)]
        public static void NoExceptionWhenAllButTheLastTransactionAreCommitted(int numberOfTransactions)
        {
            var session = new TransactionalSession();

            var lastIndex = numberOfTransactions - 1;
            for (var i = 0; i < numberOfTransactions; i++)
            {
                var transaction = session.BeginTransaction();
                if (i != lastIndex)
                {
                    transaction.Commit();
                }

                transaction.Dispose();
            }

            session.AllTransactionsExceptTheLastMustBeCommitted().Should().BeSameAs(session);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public static void ExceptionWhenAnyOfTheFirstTransactionsIsNotCommitted(int indexOfInvalidTransaction)
        {
            var session = new TransactionalSession();

            for (var i = 0; i < 5; i++)
            {
                var transaction = session.BeginTransaction();
                if (i != indexOfInvalidTransaction)
                {
                    transaction.Commit();
                }

                transaction.Dispose();
            }

            Action act = () => session.AllTransactionsExceptTheLastMustBeCommitted();

            act.ShouldThrowTransactionWasNotCommitted(indexOfInvalidTransaction);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(2)]
        [InlineData(3)]
        public static void ExceptionWhenAnyTransactionIsCommittedSeveralTimes(int indexOfInvalidTransaction)
        {
            var session = new TransactionalSession();

            var numberOfCommits = 0;
            for (var i = 0; i < 5; i++)
            {
                var transaction = session.BeginTransaction();
                if (i < 4)
                {
                    transaction.CommitMayBeTooOften(i, indexOfInvalidTransaction, ref numberOfCommits);
                }

                transaction.Dispose();
            }

            Action act = () => session.AllTransactionsExceptTheLastMustBeCommitted();

            act.ShouldThrowTransactionWasCommittedTooOften(indexOfInvalidTransaction, numberOfCommits);
        }

        [Fact]
        public static void EnsureThatThereIsAtLeastOneTransaction()
        {
            var session = new TransactionalSession();

            Action act = () => session.AllTransactionsExceptTheLastMustBeCommitted();

            act.ShouldThrowNoTransactionsStartedException();
        }
    }

    public static class WhenTransactionsWithIndexesMustBeCommittedIsCalled
    {
        [Theory]
        [InlineData(new[] { 0 })]
        [InlineData(new[] { 1, 3 })]
        [InlineData(new[] { 2, 3, 4 })]
        public static void NoExceptionWhenAllSpecifiedTransactionsAreCommitted(int[] indexesOfCommittedTransactions)
        {
            var session = new TransactionalSession();

            for (var i = 0; i < 5; i++)
            {
                var transaction = session.BeginTransaction();
                if (indexesOfCommittedTransactions.Contains(i))
                {
                    transaction.Commit();
                }

                transaction.Dispose();
            }

            session.TransactionsWithIndexesMustBeCommitted(indexesOfCommittedTransactions)
               .Should().BeSameAs(session);
        }

        [Fact]
        public static void ExceptionWhenAnEmptyArrayIsPassed()
        {
            var session = new TransactionalSession();

            Action act = () => session.TransactionsWithIndexesMustBeCommitted();

            act.Should().Throw<EmptyCollectionException>()
               .And.ParamName.Should().Be("indexes");
        }

        [Fact]
        public static void ExceptionWhenNullIsPassedForIndexes()
        {
            var session = new TransactionalSession();

            Action act = () => session.TransactionsWithIndexesMustBeCommitted(null!);

            act.Should().Throw<ArgumentNullException>()
               .And.ParamName.Should().Be("indexes");
        }

        [Fact]
        public static void ExceptionWhenNotAllTransactionsAreCommitted()
        {
            var session = new TransactionalSession();

            for (var i = 0; i < 5; i++)
            {
                var transaction = session.BeginTransaction();
                if (i is 0 or 3 or 4)
                {
                    transaction.Commit();
                }

                transaction.Dispose();
            }

            Action act = () => session.TransactionsWithIndexesMustBeCommitted(0, 2, 3, 4);

            act.ShouldThrowTransactionWasNotCommitted(2);
        }

        [Fact]
        public static void EnsureThatThereIsAtLeastOneTransaction()
        {
            var session = new TransactionalSession();

            Action act = () => session.TransactionsWithIndexesMustBeCommitted(42);

            act.ShouldThrowNoTransactionsStartedException();
        }
    }

    public static class WhenAllTransactionsMustBeRolledBackIsCalled
    {
        [Theory]
        [InlineData(1)]
        [InlineData(7)]
        [InlineData(13)]
        public static void NoExceptionWhenNoTransactionWasCommitted(int numberOfTransactions)
        {
            var session = new TransactionalSession();

            for (var i = 0; i < numberOfTransactions; i++)
            {
                var transaction = session.BeginTransaction();
                transaction.Dispose();
            }

            session.AllTransactionsMustBeRolledBack().Should().BeSameAs(session);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(2)]
        [InlineData(4)]
        public static void ExceptionWhenAnyTransactionWasCommitted(int indexOfInvalidTransaction)
        {
            var session = new TransactionalSession();

            for (var i = 0; i < 5; i++)
            {
                var transaction = session.BeginTransaction();
                if (i == indexOfInvalidTransaction)
                {
                    transaction.Commit();
                }

                transaction.Dispose();
            }

            Action act = () => session.AllTransactionsMustBeRolledBack();

            act.ShouldThrowTransactionWasNotRolledBack(indexOfInvalidTransaction);
        }

        [Fact]
        public static void EnsureThatThereIsAtLeastOneTransaction()
        {
            var session = new TransactionalSession();

            Action act = () => session.AllTransactionsMustBeRolledBack();

            act.ShouldThrowNoTransactionsStartedException();
        }
    }

    public static class WhenTransactionsWithIndexesMustBeRolledBackIsCalled
    {
        [Theory]
        [InlineData(new[] { 0 })]
        [InlineData(new[] { 1, 2, 4 })]
        [InlineData(new[] { 0, 3 })]
        public static void NoExceptionWhenAllSpecifiedTransactionsAreRolledBack(int[] indexesOfRolledBackTransactions)
        {
            var session = new TransactionalSession();

            for (var i = 0; i < 5; i++)
            {
                var transaction = session.BeginTransaction();
                if (!indexesOfRolledBackTransactions.Contains(i))
                {
                    transaction.Commit();
                }

                transaction.Dispose();
            }

            session.TransactionsWithIndexesMustBeRolledBack(indexesOfRolledBackTransactions)
               .Should().BeSameAs(session);
        }

        [Fact]
        public static void ExceptionWhenAnyOfTheSpecifiedTransactionsIsCommitted()
        {
            var session = new TransactionalSession();

            for (var i = 0; i < 5; i++)
            {
                var transaction = session.BeginTransaction();
                if (i is 0 or 3)
                {
                    transaction.Commit();
                }

                transaction.Dispose();
            }

            Action act = () => session.TransactionsWithIndexesMustBeRolledBack(1, 3, 4);

            act.ShouldThrowTransactionWasNotRolledBack(3);
        }

        [Fact]
        public static void ExceptionWhenAnEmptyArrayIsPassed()
        {
            var session = new TransactionalSession();

            Action act = () => session.TransactionsWithIndexesMustBeRolledBack();

            act.Should().Throw<EmptyCollectionException>()
               .And.ParamName.Should().Be("indexes");
        }

        [Fact]
        public static void ExceptionWhenNullIsPassedForIndexes()
        {
            var session = new TransactionalSession();

            Action act = () => session.TransactionsWithIndexesMustBeRolledBack(null!);

            act.Should().Throw<ArgumentNullException>()
               .And.ParamName.Should().Be("indexes");
        }

        [Fact]
        public static void EnsureThatThereIsAtLeastOneTransaction()
        {
            var session = new TransactionalSession();

            Action act = () => session.TransactionsWithIndexesMustBeRolledBack(42);

            act.ShouldThrowNoTransactionsStartedException();
        }
    }

    private sealed class TransactionalSession : BaseTransactionalSessionMock<TransactionMock, TransactionalSession>
    {
        public TransactionalSession(bool ensurePreviousTransactionIsClosed = true)
            : base(ensurePreviousTransactionIsClosed) { }

        public TransactionMock BeginTransaction() => CreateTransaction();
    }
}
