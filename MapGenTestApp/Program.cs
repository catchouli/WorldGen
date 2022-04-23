using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Drawing;
using System.Numerics;
using MapGenTestApp.Voronoi;

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
        PointCount = 100
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

      // Add test points
      //map.Points.Clear();
      // TODO: reenable this edge case
      //map.Points.AddRange(new[] { new Vector2(232, 79), /*new Vector2(610, 79),*/ new Vector2(939, 210), new Vector2(316, 273), new Vector2(693, 364), new Vector2(1012, 454), new Vector2(394, 485), new Vector2(131, 615), new Vector2(754, 639) });

      // Generate voronoi diagram
      map.Voronoi = FortunesAlgorithm.Generate(map.Points, new Vector4(0.0f, 0.0f, (float)Width, (float)Height));

      return map;
    }

    /// <summary>
    /// Render a map to the given filename
    /// </summary>
    /// <param name="map">The map</param>
    /// <param name="filename">The output filename</param>
    private static void RenderDebugMap(Map map, string filename)
    {
      const float PointRadius = 3.0f;

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
      var orangePen = Pens.Solid(Color.Orange, 3.0f);
      foreach (var edge in map.Voronoi.Edges)
      {
        image.Mutate(x => x.DrawLines(orangePen, new PointF(edge.a.Position.X, edge.a.Position.Y), new PointF(edge.b.Position.X, edge.b.Position.Y)));
      }

      // Save image
      image.Save(filename);
    }

    /// <summary>
    /// Render a parabola with the given focus and directrix to the output filename
    /// </summary>
    private static void RenderParabolaConstruction(int width, int height, Vector2 focus, float directrixY, string filename)
    {
      var distances = new float[width, height, 2];

      for (int y = 0; y < height; ++y)
      {
        for (int x = 0; x < width; ++x)
        {
          float distToFocus = (float)Math.Sqrt((x - focus.X) * (x - focus.X) + (y - focus.Y) * (y - focus.Y));
          float distToDirectrix = Math.Abs(y - directrixY);
          distances[x, y, 0] = distToFocus;
          distances[x, y, 1] = distToDirectrix;
        }
      }

      using var image = new Image<Rgba32>(width, height);

      for (int y = 0; y < height; ++y)
      {
        for (int x = 0; x < width; ++x)
        {
          var distToFocus = distances[x, y, 0];
          var distToDirectrix = distances[x, y, 1];
          if (Math.Abs(distToDirectrix - distToFocus) < 0.5f)
            image[x, y] = Color.White;
          else
            image[x, y] = new Color(new Vector4(Math.Abs(distances[x, y, 0] / 1024.0f - distances[x, y, 1] / 1024.0f), 0.0f, 0.0f, 1.0f));
        }
      }

      image.Mutate(x => x.DrawLines(Pens.Solid(Color.White, 5.0f), new PointF(focus.X, focus.Y), new PointF(focus.X, focus.Y)));
      image.Mutate(x => x.DrawLines(Pens.Solid(Color.White, 2.0f), new PointF(0.0f, directrixY), new PointF(1024.0f, directrixY)));

      image.Save(filename);
    }

    /// <summary>
    /// Render a parabola with the given focus and directrix to the output filename
    /// </summary>
    private static void RenderParabolaFormula(int width, int height, Vector2 focus, float directrixY, string filename)
    {
      using var image = new Image<Rgba32>(width, height);

      image.Mutate(x => x.Fill(Color.Black));

      for (int x = 0; x < width; ++x)
      {
        float y = GetParabolaY((float)x, focus, directrixY);
        float yInt = (int)y;
        if (yInt >= 0 && yInt + 1 < height)
          image[x, (int)y] = Color.White;
      }

      image.Mutate(x => x.DrawLines(Pens.Solid(Color.White, 5.0f), new PointF(focus.X, focus.Y), new PointF(focus.X, focus.Y)));
      image.Mutate(x => x.DrawLines(Pens.Solid(Color.White, 2.0f), new PointF(0.0f, directrixY), new PointF(1024.0f, directrixY)));

      image.Save(filename);
    }

    /// <summary>
    /// Get the Y of a parabola with the given x, focus and directrix
    /// </summary>
    private static float GetParabolaY(float x, Vector2 focus, float directrixY)
    {
      // https://jacquesheunis.com/post/fortunes-algorithm/
      // y = (1 / 2(yf - yd)) * (x - xf)^2 + (yf + yd)/2
      // y = A * B^2 + C
      float A = 1.0f / (2.0f * (focus.Y - directrixY));
      float B = x - focus.X;
      float C = (focus.Y + directrixY) / 2.0f;

      return A * B * B + C;
    }
  }
}