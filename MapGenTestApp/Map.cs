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
    public List<Vector2> Points = new List<Vector2>();

    /// <summary>
    /// The voronoi diagram
    /// </summary>
    public VoronoiDiagram Voronoi;
  }
}
