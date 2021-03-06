using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Drawing;
using System.Numerics;
using Algorithms.Voronoi;

namespace MapGenTestApp
{
  /// <summary>
  /// The map gen test app
  /// </summary>
  public static class MapGenTestApp
  {
    /// <summary>
    /// Width of the output image
    /// </summary>
    private const int Width = 1024;

    /// <summary>
    /// Height of the output image
    /// </summary>
    private const int Height = 1024;

    /// <summary>
    /// The random number generator
    /// </summary>
    private static Random _rng = new Random(117);

    /// <summary>
    /// Main
    /// </summary>
    public static void Main()
    {
      Console.WriteLine($"Generating {Width} * {Height} image");

      // Render map
      var mapOptions = new MapOptions
      {
        PointCount = 100,
        Relaxations = 2
      };

      var map = GenerateMap(mapOptions);
      RenderDebugMap(map, "out.png");
    }

    /// <summary>
    /// Generates the map
    /// </summary>
    /// <returns>The generated map</returns>
    private static Map GenerateMap(MapOptions options)
    {
      var map = new Map();

      // The extents for the constrained voronoi diagram
      var extents = new Vector4(0.0f, 0.0f, (float)Width, (float)Height);

      // Generate points
      for (int i = 0; i < options.PointCount; ++i)
      {
        map.Points.Add(new Vector2(_rng.NextSingle() * extents.Z, _rng.NextSingle() * extents.W));
      }

      // Generate voronoi diagram
      IVoronoiAlgorithm algorithm = new FortunesAlgorithm(true);
      map.Voronoi = algorithm.GenerateDiagram(map.Points, extents);
      for (int i = 0; i < options.Relaxations; ++i)
        map.Voronoi = algorithm.Relax(map.Voronoi, extents);

      return map;
    }

    /// <summary>
    /// Render a map to the given filename
    /// </summary>
    /// <param name="map">The map</param>
    /// <param name="filename">The output filename</param>
    private static void RenderDebugMap(Map map, string filename)
    {
      const float PointRadius = 1.0f;
      const float EdgeThickness = 3.0f;

      using var image = new Image<Rgba32>(Width, Height);

      // fill white
      image.Mutate(x => x.Fill(Color.White));

      // Draw points
      foreach (var site in map.Voronoi.Sites)
      {
        // Create new pen with random color
        var color = new Color(new Vector4(_rng.NextSingle(), _rng.NextSingle(), _rng.NextSingle(), 1.0f));
        var pen = Pens.Solid(color, EdgeThickness);

        // Draw site position
        var ellipse = new EllipsePolygon(site.Position.X, site.Position.Y, PointRadius);
        image.Mutate(x => x.Draw(pen, ellipse));

        // Draw edges
        foreach (var edge in site.Edges)
        {
          var center = 0.5f * edge.CornerA.Position + 0.5f * edge.CornerB.Position;
          var centerDir = Vector2.Normalize(center - site.Position);

          var offset = new PointF(centerDir.X * EdgeThickness, centerDir.Y * EdgeThickness);

          var pointA = new PointF(edge.CornerA.Position.X, edge.CornerA.Position.Y);
          var pointB = new PointF(edge.CornerB.Position.X, edge.CornerB.Position.Y);

          image.Mutate(x => x.DrawLines(pen, pointA - offset, pointB - offset));
        }
      }

      // Render voronoi vertices on top
      foreach (var vertex in map.Voronoi.Vertices)
      {
        var point = new PointF(vertex.Position.X, vertex.Position.Y);
        image.Mutate(x => x.DrawLines(Pens.Solid(Color.Green, 3.0f), point, point));
      }

      // Render delaunay triangulation
      foreach (var edge in map.Voronoi.Edges)
      {
        if (edge.SiteA == null || edge.SiteB == null)
          continue;

        var start = new PointF(edge.SiteA.Position.X, edge.SiteA.Position.Y);
        var end = new PointF(edge.SiteB.Position.X, edge.SiteB.Position.Y);

        image.Mutate(x => x.DrawLines(Pens.Solid(Color.Black, 1.0f), start, end));
      }

      // Save image
      image.Save(filename);
    }
  }
}