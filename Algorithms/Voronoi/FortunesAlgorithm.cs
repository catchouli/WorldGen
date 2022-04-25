using System;
using System.Diagnostics;
using System.Numerics;
using Utils;

namespace Algorithms.Voronoi
{
  /// <summary>
  /// Voronoi diagram generation using Fortune's Algorithm
  /// </summary>
  public partial class FortunesAlgorithm : IVoronoiAlgorithm
  {
    /// <summary>
    /// Tag interface for an event
    /// </summary>
    private interface IEvent { }

    /// <summary>
    /// A site event
    /// </summary>
    private class SiteEvent : IEvent
    {
      /// <summary>
      /// Position of the event
      /// </summary>
      public Vector2 Position;

      /// <summary>
      /// Index of the site, used for building polygons
      /// </summary>
      public int Index;

      /// <summary>
      /// Create a new SiteEvent
      /// </summary>
      /// <param name="position">The position of the event</param>
      /// <param name="index">Index of the site</param>
      public SiteEvent(Vector2 position, int index)
      {
        Position = position;
        Index = index;
      }
    }

    /// <summary>
    /// A vertex/circle event
    /// </summary>
    private class VertexEvent : IEvent
    {
      /// <summary>
      /// The sweep pos the event occurs at
      /// </summary>
      public float SweepPos;

      /// <summary>
      /// The intersection point of the vertices
      /// </summary>
      public Vector2 IntersectionPoint;

      /// <summary>
      /// The arc ID (because the beach line indexes change this was the best way to reference a given arc)
      /// </summary>
      public string ArcId;

      /// <summary>
      /// Whether the event is valid
      /// </summary>
      public bool IsValid = true;

      /// <summary>
      /// Create a new VertexEvent
      /// </summary>
      /// <param name="sweepPos">The sweep pos</param>
      /// <param name="intersectionPoint">The vertex intersection point</param>
      /// <param name="arcId">The arc id</param>
      public VertexEvent(float sweepPos, Vector2 intersectionPoint, string arcId)
      {
        SweepPos = sweepPos;
        IntersectionPoint = intersectionPoint;
        ArcId = arcId;
      }
    }

    /// <summary>
    /// Tag interface for beach line items
    /// </summary>
    private interface IBeachLineItem { }

    /// <summary>
    /// A half edge in the beach line
    /// </summary>
    private class HalfEdge : IBeachLineItem
    {
      /// <summary>
      /// The start point
      /// </summary>
      public Vector2 Start;

      /// <summary>
      /// The direction
      /// </summary>
      public Vector2 Direction;

      /// <summary>
      /// The site that created this half edge
      /// </summary>
      public int SiteA;

      /// <summary>
      /// The other site that created this half edge
      /// </summary>
      public int SiteB;

      /// <summary>
      /// Create a new HalfEdge
      /// </summary>
      /// <param name="start">The origin point</param>
      /// <param name="direction">The direction</param>
      /// <param name="siteA">The site that created this half edge</param>
      /// <param name="siteB">The other site that created this half edge</param>
      public HalfEdge(Vector2 start, Vector2 direction, int siteA, int siteB)
      {
        Start = start;
        Direction = direction;
        SiteA = siteA;
        SiteB = siteB;
      }
    }

    /// <summary>
    /// An arc in the beach line
    /// </summary>
    private class Arc : IBeachLineItem
    {
      /// <summary>
      /// The focus of the parabola
      /// </summary>
      public Vector2 Focus;

      /// <summary>
      /// The site index that this arc belongs to
      /// </summary>
      public int SiteIndex;

      /// <summary>
      /// The vertex event associated with this arc
      /// </summary>
      public VertexEvent VertexEvent;

      /// <summary>
      /// The id of the arc (for finding it again in the beachline)
      /// </summary>
      /// TODO: use a tree for this instead so it isn't necessary anymore
      public string Id = Guid.NewGuid().ToString();

      /// <summary>
      /// Create a new arc
      /// </summary>
      /// <param name="focus">The focus point</param>
      /// <param name="siteIndex">The site index that this arc belongs to</param>
      public Arc(Vector2 focus, int siteIndex)
      {
        Focus = focus;
        SiteIndex = siteIndex;
      }
    }

    /// <summary>
    /// A completed edge
    /// </summary>
    private struct CompletedEdge
    {
      /// <summary>
      /// The start of the edge
      /// </summary>
      public Vector2 Start;

      /// <summary>
      /// The end of the edge
      /// </summary>
      public Vector2 End;

      /// <summary>
      /// One site that this edge bounds
      /// </summary>
      public int SiteA;

      /// <summary>
      /// The other site that this edge bounds
      /// </summary>
      public int SiteB;

      /// <summary>
      /// Create a new CompletedEdge
      /// </summary>
      /// <param name="start">The start point</param>
      /// <param name="end">The end point</param>
      public CompletedEdge(Vector2 start, Vector2 end, int siteA, int siteB)
      {
        Start = start;
        End = end;
        SiteA = siteA;
        SiteB = siteB;
      }
    }

    /// <summary>
    /// Whether to enable debug drawing
    /// </summary>
    private bool _debugDraw = false;

    /// <summary>
    /// Create a new instance of FortunesAlogrithm
    /// </summary>
    /// <param name="debugDraw">Whether to enable debug drawing</param>
    public FortunesAlgorithm(bool debugDraw)
    {
      _debugDraw = debugDraw;
    }

    /// <inheritdoc/>
    public VoronoiDiagram GenerateDiagram(IList<Vector2> sites, Vector4 extents)
    {
      if (!sites.Any())
        throw new ArgumentException("Points contained no items");

      // Build queue of site events
      var siteEvents = sites.Select((x, i) => (new SiteEvent(x, i) as IEvent, x.Y));
      var events = new PriorityQueue<IEvent, float>(siteEvents);

      // Create beach line, the list in inefficient but it seems fast enough for now. The annoying thing about it is
      // that to find a specific arc again we need to store an ID, but we only do it once so that's fine.
      var beachLine = new List<IBeachLineItem>();

      // The completed edges we accumulate as we prune the beach line
      var completedEdges = new List<CompletedEdge>();

      // Handle first event manually by adding the arc to the beachline
      events.TryDequeue(out var firstEvent, out _);
      var firstSiteEvent = firstEvent as SiteEvent;
      beachLine.Add(new Arc(firstSiteEvent.Position, firstSiteEvent.Index));

      // Handle the remaining events
      while (events.TryDequeue(out var evt, out _))
      {
        // Handle site events
        if (evt is SiteEvent siteEvent)
        {
          var (leftVertexEvent, rightVertexEvent) = HandleSiteEvent(beachLine, siteEvent, extents);

          if (leftVertexEvent != null)
            events.Enqueue(leftVertexEvent, ((VertexEvent)leftVertexEvent).SweepPos);
          if (rightVertexEvent != null)
            events.Enqueue(rightVertexEvent, ((VertexEvent)rightVertexEvent).SweepPos);
        }
        // Handle vertex events
        else if (evt is VertexEvent vertexEvent)
        {
          if (!vertexEvent.IsValid)
            continue;

          var (leftVertexEvent, rightVertexEvent) = HandleVertexEvent(beachLine, vertexEvent, completedEdges, extents);

          if (leftVertexEvent != null)
            events.Enqueue(leftVertexEvent, ((VertexEvent)leftVertexEvent).SweepPos);
          if (rightVertexEvent != null)
            events.Enqueue(rightVertexEvent, ((VertexEvent)rightVertexEvent).SweepPos);
        }
      }

      // Finish the rest of our edges that are still in the beach line (ExtendHalfEdge also makes sure they're clamped)
      for (int i = 0; i < beachLine.Count; ++i)
      {
        if (beachLine[i] is HalfEdge edge)
        {
          // Extend edge to extents, and filter out 0-length edges
          var extendedEdge = ExtendHalfEdge(edge, extents);
          if (extendedEdge.Start == extendedEdge.End)
            continue;

          completedEdges.Add(extendedEdge);
        }
      }
      beachLine.Clear();

      // Draw final debug view
      if (_debugDraw)
        DrawBeachLine("diagram.png", (int)extents.Z, (int)extents.W, 0, sites, beachLine, completedEdges);

      // Convert to a VoronoiDiagram and return
      return ConvertToResult(completedEdges, sites, extents);
    }

    /// <summary>
    /// Handle a SiteEvent
    /// </summary>
    /// <param name="beachLine">The beach line</param>
    /// <param name="siteEvent">The site event</param>
    /// <returns>Any generated vertex events</returns>
    private static (IEvent, IEvent) HandleSiteEvent(List<IBeachLineItem> beachLine, SiteEvent siteEvent,
      Vector4 extents)
    {
      // Get the arc above this site event
      var arcAbovePos = FindArcAbove(beachLine, siteEvent.Position.X, siteEvent.Position.Y);
      var arcAbove = beachLine[arcAbovePos] as Arc;
      Debug.Assert(arcAbove != null);

      // Find intersection with arc above. If this fails then we're probably doing the first few points and they've all
      // been on the same sweep line, so there are no actual parabolas yet to intersect them with. In that case,
      // straightUpwards gets set to true and we make any edges between parabolas extend straight upwards.
      int insertPos = arcAbovePos;
      if (TryGetParabolaY(siteEvent.Position.X, arcAbove.Focus, siteEvent.Position.Y, out var intersectionY))
      {
        // Create new arcs
        var newArc = new Arc(siteEvent.Position, siteEvent.Index);
        var rightArc = new Arc(arcAbove.Focus, arcAbove.SiteIndex);

        // Create new edges
        var edgeStart = new Vector2(siteEvent.Position.X, intersectionY);
        var focusDir = Vector2.Normalize(newArc.Focus - arcAbove.Focus);
        // Rotate the direction between the focuses to get the direction of an edge that divides them
        var edgeDir = new Vector2(focusDir.Y, -focusDir.X);

        var leftEdge = new HalfEdge(edgeStart, -edgeDir, newArc.SiteIndex, arcAbove.SiteIndex);
        var rightEdge = new HalfEdge(edgeStart, edgeDir, newArc.SiteIndex, arcAbove.SiteIndex);

        // Insert (after arcAbove, which is the left side of the split arc) the sequence:
        // leftSplit, leftEdge, newArc, rightEdge, rightSplit
        beachLine.Insert(++insertPos, leftEdge);
        beachLine.Insert(++insertPos, newArc);
        beachLine.Insert(++insertPos, rightEdge);
        beachLine.Insert(++insertPos, rightArc);
      }
      else
      {
        // Create new arcs (we reuse arcAbove as the left arc)
        var newArc = new Arc(siteEvent.Position, siteEvent.Index);

        // Place a single new edge in between the previous arc and this arc
        var edgeStart = new Vector2(siteEvent.Position.X * 0.5f + arcAbove.Focus.X * 0.5f, extents.Y);
        var edgeDir = new Vector2(0.0f, 1.0f);
        var newEdge = new HalfEdge(edgeStart, edgeDir, newArc.SiteIndex, arcAbove.SiteIndex);

        beachLine.Insert(++insertPos, newEdge);
        beachLine.Insert(++insertPos, newArc);
      }

      // Since we reused arcAbove as leftArc we should reset its VertexEvent if there is one
      if (arcAbove.VertexEvent != null)
      {
        arcAbove.Id = Guid.NewGuid().ToString();
        arcAbove.VertexEvent.IsValid = false;
        arcAbove.VertexEvent = null;
      }

      // Return new vertex events
      return (CreateVertexEvent(beachLine, arcAbovePos), CreateVertexEvent(beachLine, insertPos));
    }

    /// <summary>
    /// Handle a vertex event, removing an arc from the beach line
    /// </summary>
    /// <param name="beachLine">The beach line</param>
    /// <param name="vertexEvent">The vertex event</param>
    /// <param name="completedEdges">The completed edges list to write to</param>
    /// <param name="extents">The extents for completed edges</param>
    /// <returns>Any newly generated vertex events, or null if there wasn't one</returns>
    private static (IEvent, IEvent) HandleVertexEvent(List<IBeachLineItem> beachLine, VertexEvent vertexEvent,
      List<CompletedEdge> completedEdges, Vector4 extents)
    {
      // Find the arc being squeezed
      int i;
      Arc squeezedArc;

      for (i = 0; i < beachLine.Count; ++i)
      {
        squeezedArc = beachLine[i] as Arc;
        if (squeezedArc != null && squeezedArc.Id == vertexEvent.ArcId)
          break;
      }

      // I found that sometimes it's already been removed, maybe this is a bug
      // TODO: consider handling this properly by removing events that don't need to be done anymore
      if (i == beachLine.Count)
        return (null, null);

      // Get left and right edge around arc
      var leftEdge = i > 0 ? beachLine[i - 1] as HalfEdge : null;
      Debug.Assert(leftEdge != null);
      var rightEdge = i + 1 < beachLine.Count ? beachLine[i + 1] as HalfEdge : null;
      Debug.Assert(rightEdge != null);

      // Get left and right arc around those edges
      var leftArc = i - 2 >= 0 ? beachLine[i - 2] as Arc : null;
      Debug.Assert(leftArc != null);
      var rightArc = i + 2 < beachLine.Count ? beachLine[i + 2] as Arc : null;
      Debug.Assert(rightArc != null);

      // Create new edges
      var edgeA = ClampEdge(new CompletedEdge(leftEdge.Start, vertexEvent.IntersectionPoint, leftEdge.SiteA,
        leftEdge.SiteB), extents);
      var edgeB = ClampEdge(new CompletedEdge(vertexEvent.IntersectionPoint, rightEdge.Start, rightEdge.SiteA,
        rightEdge.SiteB), extents);

      // Filter out 0-length edges
      if (edgeA.Start != edgeA.End)
        completedEdges.Add(edgeA);
      if (edgeB.Start != edgeB.End)
        completedEdges.Add(edgeB);

      // Work out direction between the (now) adjacent arcs, and the rotate it to get the new edge direction
      var arcDir = Vector2.Normalize(leftArc.Focus - rightArc.Focus);
      var newEdgeDirection = new Vector2(arcDir.Y, -arcDir.X);

      // Remove the sequence (el, a, er), at this point the arc has been squeezed and the edges have been inserted
      // into completedEdges. We then replace it with a new edge between the two arcs that are left adjacent.
      beachLine.RemoveRange(i - 1, 3);
      beachLine.Insert(i - 1, new HalfEdge(vertexEvent.IntersectionPoint, newEdgeDirection, leftArc.SiteIndex, rightArc.SiteIndex));

      // Return new vertex events
      return (CreateVertexEvent(beachLine, i - 2), CreateVertexEvent(beachLine, i));
    }

    /// <summary>
    /// Create a new vertex event for a given arc, returns null if none can be created
    /// </summary>
    /// <param name="beachLine">The beach line</param>
    /// <param name="arcIndex">The index of the arc</param>
    /// <returns>The generated vertex event or null</returns>
    private static IEvent CreateVertexEvent(List<IBeachLineItem> beachLine, int arcIndex)
    {
      var arc = beachLine[arcIndex] as Arc;
      Debug.Assert(arc != null);

      var leftEdge = arcIndex > 0 ? beachLine[arcIndex - 1] as HalfEdge : null;
      var rightEdge = arcIndex + 1 < beachLine.Count ? beachLine[arcIndex + 1] as HalfEdge : null;

      if (leftEdge == null || rightEdge == null)
        return null;

      if (!TryIntersectHalfEdges(leftEdge, rightEdge, out var intersection))
        return null;

      var circleCentreOffset = arc.Focus - intersection;
      float circleRadius = circleCentreOffset.Length();
      float vertexEventY = intersection.Y + circleRadius;

      // Add new vertex event
      arc.VertexEvent = new VertexEvent(vertexEventY, intersection, arc.Id);
      return arc.VertexEvent;
    }

    /// <summary>
    /// Convert our generated edges into the result type VoronoiDiagram
    /// </summary>
    /// <param name="completedEdges">The generated edges</param>
    /// <param name="inputSites">The input sites</param>
    /// <param name="extents">The extents</param>
    /// <returns>The generated Voronoi Diagram</returns>
    private static VoronoiDiagram ConvertToResult(List<CompletedEdge> completedEdges, IList<Vector2> inputSites,
      Vector4 extents)
    {
      var sites = inputSites.Select(x => new VoronoiDiagram.Site { Position = x }).ToList();
      var voronoiVertices = new Dictionary<Vector2, VoronoiDiagram.Vertex>();
      var edges = new List<VoronoiDiagram.Edge>();

      // Add corners as vertices, we can connect them later
      var v0 = new VoronoiDiagram.Vertex { Position = new Vector2(extents.X, extents.Y) };
      var v1 = new VoronoiDiagram.Vertex { Position = new Vector2(extents.Z, extents.Y) };
      var v2 = new VoronoiDiagram.Vertex { Position = new Vector2(extents.Z, extents.W) };
      var v3 = new VoronoiDiagram.Vertex { Position = new Vector2(extents.X, extents.W) };

      voronoiVertices.Add(v0.Position, v0);
      voronoiVertices.Add(v1.Position, v1);
      voronoiVertices.Add(v2.Position, v2);
      voronoiVertices.Add(v3.Position, v3);

      // The list of edge vertices we're going to connect up at the end
      var edgeVertices = new List<VoronoiDiagram.Vertex>(new[] { v0, v1, v2, v3 });

      // Add each edge and vertices to the diagram
      foreach (var edge in completedEdges)
      {
        // Filter out the 0-length edges that sometimes occur when they're clipped
        if (edge.Start == edge.End)
          continue;

        VoronoiDiagram.Vertex vertexA, vertexB;

        // Get or create vertex A
        if (!voronoiVertices.TryGetValue(edge.Start, out vertexA))
        {
          vertexA = new VoronoiDiagram.Vertex { Position = edge.Start };
          voronoiVertices.Add(vertexA.Position, vertexA);

          // If it's on one of the edges, add it to edgeVertices
          if (vertexA.Position.X == extents.X || vertexA.Position.X == extents.Z ||
            vertexA.Position.Y == extents.Y || vertexA.Position.Y == extents.W)
          {
            edgeVertices.Add(vertexA);
          }
        }

        // Get or create vertex B
        if (!voronoiVertices.TryGetValue(edge.End, out vertexB))
        {
          vertexB = new VoronoiDiagram.Vertex { Position = edge.End };
          voronoiVertices.Add(vertexB.Position, vertexB);

          // If it's on one of the edges, add it to edgeVertices
          if (vertexA.Position.X == extents.X || vertexA.Position.X == extents.Z ||
            vertexA.Position.Y == extents.Y || vertexA.Position.Y == extents.W)
          {
            edgeVertices.Add(vertexA);
          }
        }

        // Create edge (will never be a duplicate afaik)
        var voronoiEdge = new VoronoiDiagram.Edge {
          CornerA = vertexA, CornerB = vertexB,
          SiteA = sites[edge.SiteA], SiteB = sites[edge.SiteB]
        };
        edges.Add(voronoiEdge);

        // Connect edges and vertices
        vertexA.Edges.Add(voronoiEdge);
        vertexB.Edges.Add(voronoiEdge);
      }

      // Remove redundant edges created because we grow edges out from an intersection point
      voronoiVertices = MergeRedundantEdges(voronoiVertices, edges);

      // Add all the edges for each site to it so we can find the polygon easily
      foreach (var edge in edges)
      {
        edge.SiteA.Edges.Add(edge);
        edge.SiteB.Edges.Add(edge);
      }

      // Connect edge vertices on each of the four edges
      var connectVertices = (IEnumerable<VoronoiDiagram.Vertex> verticesToConnect) =>
      {
        // Create a new edge between each pair of vertices, adding it to the appropriate site
        foreach (var (a, b) in verticesToConnect.Zip(verticesToConnect.Skip(1)))
        {
          // Create new edge
          var newEdge = new VoronoiDiagram.Edge { CornerA = a, CornerB = b };
          edges.Add(newEdge);
          newEdge.CornerA.Edges.Add(newEdge);
          newEdge.CornerB.Edges.Add(newEdge);

          // Find list of candidate sites from connected vertices, and then find the nearest one
          var midpoint = a.Position * 0.5f + b.Position * 0.5f;
          var nearestSite = b.Edges.SelectMany(e => new[] { e.SiteA, e.SiteB })
          .Concat(a.Edges.SelectMany(e => new[] { e.SiteA, e.SiteB }))
          .OrderBy(site => site != null ? Vector2.DistanceSquared(midpoint, site.Position) : float.MaxValue)
          .FirstOrDefault();
          nearestSite?.Edges.Add(newEdge);
        }
      };

      // To connect the edges, we filter down to just the vertices on one edge, and then order them in the other axis.
      // We can then just connect them all together in a chain.
      connectVertices(voronoiVertices.Values.Where(m => m.Position.Y == extents.Y).OrderBy(m => m.Position.X));
      connectVertices(voronoiVertices.Values.Where(m => m.Position.X == extents.X).OrderBy(m => m.Position.Y));
      connectVertices(voronoiVertices.Values.Where(m => m.Position.X == extents.Z).OrderBy(m => m.Position.Y));
      connectVertices(voronoiVertices.Values.Where(m => m.Position.Y == extents.W).OrderBy(m => m.Position.X));

      // Order the edges for each site so that we have coherent polygons, just as a convenience
      foreach (var site in sites)
      {
        if (!site.Edges.Any())
          continue;

        // Get the first edge and the remaining edges
        var firstEdge = site.Edges.First();
        var siteEdges = site.Edges.Skip(1).ToHashSet();

        // Clear the original list so we can insert it in a new order
        site.Edges.Clear();
        site.Edges.Add(firstEdge);

        // Start from firstEdge and find the next edge repeatedly until we get back to firstEdge
        var startVertex = firstEdge.CornerA;
        var curVertex = firstEdge.CornerB;

        while (siteEdges.Any())
        {
          try
          {
            // Find next connecting edge
            var nextEdge = siteEdges.Single(e => e.CornerA.Position == curVertex.Position
              || e.CornerB.Position == curVertex.Position);

            // Remove this edge and add it to the list
            siteEdges.Remove(nextEdge);
            site.Edges.Add(nextEdge);

            // Work out which vertex is the next one to search from
            curVertex = (nextEdge.CornerA.Position != curVertex.Position ? nextEdge.CornerA : nextEdge.CornerB);
          }
          catch (InvalidOperationException e)
          {
            throw new InternalErrorException($"Failed to find next edge for vertex {curVertex}", e);
          }
        }

        // At this point we should've gone around in a loop so startVertex should == curVertex
        if (startVertex != curVertex)
          throw new InternalErrorException("Failed to find polygon loop for site");
      }

      return new VoronoiDiagram
      {
        Vertices = voronoiVertices.Select(x => x.Value).ToArray(),
        Sites = sites.ToArray(),
        Edges = edges.ToArray()
      };
    }

    /// <summary>
    /// Merge redundant edges in the voronoi diagram that we get because the algorithm grows edges away from the
    /// center of parabola intersections
    /// </summary>
    /// <param name="vertices">The vertices</param>
    private static Dictionary<Vector2, VoronoiDiagram.Vertex> MergeRedundantEdges(
      Dictionary<Vector2, VoronoiDiagram.Vertex> vertices, List<VoronoiDiagram.Edge> edges)
    {
      var ret = new Dictionary<Vector2, VoronoiDiagram.Vertex>();

      foreach (var vertex in vertices.Values)
      {
        // If a vertex has exactly two edges and they're parallel, it's redundant and the edges can be merged
        if (vertex.Edges.Count == 2 && AreParallel(vertex.Edges[0], vertex.Edges[1]))
        {
          var edgeA = vertex.Edges[0];
          var edgeB = vertex.Edges[1];

          // Merge vertex.Edges[1] into vertex.Edges[0]
          edgeA.CornerA = (edgeA.CornerA.Position != vertex.Position ? edgeA.CornerA : edgeA.CornerB);
          edgeA.CornerB = (edgeB.CornerA.Position != vertex.Position ? edgeB.CornerA : edgeB.CornerB);
          edgeA.CornerB.Edges.Add(edgeA);

          // And then delete vertex.Edges[1] from the graph completely
          edgeB.CornerA.Edges.Remove(edgeB);
          edgeB.CornerB.Edges.Remove(edgeB);
          edges.Remove(edgeB);
        }
        // And if it's not redundant, add it to the output set
        else
        {
          ret.Add(vertex.Position, vertex);
        }
      }

      return ret;
    }

    /// <summary>
    /// Check whether two edges are parallel
    /// </summary>
    /// <param name="a">The first edge</param>
    /// <param name="b">The second edge</param>
    /// <returns>Whether they were parallel</returns>
    private static bool AreParallel(VoronoiDiagram.Edge a, VoronoiDiagram.Edge b)
    {
      // Construct unit vectors for both edges (sigh)
      var dA = Vector2.Normalize(a.CornerA.Position - a.CornerB.Position);
      var dB = Vector2.Normalize(b.CornerA.Position - b.CornerB.Position);

      // Calculate angle between vectors to find out if they're parallel
      float dot = dA.X * dB.X + dA.Y * dB.Y;
      float det = dA.X * dB.Y - dA.Y * dB.X;
      float angle = MathF.Atan2(det, dot);

      return Comparison.ApproxEquals(dot, 1.0f) || Comparison.ApproxEquals(dot, -1.0f);
    }

    /// <summary>
    /// Extend a half edge out to the extents, also clamps the start point to the extents
    /// </summary>
    /// <param name="edge">The half edge</param>
    /// <param name="extents">The extents</param>
    /// <returns>The extended and clamped edge</returns>
    private static CompletedEdge ExtendHalfEdge(HalfEdge edge, Vector4 extents)
    {
      // Work out line equation
      // y = mx + c
      float m = edge.Direction.Y / edge.Direction.X;
      float c = edge.Start.Y - m * edge.Start.X;

      // Clamp start point to line
      var start = ClampPointOnLine(extents, edge.Start, m, c);

      // Now we need to work out the end point extended to the extents
      Vector2 end = start;

      // Extend line to X extent
      float extentX = edge.Direction.X > 0.0f ? extents.Z : extents.X;
      // y = mx + c
      float targetY = m * extentX + c;

      // If that point is in bounds we can extend the edge to it
      if (extents.Y <= targetY && targetY <= extents.W)
      {
        end.X = extentX;
        end.Y = targetY;
      }
      // If that calculated point was outside the bounds, we should clamp to the other extent instead
      else
      {
        end.Y = edge.Direction.Y > 0.0f ? extents.W : extents.Y;
        // x = (y - c) / m
        end.X = (end.Y - c) / m;
      }

      return new CompletedEdge(start, end, edge.SiteA, edge.SiteB);
    }

    /// <summary>
    /// Clamp the given completed edge
    /// </summary>
    /// <param name="edge">The edge to clamp</param>
    /// <param name="extents">The extents to clamp it to</param>
    /// <returns>The clamped edge</returns>
    private static CompletedEdge ClampEdge(CompletedEdge edge, Vector4 extents)
    {
      Vector2 dir = Vector2.Normalize(edge.End - edge.Start);

      // Special case: if dir.x = 0, the edge is straight up, and we should just clamp the y coordinates to the extents
      if (dir.X == 0.0f)
      {
        edge.Start.Y = Math.Clamp(edge.Start.Y, extents.Y, extents.W);
        edge.End.Y = Math.Clamp(edge.End.Y, extents.Y, extents.W);
        return edge;
      }

      // y = mx + c
      // c = y - mx
      float m = dir.Y / dir.X;
      float c = edge.Start.Y - m * edge.Start.X;

      edge.Start = ClampPointOnLine(extents, edge.Start, m, c);
      edge.End = ClampPointOnLine(extents, edge.End, m, c);

      return edge;
    }

    /// <summary>
    /// Clamp a point on a line to the given extents, where the line equation is given as y = mx + c
    /// </summary>
    /// <param name="extents">The extents</param>
    /// <param name="point">The point</param>
    /// <param name="m">The gradient of the line</param>
    /// <param name="c">The constant of the line</param>
    /// <returns>The clamped point</returns>
    private static Vector2 ClampPointOnLine(Vector4 extents, Vector2 point, float m, float c)
    {
      // Clamp to X extents and calculate a new Y
      // y = mx + c
      if (point.X < extents.X)
      {
        point.X = extents.X;
        point.Y = m * extents.X + c;
      }
      else if (point.X > extents.Z)
      {
        point.X = extents.Z;
        point.Y = m * extents.Z + c;
      }

      // Clamp to Y extents and calculate a new X
      // x = (y - c) / m
      if (point.Y < extents.Y)
      {
        point.X = (extents.Y - c) / m;
        point.Y = extents.Y;
      }
      else if (point.Y > extents.W)
      {
        point.X = (extents.W - c) / m;
        point.Y = extents.W;
      }

      return point;
    }

    /// <summary>
    /// Get the Y of a parabola with the given x, focus and directrix
    /// </summary>
    /// <param name="x">The x position to query</param>
    /// <param name="focus">The focus of the parabola</param>
    /// <param name="directrixY">The directrix of the parabola</param>
    /// <param name="outY">The output Y</param>
    /// <returns>Whether the parabola intersects this x position</returns>
    private static bool TryGetParabolaY(float x, Vector2 focus, float directrixY, out float outY)
    {
      // If the focus is on the directrix, the arc is basically a straight line up instead of a parabola
      if (directrixY == focus.Y)
      {
        outY = 0.0f;
        return false;
      }

      // https://jacquesheunis.com/post/fortunes-algorithm/
      // y = (1 / 2(yf - yd)) * (x - xf)^2 + (yf + yd)/2
      float A = 1.0f / (2.0f * (focus.Y - directrixY));
      float B = x - focus.X;
      float C = (focus.Y + directrixY) / 2.0f;

      outY = A * B * B + C;
      return true;
    }

    /// <summary>
    /// Find the arc above a given X in the beach line
    /// </summary>
    /// <param name="beachLine">The beach line</param>
    /// <param name="x">The x coordinate</param>
    /// <param name="sweepPos">The sweep line Y position</param>
    /// <returns>The index of the arc in the beach line which is above the given x coordinate</returns>
    private static int FindArcAbove(List<IBeachLineItem> beachLine, float x, float sweepPos)
    {
      // An epsilon for detecting parabolas with a focus almost exactly on the sweep line and handling them specially.
      // This needs to be tuned as without it points too close together start to lose precision with the parabola's
      // coordinates. But going too far in the other direction introduces false positives which cause a similar
      // problem. If the conditions below throw an InternalErrorException then this needs to be increased, but it may
      // start introducing error itself.
      const float SweepLineEpsilon = 1e-3f;

      // Should never happen because we add an item to the beachline at the start
      if (beachLine.Count == 0)
        throw new InvalidOperationException("Beachline should never be empty");

      // Walk the beach line finding the extents of each arc in order to find out which one is above the current X pos
      for (int curPos = 0; curPos < beachLine.Count; ++curPos)
      {
        if (beachLine[curPos] is Arc curArc)
        {
          // If this is the last arc just return it
          if (curPos + 1 == beachLine.Count)
          {
            return curPos;
          }

          // If this isn't the first arc, there'll be an edge before this one that we can use to get arcMin
          float arcMin = 0;
          if (curPos > 0)
          {
            var edgeBefore = beachLine[curPos - 1] as HalfEdge;
            Debug.Assert(edgeBefore != null);

            if (Comparison.ApproxEquals(curArc.Focus.Y, sweepPos, SweepLineEpsilon))
              arcMin = curArc.Focus.X;
            else if (TryIntersectArcHalfEdge(curArc, edgeBefore, sweepPos, out var prevIntersection))
              arcMin = prevIntersection.X;
            else
              throw new InternalErrorException("(probably precision error)");
          }

          // If this isn't the last arc, there'll be an edge after that we can use to get arcMax
          float arcMax = float.MaxValue;
          if (curPos + 1 < beachLine.Count)
          {
            var edgeAfter = beachLine[curPos + 1] as HalfEdge;
            Debug.Assert(edgeAfter != null);

            if (Comparison.ApproxEquals(curArc.Focus.Y, sweepPos, SweepLineEpsilon))
              arcMax = curArc.Focus.X;
            else if (TryIntersectArcHalfEdge(curArc, edgeAfter, sweepPos, out var nextIntersection))
              arcMax = nextIntersection.X;
            else
              throw new InternalErrorException("(probably precision error)");
          }

          // If this x lies between arcMin and arcMax, this is the right arc
          if (arcMin <= x && x <= arcMax)
            return curPos;
        }
      }

      throw new InternalErrorException($"Couldn't find arc in beachline above x={x}," +
        $" beachLine.Count={beachLine.Count}");
    }

    /// <summary>
    /// Find the intersection of two half edges
    /// </summary>
    /// <param name="a">The first half edge</param>
    /// <param name="b">The second half edge</param>
    /// <param name="intersection">The output intersection</param>
    /// <returns>Whether the half edges intersect</returns>
    private static bool TryIntersectHalfEdges(HalfEdge a, HalfEdge b, out Vector2 intersection)
    {
      float dx = b.Start.X - a.Start.X;
      float dy = b.Start.Y - a.Start.Y;
      float det = b.Direction.X * a.Direction.Y - b.Direction.Y * a.Direction.X;
      float u = (dy * b.Direction.X - dx * b.Direction.Y) / det;
      float v = (dy * a.Direction.X - dx * a.Direction.Y) / det;

      if (u < 0.0f || v < 0.0f || (u == 0.0f && v == 0.0f))
      {
        intersection = Vector2.Zero;
        return false;
      }
      else
      {
        intersection = new Vector2(a.Start.X + a.Direction.X * u, a.Start.Y + a.Direction.Y * u);
        return true;
      }
    }

    /// <summary>
    /// Find the intersection of an arc with a half edge
    /// </summary>
    /// <param name="arc">The arc</param>
    /// <param name="edge">The half edge</param>
    /// <param name="directrix">The directrix</param>
    /// <param name="intersection">The (output) intersection point</param>
    /// <returns>Whether the arc intersects with the half edge</returns>
    private static bool TryIntersectArcHalfEdge(Arc arc, HalfEdge edge, float directrix, out Vector2 intersection)
    {
      // If the edge is a vertical line
      if (edge.Direction.X == 0.0f)
      {
        // If the arc is also a vertical line because the sweep line is on its focus
        if (arc.Focus.Y == directrix)
        {
          // Will get set even if we return false, because we have to set it to something anyway
          intersection = arc.Focus;
          return edge.Start.X == arc.Focus.X;
        }

        // Otherwise they definitely intersect and we can just use GetParabolaY
        if (!TryGetParabolaY(edge.Start.X, arc.Focus, directrix, out var parabolaY))
          throw new InternalErrorException();
        intersection = new Vector2(edge.Start.X, parabolaY);
        return true;
      }

      // Work out m and c in y = mx + k for the line
      float m = edge.Direction.Y / edge.Direction.X;
      // k = y - mx
      float k = edge.Start.Y - m * edge.Start.X;

      // If the arc is a vertical line because the sweep line is on its focus
      if (arc.Focus.Y == directrix)
      {
        // If the intersection is in the direction of the edge there's an intersection
        float intersectionOffset = arc.Focus.X - edge.Start.X;
        if (intersectionOffset * edge.Direction.X >= 0.0f)
        {
          intersection = new Vector2(arc.Focus.X, m * arc.Focus.X + k);
          return true;
        }
        else
        {
          intersection = Vector2.Zero;
          return false;
        }
      }

      // Now we have to solve the equations:
      // y = mx + k
      // y = ax^2 + bx + c
      // But first we need a, b, and c (we already have m and k above)
      // Our known y = (focus.y * 0.5 + directrix * 0.5)
      float knownY = 0.5f * arc.Focus.Y + 0.5f * directrix;

      // https://jacquesheunis.com/post/fortunes-algorithm/
      float a = 1.0f / (2.0f * (arc.Focus.Y - directrix));
      float b = -m - 2.0f * a * arc.Focus.X;
      float c = a * arc.Focus.X * arc.Focus.X + knownY - k;

      float discriminant = b * b - 4.0f * a * c;

      if (discriminant < 0.0f)
      {
        intersection = Vector2.Zero;
        return false;
      }

      float sqrtDisc = MathF.Sqrt(discriminant);
      float x1 = (-b + sqrtDisc) / (2.0f * a);
      float x2 = (-b - sqrtDisc) / (2.0f * a);

      float x1Offset = x1 - edge.Start.X;
      float x2Offset = x2 - edge.Start.X;
      float x1Dot = x1Offset * edge.Direction.X;
      float x2Dot = x2Offset * edge.Direction.X;

      float x;

      if (x1Dot >= 0.0f && x2Dot < 0.0f)
        x = x1;
      else if (x1Dot < 0.0f && x2Dot >= 0.0f)
        x = x2;
      else if (x1Dot >= 0.0f && x2Dot >= 0.0f)
      {
        if (x1Dot < x2Dot)
          x = x1;
        else
          x = x2;
      }
      else
      {
        if (x1Dot < x2Dot)
          x = x2;
        else
          x = x1;
      }

      if (!TryGetParabolaY(x, arc.Focus, directrix, out var outY))
        throw new InternalErrorException();

      intersection = new Vector2(x, outY);
      return true;
    }

    /// <inheritdoc/>
    public VoronoiDiagram Relax(in VoronoiDiagram diagram, in Vector4 extents)
    {
      // Select averaged out positions for new sites
      var centroids = diagram.Sites.Select((site) =>
      {
        if (!site.Edges.Any())
          return site.Position;

        // Get first edge
        var firstEdge = site.Edges.First();
        var vertices = new List<Vector2> { firstEdge.CornerA.Position, firstEdge.CornerB.Position };

        foreach (var edge in site.Edges.Skip(1))
        {
          var nextVertex = (edge.CornerA.Position != vertices.Last() ? edge.CornerA : edge.CornerB);

          if (nextVertex.Position == vertices.First())
            break;
          else
            vertices.Add(nextVertex.Position);
        }

        return GetCentroid(vertices);
      });

      return GenerateDiagram(centroids.ToList(), extents);
    }

    /// <summary>
    /// Get the centroid of a non-complex polygon: https://stackoverflow.com/a/16841009
    /// (dunno if it's right tbh but it looks fine)
    /// </summary>
    /// <param name="vertices">The vertices of the polygon in order</param>
    /// <returns>The centroid</returns>
    public static Vector2 GetCentroid(IList<Vector2> vertices)
    {
      float x = 0.0f;
      float y = 0.0f;
      float area = 0.0f;
      var lastVertex = vertices.Last();

      foreach (var curVertex in vertices)
      {
        float det = curVertex.Y * lastVertex.X - curVertex.X * lastVertex.Y;

        area += det;
        x += (curVertex.X + lastVertex.X) * det;
        y += (curVertex.Y + lastVertex.Y) * det;

        lastVertex = curVertex;
      }
      area *= 3;

      return new Vector2(x / area, y / area);
    }
  }
}
