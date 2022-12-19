using System;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Light.DataAccessMocks.Tests;

public static class AsyncSessionMockTests
{
    [Fact]
    public static void MustBeAbstract()
    {
        typeof(AsyncSessionMock<>).Should().BeAbstract();
        typeof(AsyncSessionMock).Should().BeAbstract();
    }

    [Fact]
    public static void NonGenericTypeMustDeriveFromGenericType() =>
        typeof(AsyncSessionMock).Should().BeDerivedFrom<AsyncSessionMock<AsyncSessionMock>>();

    [Fact]
    public static void MustDeriveFromAsyncReadOnlySessionMock() =>
        typeof(AsyncSessionMock<>).Should().BeDerivedFrom(typeof(BaseSessionMock<>));

    [Fact]
    public static void ExceptionWhenSaveChangesWasNotCalled()
    {
        var session = new AsyncSession();

        session.CheckException(0);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(23)]
    public static async Task ExceptionWhenSaveChangesAsyncWasCalledTooOften(int numberOfCalls)
    {
        var session = new AsyncSession();

        for (var i = 0; i < numberOfCalls; i++)
        {
            await session.SaveChangesAsync();
        }

        session.CheckException(numberOfCalls);
    }

    private static void CheckException(this AsyncSession session, int numberOfCalls)
    {
        Action act = () => session.SaveChangesMustHaveBeenCalled();

        act.Should().Throw<TestException>()
           .And.Message.Should().Be($"SaveChangesAsync must have been called exactly once, but it was called {numberOfCalls} times.");
    }

    [Fact]
    public static async Task NoExceptionWhenSaveChangesWasCalledExactlyOnce()
    {
        var session = new AsyncSession();

        await session.SaveChangesAsync();

        session.SaveChangesMustHaveBeenCalled().Should().BeSameAs(session);
    }

    [Fact]
    public static void CallCountIncrementationMustBeChecked()
    {
        var session = new AsyncSession().SetSaveChangesCallCountToMaximum();

        Func<Task> act = () => session.SaveChangesAsync();

        act.Should().ThrowAsync<OverflowException>();
    }

    [Fact]
    public static void NoExceptionWhenSaveChangesAsyncWasNotCalled()
    {
        var session = new AsyncSession();

        session.SaveChangesMustNotHaveBeenCalled().Should().BeSameAs(session);
    }

    [Fact]
    public static async Task ExceptionWhenSaveChangesWasCalled()
    {
        var session = new AsyncSession();
        await session.SaveChangesAsync();

        Action act = () => session.SaveChangesMustNotHaveBeenCalled();

        act.Should().Throw<TestException>()
           .And.Message.Should().Be("SaveChangesAsync must not have been called, but it was called 1 time.");
    }

    [Fact]
    public static async Task ThrowExceptionOnSaveChanges()
    {
        var exception = new Exception();
        var session = new AsyncSession { ExceptionOnSaveChanges = exception };

        Func<Task> act = () => session.SaveChangesAsync();

        (await act.Should().ThrowAsync<Exception>())
           .Which.Should().BeSameAs(exception);
    }

    private sealed class AsyncSession : AsyncSessionMock
    {
        public AsyncSession SetSaveChangesCallCountToMaximum()
        {
            SaveChangesCallCount = int.MaxValue;
            return this;
        }
    }
}