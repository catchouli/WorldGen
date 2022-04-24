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
      new object[] { "1 point", 4, 4, new HashSet<Vector2> {
        new Vector2(512.0f, 512.0f)
      } },

      // Simple 2-point test case
      new object[] { "2 points", 4, 5, new HashSet<Vector2> {
        new Vector2(750.0f, 250.0f), new Vector2(250.0f, 750.0f)
      } },

      // Simple 8-point test case
      new object[] { "8 points", 24, 31, new HashSet<Vector2> {
          new Vector2(232, 79), new Vector2(939, 210), new Vector2(316, 273), new Vector2(693, 364),
          new Vector2(1012, 454), new Vector2(394, 485), new Vector2(131, 615), new Vector2(754, 639)
      } },

      // Edge case where the first n points have the same Y coordinate so there are no active parabolas yet
      new object[] { "SameYCoordinate", 26, 34, new HashSet<Vector2>
      {
        new Vector2(232, 79), new Vector2(610, 79), new Vector2(939, 210), new Vector2(316, 273),
        new Vector2(693, 364), new Vector2(1012, 454), new Vector2(394, 485), new Vector2(131, 615),
        new Vector2(754, 639)
      } },

      // Edge case where two points were close on n=37 and caused a weird issue where an edge would extend all the way
      // across the diagram
      new object[] { "Weird37Case", 109, 145, new HashSet<Vector2>
      {
        new Vector2(851.7579f, 341.15997f), new Vector2(921.57135f, 465.1639f), new Vector2(834.8818f, 18.377758f),
        new Vector2(483.43f, 826.0167f), new Vector2(307.37506f, 895.4595f), new Vector2(486.83398f, 675.895f),
        new Vector2(209.39368f, 551.3723f), new Vector2(846.1922f, 930.3768f), new Vector2(833.5957f, 348.9687f),
        new Vector2(886.3948f, 12.622356f), new Vector2(722.3821f, 337.71747f), new Vector2(481.41415f, 961.1617f),
        new Vector2(571.9369f, 540.9318f), new Vector2(206.54587f, 166.68617f), new Vector2(269.2978f, 443.39212f),
        new Vector2(95.618164f, 221.56389f), new Vector2(484.71545f, 829.5344f), new Vector2(129.79942f, 96.846634f),
        new Vector2(318.38266f, 445.55557f), new Vector2(364.3122f, 994.4842f), new Vector2(1009.58514f, 915.5187f),
        new Vector2(342.62958f, 925.90265f), new Vector2(368.69266f, 848.5977f), new Vector2(253.50389f, 21.113436f),
        new Vector2(154.34952f, 736.9793f), new Vector2(511.25812f, 416.3551f), new Vector2(826.05536f, 439.902f),
        new Vector2(642.75006f, 514.0404f), new Vector2(883.7458f, 984.40967f), new Vector2(917.227f, 293.94998f),
        new Vector2(835.8319f, 316.74384f), new Vector2(556.71893f, 887.9829f), new Vector2(799.8413f, 265.2701f),
        new Vector2(191.17955f, 403.85925f), new Vector2(421.57288f, 749.3456f), new Vector2(611.9941f, 388.04016f),
        new Vector2(1008.6565f, 915.91064f)
      } },
    };

    /// <summary>
    /// A simple case that should work fine
    /// </summary>
    [DataTestMethod]
    [DynamicData(nameof(GenerateDiagram_BasicTestCases))]
    public void GenerateDiagram_BasicTest(string _, int expectedVertexCount, int expectedEdgeCount,
      HashSet<Vector2> points)
    {
      // Arrange
      const int width = 1024;
      const int height = 1024;

      // I put debugOutput to true because it's handy to see and it might catch some extra issues
      var fortune = new FortunesAlgorithm(true);

      // Act
      var voronoi = fortune.GenerateDiagram(points, new Vector4(0.0f, 0.0f, width, height));

      // Assert
      voronoi.Vertices.Length.ShouldBe(expectedVertexCount);
      voronoi.Edges.Length.ShouldBe(expectedEdgeCount);

      // Basic sanity check for each vertex
      var verticesEncountered = new List<Vector2>();
      foreach (var v in voronoi.Vertices)
      {
        float.IsNaN(v.Position.X).ShouldBe(false);
        float.IsNaN(v.Position.Y).ShouldBe(false);
        float.IsInfinity(v.Position.X).ShouldBe(false);
        float.IsInfinity(v.Position.Y).ShouldBe(false);
        v.Position.X.ShouldBeGreaterThanOrEqualTo(0.0f);
        v.Position.Y.ShouldBeGreaterThanOrEqualTo(0.0f);
        v.Position.X.ShouldBeLessThanOrEqualTo((float)width);
        v.Position.Y.ShouldBeLessThanOrEqualTo((float)height);

        v.Edges.Count.ShouldBeGreaterThan(0);

        verticesEncountered.Add(v.Position);
      }

      // Basic sanity check for edges
      var verticesToEdges = new Dictionary<VoronoiDiagram.Vertex, List<VoronoiDiagram.Edge>>();
      var edgesEncountered = new List<(Vector2, Vector2)>();
      foreach (var e in voronoi.Edges)
      {
        voronoi.Vertices.ShouldContain(e.a);
        voronoi.Vertices.ShouldContain(e.b);
        edgesEncountered.Add((e.a.Position, e.b.Position));

        if (!verticesToEdges.TryGetValue(e.a, out var vertexEdgesA))
        {
          vertexEdgesA = new List<VoronoiDiagram.Edge>();
          verticesToEdges.Add(e.a, vertexEdgesA);
        }

        if (!verticesToEdges.TryGetValue(e.b, out var vertexEdgesB))
        {
          vertexEdgesB = new List<VoronoiDiagram.Edge>();
          verticesToEdges.Add(e.b, vertexEdgesB);
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
      foreach (var a in voronoi.Edges)
      {
        foreach (var b in voronoi.Edges)
        {
          if (a.a.Position == b.a.Position && a.b.Position == b.b.Position)
            continue;

          HasInvalidIntersection(a, b).ShouldBe(false);
        }
      }
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
        () => fortune.GenerateDiagram(new HashSet<Vector2>(), new Vector4(0.0f, 0.0f, 1.0f, 1.0f)),
        "Points contained no items");
    }

    /// <summary>
    /// Checks whether two edges intersect (beyond just touching at one vertex, which is allowed)
    /// </summary>
    /// <param name="a">Vertex a</param>
    /// <param name="b">Vertex b</param>
    /// <returns>Whether they intersect</returns>
    private static bool HasInvalidIntersection(VoronoiDiagram.Edge a, VoronoiDiagram.Edge b)
    {
      var a0 = new Point3d((double)a.a.Position.X, (double)a.a.Position.Y, 0.0);
      var a1 = new Point3d((double)a.b.Position.X, (double)a.b.Position.Y, 0.0);
      var b0 = new Point3d((double)b.a.Position.X, (double)b.a.Position.Y, 0.0);
      var b1 = new Point3d((double)b.b.Position.X, (double)b.b.Position.Y, 0.0);

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