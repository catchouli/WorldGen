using System.Numerics;

namespace Algorithms.Voronoi
{
  /// <summary>
  /// Class that represents a voronoi diagram
  /// </summary>
  public class VoronoiDiagram
  {
    /// <summary>
    /// A vertex in the diagram
    /// </summary>
    public class Vertex
    {
      public Vector2 Position;
      public List<Edge> Edges = new List<Edge>();
    }

    /// <summary>
    /// An edge in the diagram
    /// </summary>
    public class Edge
    {
      public Vertex a, b;
    }

    /// <summary>
    /// The vertices in the diagram
    /// </summary>
    public Vertex[] Vertices;

    /// <summary>
    /// The edges in the diagram
    /// </summary>
    public Edge[] Edges;
  }
}
