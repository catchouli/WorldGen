using System.Numerics;

namespace Algorithms.Voronoi
{
  /// <summary>
  /// Class that represents a voronoi diagram
  /// </summary>
  public class VoronoiDiagram
  {
    /// <summary>
    /// A site from the input data
    /// </summary>
    public class Site
    {
      /// <summary>
      /// The site's position
      /// </summary>
      public Vector2 Position;

      /// <summary>
      /// The edges of the cell around the site
      /// </summary>
      public List<Edge> Edges = new List<Edge>();
    }

    /// <summary>
    /// A vertex in the diagram
    /// </summary>
    public class Vertex
    {
      /// <summary>
      /// The position of the vertex
      /// </summary>
      public Vector2 Position;

      /// <summary>
      /// The edges that make up the vertex
      /// </summary>
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
      public Site SiteA, SiteB;
    }

    /// <summary>
    /// The site vertices
    /// </summary>
    public Site[] Sites;

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
