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
      public HashSet<Edge> Edges = new HashSet<Edge>();
    }

    /// <summary>
    /// An edge in the diagram
    /// </summary>
    public class Edge
    {
      public Vertex a, b;
    }

    /// <summary>
    /// The sites used to generate the diagram
    /// </summary>
    public Vector2[] Sites;

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
