using FluentAssertions;
using Xunit;

namespace Light.DataAccessMocks.Tests;

public static class ReadOnlySessionMockTests
{
    [Fact]
    public static void MustBeAbstractClass()
    {
        typeof(ReadOnlySessionMock<>).Should().BeAbstract();
        typeof(ReadOnlySessionMock).Should().BeAbstract();
    }

    [Fact]
    public static void NonGenericTypeMustDeriveFromGenericType() =>
        typeof(ReadOnlySessionMock).Should().BeDerivedFrom<ReadOnlySessionMock<ReadOnlySessionMock>>();

    [Fact]
    public static void MustDeriveFromDisposableMock() =>
        typeof(ReadOnlySessionMock<>).Should().BeDerivedFrom(typeof(DisposableMock<>));
}