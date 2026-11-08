namespace AIPractice.BuildingBlock;

public static class VectorMath
{
    public static double Cosine(float[] left, float[] right)
    {
        if (left.Length == 0 || left.Length != right.Length)
            throw new InvalidDataException("Embedding dimensions differ.");

        double dot = 0, leftNorm = 0, rightNorm = 0;
        for (var i = 0; i < left.Length; i++)
        {
            dot += (double)left[i] * right[i];
            leftNorm += (double)left[i] * left[i];
            rightNorm += (double)right[i] * right[i];
        }

        if (leftNorm == 0 || rightNorm == 0)
            throw new InvalidDataException("Zero embedding vector.");

        return dot / (Math.Sqrt(leftNorm) * Math.Sqrt(rightNorm));
    }
}
