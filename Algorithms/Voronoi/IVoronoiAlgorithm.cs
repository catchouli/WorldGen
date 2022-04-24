using System.Numerics;

namespace Algorithms.Voronoi
{
  /// <summary>
  /// Interface for generating voronoi diagrams from points
  /// </summary>
  public interface IVoronoiAlgorithm
  {
    /// <summary>
    /// Generates a Voronoi diagram/triangulation using Fortune's algorithm
    /// https://jacquesheunis.com/post/fortunes-algorithm/
    /// https://pvigier.github.io/2018/11/18/fortune-algorithm-details.html
    /// http://paul-reed.co.uk/fortune.htm
    /// </summary>
    /// <param name="sites">The points</param>
    /// <param name="extents">The extents as (minX, minY, maxX, maxY)</param>
    public VoronoiDiagram GenerateDiagram(IList<Vector2> sites, Vector4 extents);

    /// <summary>
    /// Relax a voronoi diagram using Lloyd's algorithm, the original diagram is unchanged and a new one is returned
    /// </summary>
    /// <param name="diagram">The existing voronoi diagram</param>
    /// <param name="extents">The extents as (minX, minY, maxX, maxY)</param>
    /// <returns>The relaxed voronoi diagram</returns>
    public VoronoiDiagram Relax(in VoronoiDiagram diagram, in Vector4 extents);
  }
}
