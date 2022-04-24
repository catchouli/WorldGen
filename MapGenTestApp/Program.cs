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
    private static Random _rng = new Random();

    /// <summary>
    /// Main
    /// </summary>
    public static void Main()
    {
      Console.WriteLine($"Generating {Width} * {Height} image");

      // Render parabola
      //RenderParabolaConstruction(Width, Height, new Vector2(500, 500), 700, "parabola.png");
      RenderParabolaFormula(Width, Height, new Vector2(500, 500), 700, "parabola.png");

      // Render map
      var mapOptions = new MapOptions
      {
        PointCount = 10000
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

      // Generate points
      for (int i = 0; i < options.PointCount; ++i)
      {
        map.Points.Add(new Vector2(_rng.NextSingle() * (float)Width, _rng.NextSingle() * (float)Height));
      }

      // same Y edge case
      //map.Points.Clear();
      //map.Points = new List<Vector2>
      //{
        //new Vector2(232, 79), new Vector2(610, 79), new Vector2(939, 210), new Vector2(316, 273),
        //new Vector2(693, 364), new Vector2(1012, 454), new Vector2(394, 485), new Vector2(131, 615),
        //new Vector2(754, 639)
      //};

      // Generate voronoi diagram
      var fortune = new FortunesAlgorithm(true);
      map.Voronoi = fortune.GenerateDiagram(map.Points, new Vector4(0.0f, 0.0f, (float)Width, (float)Height));

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

      using var image = new Image<Rgba32>(Width, Height);

      // fill white
      image.Mutate(x => x.Fill(Color.White));

      // Draw points
      foreach (var point in map.Points)
      {
        var ellipse = new EllipsePolygon(point.X, point.Y, PointRadius);
        image.Mutate(x => x.Draw(Pens.Solid(Color.Red, 1.0f), ellipse));
      }

      // Render voronoi diagram
      var orangePen = Pens.Solid(Color.Orange, PointRadius);
      foreach (var edge in map.Voronoi.Edges)
      {
        image.Mutate(x => x.DrawLines(orangePen, new PointF(edge.a.Position.X, edge.a.Position.Y), new PointF(edge.b.Position.X, edge.b.Position.Y)));
      }
      foreach (var vertex in map.Voronoi.Vertices)
      {
        var point = new PointF(vertex.Position.X, vertex.Position.Y);
        image.Mutate(x => x.DrawLines(Pens.Solid(Color.Green, PointRadius), point, point));
      }

      // Save image
      image.Save(filename);
    }
  }
}