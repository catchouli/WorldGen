using Algorithms.Voronoi;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Numerics;

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
        () => fortune.GenerateDiagram(Array.Empty<Vector2>(), new Vector4(0.0f, 0.0f, 1.0f, 1.0f)),
        "Points contained no items");
    }
  }
}