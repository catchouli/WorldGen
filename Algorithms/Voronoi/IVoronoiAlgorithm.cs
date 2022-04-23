﻿using System.Numerics;

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
    /// <param name="points">The points</param>
    /// <param name="extents">The extents as (minX, minY, maxX, maxY)</param>
    public VoronoiDiagram GenerateDiagram(IEnumerable<Vector2> points, Vector4 extents);
  }
}