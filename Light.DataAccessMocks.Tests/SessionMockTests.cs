using System;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Light.DataAccessMocks.Tests;

public static class SessionMockTests
{
    [Fact]
    public static void MustBeAbstract()
    {
        typeof(SessionMock<>).Should().BeAbstract();
        typeof(SessionMock).Should().BeAbstract();
    }

    [Fact]
    public static void NonGenericTypeMustDeriveFromGenericType() =>
        typeof(SessionMock).Should().BeDerivedFrom<SessionMock<SessionMock>>();

    [Fact]
    public static void MustDeriveFromAsyncReadOnlySessionMock() =>
        typeof(SessionMock<>).Should().BeDerivedFrom(typeof(BaseSessionMock<>));

    [Fact]
    public static void ExceptionWhenSaveChangesWasNotCalled()
    {
        var session = new Session();

        session.CheckException(0);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(23)]
    public static async Task ExceptionWhenSaveChangesAsyncWasCalledTooOften(int numberOfCalls)
    {
        var session = new Session();

        for (var i = 0; i < numberOfCalls; i++)
        {
            await session.SaveChangesAsync();
        }

        session.CheckException(numberOfCalls);
    }

    private static void CheckException(this Session session, int numberOfCalls)
    {
        Action act = () => session.SaveChangesMustHaveBeenCalled();

        act.Should().Throw<TestException>()
           .And.Message.Should().Be(
                $"SaveChangesAsync must have been called exactly once, but it was called {numberOfCalls} times."
            );
    }

    [Fact]
    public static async Task NoExceptionWhenSaveChangesWasCalledExactlyOnce()
    {
        var session = new Session();

        await session.SaveChangesAsync();

        session.SaveChangesMustHaveBeenCalled().Should().BeSameAs(session);
    }

    [Fact]
    public static async Task CallCountIncrementationMustBeChecked()
    {
        var session = new Session().SetSaveChangesCallCountToMaximum();

        var act = () => session.SaveChangesAsync();

        await act.Should().ThrowAsync<OverflowException>();
    }

    [Fact]
    public static void NoExceptionWhenSaveChangesAsyncWasNotCalled()
    {
        var session = new Session();

        session.SaveChangesMustNotHaveBeenCalled().Should().BeSameAs(session);
    }

    [Fact]
    public static async Task ExceptionWhenSaveChangesWasCalled()
    {
        var session = new Session();
        await session.SaveChangesAsync();

        Action act = () => session.SaveChangesMustNotHaveBeenCalled();

        act.Should().Throw<TestException>()
           .And.Message.Should().Be("SaveChangesAsync must not have been called, but it was called 1 time.");
    }

    [Fact]
    public static async Task ThrowExceptionOnSaveChanges()
    {
        var exception = new Exception();
        var session = new Session { ExceptionOnSaveChanges = exception };

        var act = () => session.SaveChangesAsync();

        (await act.Should().ThrowAsync<Exception>())
           .Which.Should().BeSameAs(exception);
    }

    private sealed class Session : SessionMock
    {
        public Session SetSaveChangesCallCountToMaximum()
        {
            SaveChangesCallCount = int.MaxValue;
            return this;
        }
    }
}
