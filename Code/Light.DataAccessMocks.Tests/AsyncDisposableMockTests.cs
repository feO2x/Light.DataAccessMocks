using System;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Light.DataAccessMocks.Tests;

public static class AsyncDisposableMockTests
{
    [Fact]
    public static void MustBeAbstractClass()
    {
        typeof(AsyncDisposableMock<>).Should().BeAbstract();
        typeof(AsyncDisposableMock).Should().BeAbstract();
    }

    [Fact]
    public static void NonGenericTypeMustDeriveFromGenericType() =>
        typeof(AsyncDisposableMock).Should().BeDerivedFrom<AsyncDisposableMock<AsyncDisposableMock>>();

    [Fact]
    public static void MustImplementIDisposable() =>
        typeof(AsyncDisposableMock<>).Should().Implement<IDisposable>();

    [Fact]
    public static void MustImplementIAsyncDisposable() =>
        typeof(AsyncDisposableMock<>).Should().Implement<IAsyncDisposable>();

    [Fact]
    public static void MustImplementIDisposableMock() =>
        typeof(AsyncDisposableMock<>).Should().Implement<IDisposableMock>();

    [Fact]
    public static void ThrowExceptionWhenNotDisposed()
    {
        var disposable = new AsyncDisposable();

        Action act = () => disposable.MustBeDisposed();

        act.Should().Throw<TestException>()
           .And.Message.Should().Be("\"AsyncDisposable\" was not disposed.");
    }

    [Fact]
    public static void NoExceptionWhenDisposed()
    {
        var disposable = new AsyncDisposable();

        disposable.Dispose();

        disposable.EnsureNoExceptionIsThrown();
    }

    [Fact]
    public static async Task NoExceptionWhenDisposedAsync()
    {
        var disposable = new AsyncDisposable();

        await disposable.DisposeAsync();

        disposable.EnsureNoExceptionIsThrown();
    }

    private static void EnsureNoExceptionIsThrown(this AsyncDisposable disposable) =>
        disposable.MustBeDisposed().Should().BeSameAs(disposable);

    [Fact]
    public static void SyncCallCountIncrementationMustBeChecked()
    {
        var disposable = new AsyncDisposable().SetDisposeCountToMaximum();

        Action act = () => disposable.Dispose();

        act.Should().Throw<OverflowException>();
    }

    [Fact]
    public static void AsyncCallCountIncrementationMustBeChecked()
    {
        var disposable = new AsyncDisposable().SetDisposeCountToMaximum();

        Func<Task> act = () => disposable.DisposeAsync().AsTask();

        act.Should().ThrowAsync<OverflowException>();
    }

    private sealed class AsyncDisposable : AsyncDisposableMock
    {
        public AsyncDisposable SetDisposeCountToMaximum()
        {
            DisposeCallCount = int.MaxValue;
            return this;
        }
    }
}