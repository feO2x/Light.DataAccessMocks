using System;
using FluentAssertions;
using Xunit;

namespace Light.DataAccessMocks.Tests;

public static class DisposableMockTests
{
    [Fact]
    public static void MustBeAbstractClass()
    {
        typeof(DisposableMock<>).Should().BeAbstract();
        typeof(DisposableMock).Should().BeAbstract();
    }

    [Fact]
    public static void NonGenericTypeMustDeriveFromGenericType() =>
        typeof(DisposableMock).Should().BeDerivedFrom<DisposableMock<DisposableMock>>();

    [Fact]
    public static void MustImplementIDisposable() =>
        typeof(DisposableMock<>).Should().Implement<IDisposable>();

    [Fact]
    public static void MustImplementIDisposableMock() =>
        typeof(DisposableMock<>).Should().Implement<IDisposableMock>();

    [Fact]
    public static void ThrowExceptionWhenNotDisposed()
    {
        var disposable = new Disposable();

        Action act = () => disposable.MustBeDisposed();

        act.Should().Throw<TestException>()
           .And.Message.Should().Be($"\"{nameof(Disposable)}\" was not disposed.");
    }

    [Fact]
    public static void NoExceptionWhenDisposed()
    {
        var disposable = new Disposable();

        disposable.Dispose();

        disposable.MustBeDisposed().Should().BeSameAs(disposable);
    }

    [Fact]
    public static void CallCountIncrementationMustBeChecked()
    {
        var disposable = new Disposable().SetDisposeCountToMaximum();

        Action act = () => disposable.Dispose();

        act.Should().Throw<OverflowException>();
    }

    private sealed class Disposable : DisposableMock
    {
        public DisposableMock SetDisposeCountToMaximum()
        {
            DisposeCallCount = int.MaxValue;
            return this;
        }
    }
}