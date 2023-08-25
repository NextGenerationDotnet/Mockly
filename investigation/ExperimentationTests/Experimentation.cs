using Mockly.Attributes;

using NSubstitute;

namespace ExperimentationTests;

public class Experimentation
{
    [Fact]
    public void StructureOfNSubCallMockingAndAssertion()
    {
        // Arrange
        var dependency = Substitute.For<IDependency>();
        dependency.DoSomethingThatNeedsMocking(5).Returns(1);

        var sut = new SubjectUnderTest(dependency);

        // Act
        sut.Run(5);

        // Assert
        dependency.Received(1).DoSomethingThatNeedsMocking(5);
    }

    [Fact]
    public void StructureOfMocklyCallAssertion()
    {
        // Arrange
        var dependency = new FakeDependency
        {
            MockDoSomethingThatNeedsMockingMethod = _ => 7
        };

        var sut = new SubjectUnderTest(dependency);

        int startingNumber = 5;

        // Act
        sut.Run(startingNumber);

        // Assert
        dependency.NumberOfCallsToDoSomethingThatNeedsMockingWith(startingNumber).ShouldBe(1);
    }
}

public partial class FakeDependency : IDependency
{
    [Mocklify]
    public partial int DoSomethingThatNeedsMocking(int val);
}

public partial class FakeDependency : IDependency
{
    public Func<int, int>? MockDoSomethingThatNeedsMockingMethod { set; get; }

    private readonly Dictionary<int, int> _doSomethingThatNeedsMockingCallCounters = new();

    public partial int DoSomethingThatNeedsMocking(int val)
    {
        _doSomethingThatNeedsMockingCallCounters.TryGetValue(val, out int counter);
        _doSomethingThatNeedsMockingCallCounters[val] = ++counter;

        return MockDoSomethingThatNeedsMockingMethod?.Invoke(val) ?? default;
    }

    public int NumberOfCallsToDoSomethingThatNeedsMockingWith(int val)
    {
        _doSomethingThatNeedsMockingCallCounters.TryGetValue(val, out int counter);

        return counter;
    }
}

public class SubjectUnderTest
{
    private readonly IDependency _dependency;

    public SubjectUnderTest(IDependency dependency)
    {
        _dependency = dependency;
    }

    public void Run(int startingNumber)
    {
        _ = _dependency.DoSomethingThatNeedsMocking(startingNumber);
    }
}

public interface IDependency
{
    public int DoSomethingThatNeedsMocking(int val);
}