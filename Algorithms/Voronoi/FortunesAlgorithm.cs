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
      /// Create a new SiteEvent
      /// </summary>
      /// <param name="position">The position of the event</param>
      public SiteEvent(Vector2 position)
      {
        Position = position;
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
      /// Create a new HalfEdge
      /// </summary>
      /// <param name="start">The origin point</param>
      /// <param name="direction">The direction</param>
      public HalfEdge(Vector2 start, Vector2 direction)
      {
        Start = start;
        Direction = direction;
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
      public Arc(Vector2 focus)
      {
        Focus = focus;
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
      /// Create a new CompletedEdge
      /// </summary>
      /// <param name="start">The start point</param>
      /// <param name="end">The end point</param>
      public CompletedEdge(Vector2 start, Vector2 end)
      {
        Start = start;
        End = end;
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
    public VoronoiDiagram GenerateDiagram(ISet<Vector2> points, Vector4 extents)
    {
      if (!points.Any())
        throw new ArgumentException("Points contained no items");

      // Build queue of site events
      var siteEvents = points.Select(x => (new SiteEvent(x) as IEvent, x.Y));
      var events = new PriorityQueue<IEvent, float>(siteEvents);

      // Create beach line, the list in inefficient but it seems fast enough for now. The annoying thing about it is
      // that to find a specific arc again we need to store an ID, but we only do it once so that's fine.
      var beachLine = new List<IBeachLineItem>();

      // The completed edges we accumulate as we prune the beach line
      var completedEdges = new List<CompletedEdge>();

      // Handle first event manually by adding the arc to the beachline
      events.TryDequeue(out var firstEvent, out _);
      beachLine.Add(new Arc((firstEvent as SiteEvent).Position));

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

          // Edge case: not too important but this edge might actually (and is likely to) be parallel to and connected
          // to the last one we added, as we grow two edges out away from each other. In that case, we can actually just
          // add it on to that one instead, and end up with a more optimal graph.
          if (completedEdges.Any() && TryMergeAdjacentEdges(completedEdges.Last(), extendedEdge, out var mergedEdge))
          {
            completedEdges[completedEdges.Count - 1] = mergedEdge;
            continue;
          }

          // Finally, just add this edge if that edge case didn't apply. Again, we can just do this anyway and end up
          // with a slightly less optimal graph.
          completedEdges.Add(extendedEdge);
        }
      }
      beachLine.Clear();

      // Draw final debug view
      if (_debugDraw)
        DrawBeachLine("diagram.png", (int)extents.Z, (int)extents.W, 0, points, beachLine, completedEdges);

      // Convert to a VoronoiDiagram and return
      return ConvertToResult(completedEdges, extents);
    }

    /// <summary>
    /// Try and merge two adjacent edges if they are parallel and share a vertex
    /// </summary>
    /// <param name="a">The first edge</param>
    /// <param name="b">The second edge</param>
    /// <param name="c">The output edge</param>
    /// <returns>Whether the edges could be merged</returns>
    private static bool TryMergeAdjacentEdges(CompletedEdge a, CompletedEdge b, out CompletedEdge c)
    {
      // Check if the edges actually share a vertex
      if (b.Start != a.Start && b.End != a.Start &&
        b.Start != a.End && b.End != a.End)
      {
        c = new CompletedEdge();
        return false;
      }

      // Unfortunate that we have to do this vector normalizes but we don't do it that often...
      var aDir = Vector2.Normalize(a.End - a.Start);
      var bDir = Vector2.Normalize(b.End - b.Start);
      var dotProduct = Vector2.Dot(aDir, bDir);

      // If the direction of the two vectors is the same or opposite, the lines are parallel
      if (!Comparison.ApproxEquals(dotProduct, 1.0f) && !Comparison.ApproxEquals(dotProduct, -1.0f))
      {
        c = new CompletedEdge();
        return false;
      }

      // Now work out which edge is shared, and replace that one
      if (a.Start == b.Start)
        a.Start = b.End;
      else if (a.Start == b.End)
        a.Start = b.Start;
      else if (a.End == b.Start)
        a.End = b.Start;
      else if (a.End == b.End)
        a.End = b.End;

      c = a;
      return true;
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
        // Create new arcs (we reuse arcAbove as the left arc)
        var newArc = new Arc(siteEvent.Position);
        var rightArc = new Arc(arcAbove.Focus);

        // Create new edges
        var edgeStart = new Vector2(siteEvent.Position.X, intersectionY);
        var focusDir = Vector2.Normalize(newArc.Focus - arcAbove.Focus);
        // Rotate the direction between the focuses to get the direction of an edge that divides them
        var edgeDir = new Vector2(focusDir.Y, -focusDir.X);

        var leftEdge = new HalfEdge(edgeStart, -edgeDir);
        var rightEdge = new HalfEdge(edgeStart, edgeDir);

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
        var newArc = new Arc(siteEvent.Position);

        // Place a single new edge in between the previous arc and this arc
        var edgeStart = new Vector2(siteEvent.Position.X * 0.5f + arcAbove.Focus.X * 0.5f, extents.Y);
        var edgeDir = new Vector2(0.0f, 1.0f);
        var newEdge = new HalfEdge(edgeStart, edgeDir);

        beachLine.Insert(++insertPos, newEdge);
        beachLine.Insert(++insertPos, newArc);
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
      var edgeA = ClampEdge(new CompletedEdge(leftEdge.Start, vertexEvent.IntersectionPoint), extents);
      var edgeB = ClampEdge(new CompletedEdge(vertexEvent.IntersectionPoint, rightEdge.Start), extents);

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
      beachLine.Insert(i - 1, new HalfEdge(vertexEvent.IntersectionPoint, newEdgeDirection));

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
    /// <param name="extents">The extents</param>
    /// <returns>The generated Voronoi Diagram</returns>
    private static VoronoiDiagram ConvertToResult(List<CompletedEdge> completedEdges, Vector4 extents)
    {
      var vertices = new Dictionary<Vector2, VoronoiDiagram.Vertex>();
      var edges = new List<VoronoiDiagram.Edge>();

      // Add corners as vertices, we can connect them later
      var v0 = new VoronoiDiagram.Vertex { Position = new Vector2(extents.X, extents.Y) };
      var v1 = new VoronoiDiagram.Vertex { Position = new Vector2(extents.Z, extents.Y) };
      var v2 = new VoronoiDiagram.Vertex { Position = new Vector2(extents.Z, extents.W) };
      var v3 = new VoronoiDiagram.Vertex { Position = new Vector2(extents.X, extents.W) };

      vertices.Add(v0.Position, v0);
      vertices.Add(v1.Position, v1);
      vertices.Add(v2.Position, v2);
      vertices.Add(v3.Position, v3);

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
        if (!vertices.TryGetValue(edge.Start, out vertexA))
        {
          vertexA = new VoronoiDiagram.Vertex { Position = edge.Start };
          vertices.Add(vertexA.Position, vertexA);

          // If it's on one of the edges, add it to edgeVertices
          if (vertexA.Position.X == extents.X || vertexA.Position.X == extents.Z ||
            vertexA.Position.Y == extents.Y || vertexA.Position.Y == extents.W)
          {
            edgeVertices.Add(vertexA);
          }
        }

        // Get or create vertex B
        if (!vertices.TryGetValue(edge.End, out vertexB))
        {
          vertexB = new VoronoiDiagram.Vertex { Position = edge.End };
          vertices.Add(vertexB.Position, vertexB);

          // If it's on one of the edges, add it to edgeVertices
          if (vertexA.Position.X == extents.X || vertexA.Position.X == extents.Z ||
            vertexA.Position.Y == extents.Y || vertexA.Position.Y == extents.W)
          {
            edgeVertices.Add(vertexA);
          }
        }

        // Create edge (will never be a duplicate afaik)
        var voronoiEdge = new VoronoiDiagram.Edge { a = vertexA, b = vertexB };
        edges.Add(voronoiEdge);

        // Connect edges and vertices
        vertexA.Edges.Add(voronoiEdge);
        vertexB.Edges.Add(voronoiEdge);
      }

      // Connect edge vertices on each of the four edges
      var connectVertices = (IEnumerable<VoronoiDiagram.Vertex> verticesToConnect) =>
      {
        foreach (var pair in verticesToConnect.Zip(verticesToConnect.Skip(1)))
        {
          var newEdge = new VoronoiDiagram.Edge { a = pair.First, b = pair.Second };
          edges.Add(newEdge);
          newEdge.a.Edges.Add(newEdge);
          newEdge.b.Edges.Add(newEdge);
        }
      };

      // To connect the edges, we filter down to just the vertices on one edge, and then order them in the other axis.
      // We can then just connect them all together in a chain.
      connectVertices(vertices.Values.Where(m => m.Position.Y == extents.Y).OrderBy(m => m.Position.X));
      connectVertices(vertices.Values.Where(m => m.Position.X == extents.X).OrderBy(m => m.Position.Y));
      connectVertices(vertices.Values.Where(m => m.Position.X == extents.Z).OrderBy(m => m.Position.Y));
      connectVertices(vertices.Values.Where(m => m.Position.Y == extents.W).OrderBy(m => m.Position.X));

      return new VoronoiDiagram { Vertices = vertices.Select(x => x.Value).ToArray(), Edges = edges.ToArray() };
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

      return new CompletedEdge(start, end);
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

            if (TryIntersectArcHalfEdge(curArc, edgeBefore, sweepPos, out var prevIntersection))
              arcMin = prevIntersection.X;
          }

          // If this isn't the last arc, there'll be an edge after that we can use to get arcMax
          float arcMax = float.MaxValue;
          if (curPos + 1 < beachLine.Count)
          {
            var edgeAfter = beachLine[curPos + 1] as HalfEdge;
            Debug.Assert(edgeAfter != null);

            if (TryIntersectArcHalfEdge(curArc, edgeAfter, sweepPos, out var nextIntersection))
              arcMax = nextIntersection.X;
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
  }
}
