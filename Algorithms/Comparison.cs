namespace Algorithms
{
  /// <summary>
  /// Comparison methods
  /// </summary>
  public static class Comparison
  {
    /// <summary>
    /// Floating point approx equals with a fixed epsilon
    /// </summary>
    /// <param name="a">The first float</param>
    /// <param name="b">The second float</param>
    /// <returns>Whether they were approximately equal</returns>
    public static bool ApproxEquals(float a, float b)
    {
      return ApproxEquals(a, b, 1e-6f);
    }

    /// <summary>
    /// Floating point approx equals with a custom epsilon
    /// </summary>
    /// <param name="a">The first float</param>
    /// <param name="b">The second float</param>
    /// <returns>Whether they were approximately equal</returns>
    public static bool ApproxEquals(float a, float b, float epsilon)
    {
      return Math.Abs(a - b) < epsilon;
    }
  }
}
