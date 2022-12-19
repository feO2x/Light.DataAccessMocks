using FluentAssertions;
using Light.SharedCore.DataAccessAbstractions;
using Xunit;

namespace Light.DataAccessMocks.Tests;

public static class AsyncReadOnlySessionMockTests
{
    [Fact]
    public static void MustBeAbstractClass()
    {
        typeof(AsyncReadOnlySessionMock<>).Should().BeAbstract();
        typeof(AsyncReadOnlySessionMock).Should().BeAbstract();
    }

    [Fact]
    public static void NonGenericTypeMustDeriveFromGenericType() =>
        typeof(AsyncReadOnlySessionMock).Should().BeDerivedFrom<AsyncReadOnlySessionMock<AsyncReadOnlySessionMock>>();

    [Fact]
    public static void MustDeriveFromAsyncDisposableMock() =>
        typeof(AsyncReadOnlySessionMock<>).Should().BeDerivedFrom(typeof(AsyncDisposableMock<>));

    [Fact]
    public static void MustImplementIAsyncReadOnlySession() =>
        typeof(AsyncReadOnlySessionMock<>).Should().Implement<IAsyncReadOnlySession>();
}