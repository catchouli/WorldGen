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
      /// <summary>
      /// The voronoi vertices this edge connects (aka the edges in the voronoi diagram)
      /// </summary>
      public Vertex CornerA, CornerB;

      /// <summary>
      /// The sites this edge connects (aka the edges in the delaunay triangulation)
      /// </summary>
      public Vertex SiteA, SiteB;
    }

    /// <summary>
    /// The site vertices
    /// </summary>
    public Vertex[] SiteVertices;

    /// <summary>
    /// The vertices in the diagram
    /// </summary>
    public Vertex[] VoronoiVertices;

    /// <summary>
    /// The edges in the diagram
    /// </summary>
    public Edge[] Edges;
  }
}
