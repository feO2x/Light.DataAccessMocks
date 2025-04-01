using FluentAssertions;
using Xunit;

namespace Light.DataAccessMocks.Tests;

public static class TransactionalSessionMockTests
{
    [Fact]
    public static void MustBeAbstractClass()
    {
        typeof(TransactionalSessionMock<>).Should().BeAbstract();
        typeof(TransactionalSessionMock).Should().BeAbstract();
    }

    [Fact]
    public static void NonGenericTypeMustDeriveFromGenericType() =>
        typeof(TransactionalSessionMock).Should().BeDerivedFrom<TransactionalSessionMock<TransactionalSessionMock>>();

    [Fact]
    public static void MustDeriveFromBaseTransactionSessionMock() =>
        typeof(TransactionalSessionMock).Should()
           .BeDerivedFrom<BaseTransactionalSessionMock<TransactionMock, TransactionalSessionMock>>();
}
