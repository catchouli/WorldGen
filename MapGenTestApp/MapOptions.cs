namespace MapGenTestApp
{
  /// <summary>
  /// The options for the generated map
  /// </summary>
  public class MapOptions
  {
    /// <summary>
    /// The number of points to use when generating the voronoi diagram
    /// </summary>
    public int PointCount = 100;

    /// <summary>
    /// The number of times to relax the voronoi diagram
    /// </summary>
    public int Relaxations = 2;
  }
}
