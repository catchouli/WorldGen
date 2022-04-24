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

      var sitePens = new Dictionary<Vector2, Pen>(map.Voronoi.SiteVertices.Select(v => {
        var color = new Color(new Vector4(_rng.NextSingle(), _rng.NextSingle(), _rng.NextSingle(), 1.0f));
        var pen = Pens.Solid(color, 3.0f);
        return new KeyValuePair<Vector2, Pen>(v.Position, pen);
      }));

      using var image = new Image<Rgba32>(Width, Height);

      // fill white
      image.Mutate(x => x.Fill(Color.White));

      // Draw points
      foreach (var site in map.Voronoi.SiteVertices)
      {
        var ellipse = new EllipsePolygon(site.Position.X, site.Position.Y, PointRadius);
        image.Mutate(x => x.Draw(sitePens[site.Position], ellipse));
      }

      // Render voronoi diagram
      var orangePen = Pens.Solid(Color.Orange, PointRadius);
      foreach (var edge in map.Voronoi.Edges)
      {
        // TODO: fix the edge edges
        if (edge.SiteA == null || edge.SiteB == null)
          continue;

        var center = 0.5f * edge.CornerA.Position + 0.5f * edge.CornerB.Position;
        var centerDir = Vector2.Normalize(center - edge.SiteA.Position);

        var offset = new PointF(centerDir.X * 1.0f, centerDir.Y * 1.0f);
        offset.X = 0.0f;
        offset.Y = 0.0f;

        var pointA = new PointF(edge.CornerA.Position.X, edge.CornerA.Position.Y);
        var pointB = new PointF(edge.CornerB.Position.X, edge.CornerB.Position.Y);

        image.Mutate(x => x.DrawLines(sitePens[edge.SiteA.Position], pointA - offset, pointB - offset));
        image.Mutate(x => x.DrawLines(sitePens[edge.SiteB.Position], pointA + offset, pointB + offset));
      }
      foreach (var vertex in map.Voronoi.VoronoiVertices)
      {
        var point = new PointF(vertex.Position.X, vertex.Position.Y);
        image.Mutate(x => x.DrawLines(Pens.Solid(Color.Green, PointRadius), point, point));
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