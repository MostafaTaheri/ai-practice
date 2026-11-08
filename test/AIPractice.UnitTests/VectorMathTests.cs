using AIPractice.BuildingBlock;

namespace AIPractice.UnitTests;

public class VectorMathTests
{
    [Fact]
    public void IdenticalVectors_HaveCosineOne() =>
        Assert.Equal(1, VectorMath.Cosine([1f, 0f], [1f, 0f]), 6);

    [Fact]
    public void OrthogonalVectors_HaveCosineZero() =>
        Assert.Equal(0, VectorMath.Cosine([1f, 0f], [0f, 1f]), 6);

    [Fact]
    public void DifferentDimensions_Throw() =>
        Assert.Throws<InvalidDataException>(() => VectorMath.Cosine([1f], [1f, 0f]));

    [Fact]
    public void ZeroVector_Throws() =>
        Assert.Throws<InvalidDataException>(() => VectorMath.Cosine([0f, 0f], [1f, 0f]));
}
