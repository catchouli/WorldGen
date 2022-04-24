using Algorithms.Voronoi;
using System.Numerics;

namespace MapGenTestApp
{
  /// <summary>
  /// A generated map
  /// </summary>
  public class Map
  {
    /// <summary>
    /// The points used for the voronoi diagram
    /// </summary>
    public HashSet<Vector2> Points = new HashSet<Vector2>();

    /// <summary>
    /// The voronoi diagram
    /// </summary>
    public VoronoiDiagram Voronoi;
  }
}
