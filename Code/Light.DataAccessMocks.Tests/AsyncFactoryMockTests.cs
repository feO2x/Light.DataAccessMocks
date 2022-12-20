using System.Threading.Tasks;
using FluentAssertions;
using Light.SharedCore.DataAccessAbstractions;
using Light.SharedCore.Initialization;
using Xunit;

namespace Light.DataAccessMocks.Tests;

public static class SessionFactoryMockTests
{
    [Fact]
    public static void MustImplementIAsyncFactory() =>
        typeof(AsyncFactoryMock<Session>).Should().Implement(typeof(IAsyncFactory<Session>));

    [Fact]
    public static void SessionMustBeRetrievable()
    {
        using var session = new Session();
        var factory = CreateAsyncFactory(session);

        factory.Instance.Should().BeSameAs(session);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(9)]
    [InlineData(21)]
    public static async Task SessionMustBeReturnedOnCreateAsync(int numberOfCalls)
    {
        await using var session = new Session();
        var factory = CreateAsyncFactory(session);

        for (var i = 0; i < numberOfCalls; i++)
        {
            await using var createdSession = await factory.CreateAsync();
            createdSession.Should().BeSameAs(session);
        }
    }

    public static class WhenCreateMustNotHaveBeenCalled
    {
        [Fact]
        public static void NoExceptionWhenSessionWasNotCreated()
        {
            var factory = CreateAsyncFactory();

            factory.CreateMustNotHaveBeenCalled().Should().BeSameAs(factory);
        }

        [Fact]
        public static async Task ExceptionWhenCreateWasCalledOnce()
        {
            var factory = CreateAsyncFactory();
            await factory.CreateAsync();

            var act = () => factory.CreateMustNotHaveBeenCalled();

            act.Should().Throw<TestException>()
               .And.Message.Should().Be("CreateAsync must not have been called, but it was actually called 1 time.");
        }

        [Theory]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(11)]
        public static async Task ExceptionWhenCreateWasCalledMultipleTimes(int numberOfCalls)
        {
            var factory = CreateAsyncFactory();
            for (var i = 0; i < numberOfCalls; i++)
            {
                await factory.CreateAsync();
            }

            var act = () => factory.CreateMustNotHaveBeenCalled();

            act.Should().Throw<TestException>()
               .And.Message.Should().Be($"CreateAsync must not have been called, but it was actually called {numberOfCalls} times.");
        }
    }

    public static class WhenCreateMustHaveBeenCalled
    {
        [Fact]
        public static void ExceptionWhenCreateWasNotCalled()
        {
            var factory = CreateAsyncFactory();

            var act = () => factory.CreateMustHaveBeenCalled();

            act.Should().Throw<TestException>()
               .And.Message.Should().Be("CreateAsync must have been called exactly once, but it was actually never called.");
        }

        [Fact]
        public static async Task NoExceptionWhenCreateIsCalledExactlyOnce()
        {
            var factory = CreateAsyncFactory();

            await factory.CreateAsync();

            factory.CreateMustHaveBeenCalled().Should().BeSameAs(factory);
        }

        [Theory]
        [InlineData(5)]
        [InlineData(2)]
        [InlineData(28)]
        public static async Task ExceptionWhenCreateWasCalledSeveralTimes(int numberOfCalls)
        {
            var factory = CreateAsyncFactory();
            for (var i = 0; i < numberOfCalls; i++)
            {
                await factory.CreateAsync();
            }

            var act = () => factory.CreateMustHaveBeenCalled();

            act.Should().Throw<TestException>()
               .And.Message.Should().Be($"CreateAsync must have been called exactly once, but it was actually called {numberOfCalls} times.");
        }
    }

    private static AsyncFactoryMock<IAsyncSession> CreateAsyncFactory(Session? session = null) =>
        new (session ?? new Session());

    private sealed class Session : AsyncSessionMock { }
}