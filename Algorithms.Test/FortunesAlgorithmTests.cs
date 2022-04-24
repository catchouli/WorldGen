using Algorithms.Voronoi;
using GeometRi;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Numerics;
using Utils;

namespace Algorithms.Test
{
  /// <summary>
  /// Tests for FortunesAlgorithm
  /// </summary>
  [TestClass]
  public class FortunesAlgorithmTests
  {
    /// <summary>
    /// A simple case with no weird behavior
    /// </summary>
    public static IEnumerable<object[]> GenerateDiagram_BasicTestCases { get; } = new[]
    {
      // Simple 1-point test case
      new object[] { "1 point", 4, 4, new List<Vector2> {
        new Vector2(512.0f, 512.0f)
      } },

      // Simple 2-point test case
      new object[] { "2 points", 4, 5, new List<Vector2> {
        new Vector2(750.0f, 250.0f), new Vector2(250.0f, 750.0f)
      } },

      // Simple 8-point test case
      new object[] { "8 points", 24, 31, new List<Vector2> {
          new Vector2(232, 79), new Vector2(939, 210), new Vector2(316, 273), new Vector2(693, 364),
          new Vector2(1012, 454), new Vector2(394, 485), new Vector2(131, 615), new Vector2(754, 639)
      } },

      // Edge case where the first n points have the same Y coordinate so there are no active parabolas yet
      new object[] { "SameYCoordinate", 26, 34, new List<Vector2>
      {
        new Vector2(232, 79), new Vector2(610, 79), new Vector2(939, 210), new Vector2(316, 273),
        new Vector2(693, 364), new Vector2(1012, 454), new Vector2(394, 485), new Vector2(131, 615),
        new Vector2(754, 639)
      } },

      // Edge case where two points were close on n=37 and caused a weird issue where an edge would extend all the way
      // across the diagram
      new object[] { "PointsFromSeed(908772445, 37)", 109, 145, PointsFromSeed(908772445, 37, 1024.0f, 1024.0f) },

      // Something similar, no idea what's happening there but it has a lot of edges going everywhere over the diagram
      new object[] { "PointsFromSeed(1380721063, 91)", 268, 358, PointsFromSeed(1380721063, 91, 1024.0f, 1024.0f) },
    };

    /// <summary>
    /// A simple case that should work fine
    /// </summary>
    [DataTestMethod]
    [DynamicData(nameof(GenerateDiagram_BasicTestCases))]
    public void GenerateDiagram_BasicTest(string _, int expectedVertexCount, int expectedEdgeCount,
      List<Vector2> points)
    {
      // Arrange
      const float Width = 1024;
      const float Height = 1024;

      // I put debugOutput to true because it's handy to see and it might catch some extra issues
      var fortune = new FortunesAlgorithm(true);

      // Act
      var voronoi = fortune.GenerateDiagram(points, new Vector4(0.0f, 0.0f, Width, Height));

      // Assert
      voronoi.VoronoiVertices.Length.ShouldBe(expectedVertexCount);
      voronoi.Edges.Length.ShouldBe(expectedEdgeCount);

      // Some simple sanity checks
      CheckDiagramRoughlyValid(voronoi, Width, Height).ShouldBe(true);
    }

    /// <summary>
    /// Check that an invalid input of 0 points is rejected with an argument exception
    /// </summary>
    [TestMethod]
    public void GenerateDiagram_InvalidInput()
    {
      // Arrange
      var fortune = new FortunesAlgorithm(false);

      // Assert
      Assert.ThrowsException<ArgumentException>(
        () => fortune.GenerateDiagram(new List<Vector2>(), new Vector4(0.0f, 0.0f, 1.0f, 1.0f)),
        "Points contained no items");
    }

    /// <summary>
    /// A test that generates a voronoi diagram with random input points to see if it fails
    /// </summary>
    [TestMethod]
    public void GenerateDiagram_RandomInputTest()
    {
      const bool ShouldLoop = false;

      // === Assert ===
      const float Width = 1024.0f;
      const float Height = 1024.0f;

      // Create fortune algorithm instance
      var fortune = new FortunesAlgorithm(false);

      // Create random number generator
      var rng = new Random();

      // Loop forever if ShouldLoop is true
      while (true)
      {
        // Generate seed and count
        int seed = rng.Next();
        int count = rng.Next(1, 100);

        // Generate points
        var points = PointsFromSeed(seed, count, Width, Height);

        // === Act ===
        // Generate diagram
        Console.WriteLine($"Generating diagram for seed={seed} and count={count}");
        var diagram = fortune.GenerateDiagram(points, new Vector4(0.0f, 0.0f, Width, Height));

        // === Assert ===
        CheckDiagramRoughlyValid(diagram, Width, Height).ShouldBe(true);

        if (!ShouldLoop)
          break;
      }
    }

    /// <summary>
    /// Create a set of input points from a given seed and count
    /// </summary>
    /// <param name="seed">The seed</param>
    /// <param name="count">The count</param>
    /// <param name="maxX">The max X coordinate value</param>
    /// <param name="maxY">The max Y coordinate value</param>
    /// <returns>The points</returns>
    private static List<Vector2> PointsFromSeed(int seed, int count, float maxX, float maxY)
    {
      var set = new List<Vector2>();
      var rng = new Random(seed);

      for (int i = 0; i < count; ++i)
      {
        set.Add(new Vector2(rng.NextSingle() * maxX, rng.NextSingle() * maxY));
      }

      return set;
    }

    /// <summary>
    /// Runs some simple sanity checks to see if the diagram is roughly valid
    /// </summary>
    /// <param name="diagram">The voronoi diagram</param>
    private static bool CheckDiagramRoughlyValid(VoronoiDiagram diagram, float maxX, float maxY)
    {
      // Basic sanity check for each vertex
      var verticesEncountered = new List<Vector2>();
      foreach (var v in diagram.VoronoiVertices)
      {
        float.IsNaN(v.Position.X).ShouldBe(false);
        float.IsNaN(v.Position.Y).ShouldBe(false);
        float.IsInfinity(v.Position.X).ShouldBe(false);
        float.IsInfinity(v.Position.Y).ShouldBe(false);
        v.Position.X.ShouldBeGreaterThanOrEqualTo(0.0f);
        v.Position.Y.ShouldBeGreaterThanOrEqualTo(0.0f);
        v.Position.X.ShouldBeLessThanOrEqualTo(maxX);
        v.Position.Y.ShouldBeLessThanOrEqualTo(maxY);

        v.Edges.Count.ShouldBeGreaterThan(0);

        verticesEncountered.Add(v.Position);
      }

      // Basic sanity check for edges
      var verticesToEdges = new Dictionary<VoronoiDiagram.Vertex, List<VoronoiDiagram.Edge>>();
      var edgesEncountered = new List<(Vector2, Vector2)>();
      foreach (var e in diagram.Edges)
      {
        diagram.VoronoiVertices.ShouldContain(e.CornerA);
        diagram.VoronoiVertices.ShouldContain(e.CornerB);
        edgesEncountered.Add((e.CornerA.Position, e.CornerB.Position));

        if (!verticesToEdges.TryGetValue(e.CornerA, out var vertexEdgesA))
        {
          vertexEdgesA = new List<VoronoiDiagram.Edge>();
          verticesToEdges.Add(e.CornerA, vertexEdgesA);
        }

        if (!verticesToEdges.TryGetValue(e.CornerB, out var vertexEdgesB))
        {
          vertexEdgesB = new List<VoronoiDiagram.Edge>();
          verticesToEdges.Add(e.CornerB, vertexEdgesB);
        }

        vertexEdgesA.Add(e);
        vertexEdgesB.Add(e);
      }

      // Check for duplicate vertices and edges
      CollectionAssert.AllItemsAreUnique(verticesEncountered);
      CollectionAssert.AllItemsAreUnique(edgesEncountered);

      // Check each vertex actually has a reference to each edge connected to it
      foreach (var (vertex, vertexEdges) in verticesToEdges)
      {
        vertex.Edges.Count.ShouldBe(vertexEdges.Count);
        foreach (var edge in vertexEdges)
        {
          vertex.Edges.ShouldContain(edge);
        }
      }

      // Check if any segments intersect (they should only ever meet at vertices)
      foreach (var a in diagram.Edges)
      {
        foreach (var b in diagram.Edges)
        {
          if (a.CornerA.Position == b.CornerA.Position && a.CornerB.Position == b.CornerB.Position)
            continue;

          HasInvalidIntersection(a, b).ShouldBe(false);
        }
      }

      return true;
    }

    /// <summary>
    /// Checks whether two edges intersect (beyond just touching at one vertex, which is allowed)
    /// </summary>
    /// <param name="a">Vertex a</param>
    /// <param name="b">Vertex b</param>
    /// <returns>Whether they intersect</returns>
    private static bool HasInvalidIntersection(VoronoiDiagram.Edge a, VoronoiDiagram.Edge b)
    {
      var a0 = new Point3d((double)a.CornerA.Position.X, (double)a.CornerA.Position.Y, 0.0);
      var a1 = new Point3d((double)a.CornerB.Position.X, (double)a.CornerB.Position.Y, 0.0);
      var b0 = new Point3d((double)b.CornerA.Position.X, (double)b.CornerA.Position.Y, 0.0);
      var b1 = new Point3d((double)b.CornerB.Position.X, (double)b.CornerB.Position.Y, 0.0);

      var segmentA = new Segment3d(a0, a1);
      var segmentB = new Segment3d(b0, b1);

      var intersection = segmentA.IntersectionWith(segmentB);

      // If there was no intersect that's good
      if (intersection == null)
      {
        return false;
      }
      // If there was a segment intersection that's no good
      else if (intersection is Segment3d)
      {
        return true;
      }
      // If there was a point intersection we need to check it's actually one of the vertices on each segment, in which
      // case they just share that vertex.
      else if (intersection is Point3d point)
      {
        bool sharedVertex = (point == a0 || point == a1) && (point == b0 || point == b1);
        return !sharedVertex;
      }
      else
      {
        // This should never happen since those are the three return values of IntersectionWith
        throw new InternalErrorException("IntersectionWith returned an invalid type");
      }
    }
  }
}