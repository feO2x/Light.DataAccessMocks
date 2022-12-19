using FluentAssertions;
using Xunit;

namespace Light.DataAccessMocks.Tests;

public static class AsyncTransactionalSessionMockTests
{
    [Fact]
    public static void MustBeAbstractClass()
    {
        typeof(AsyncTransactionalSessionMock<>).Should().BeAbstract();
        typeof(AsyncTransactionalSessionMock).Should().BeAbstract();
    }

    [Fact]
    public static void NonGenericTypeMustDeriveFromGenericType() =>
        typeof(AsyncTransactionalSessionMock).Should().BeDerivedFrom<AsyncTransactionalSessionMock<AsyncTransactionalSessionMock>>();

    [Fact]
    public static void MustDeriveFromBaseTransactionSessionMock() =>
        typeof(AsyncTransactionalSessionMock).Should().BeDerivedFrom<BaseTransactionalSessionMock<AsyncTransactionMock, AsyncTransactionalSessionMock>>();
}