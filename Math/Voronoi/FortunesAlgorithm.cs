using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Numerics;

namespace MapGenTestApp.Voronoi
{
  /// <summary>
  /// Voronoi diagram generation using Fortune's Algorithm
  /// </summary>
  public static class FortunesAlgorithm
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
    }

    /// <summary>
    /// A circle event
    /// </summary>
    private class CircleEvent : IEvent
    {
      public float SweepPos;
      public Vector2 IntersectionPoint;
      public string ArcId;
      public bool IsValid = true;
    }

    private enum BeachLineItemType
    {
      Arc,
      Edge
    }

    private class BeachLineItem
    {
      public BeachLineItemType Type;

      public Arc Arc;
      public HalfEdge Edge;

      public string Id = Guid.NewGuid().ToString();
    }

    private struct HalfEdge
    {
      public Vector2 Start;
      public Vector2 Direction;
      public bool extendsUpwardsForever = false;
    }

    private struct Edge
    {
      public Vector2 Start;
      public Vector2 End;
    }

    private struct Arc
    {
      public Vector2 Focus;
      public CircleEvent CircleEvent;
    }

    /// <summary>
    /// Generates a Voronoi diagram/triangulation using Fortune's algorithm
    /// https://jacquesheunis.com/post/fortunes-algorithm/
    /// https://pvigier.github.io/2018/11/18/fortune-algorithm-details.html
    /// http://paul-reed.co.uk/fortune.htm
    /// </summary>
    /// <param name="points">The points</param>
    /// <param name="extents">The extents as (minX, minY, maxX, maxY)</param>
    public static VoronoiDiagram Generate(IEnumerable<Vector2> points, Vector4 extents)
    {
      if (!points.Any())
        throw new ArgumentException("Points contained no items");

      // Generate voronoi diagram
      var siteEvents = points.Select(x => (new SiteEvent { Position = x } as IEvent, x.Y));
      var events = new PriorityQueue<IEvent, float>(siteEvents);

      var beachLine = new List<BeachLineItem>();

      var completeEdges = new List<Edge>();

      // Handle first event manually
      events.TryDequeue(out var firstEvent, out _);
      beachLine.Add(new BeachLineItem
      {
        Type = BeachLineItemType.Arc,
        Arc = new Arc { Focus = ((SiteEvent)firstEvent).Position }
      });

      float sweepPos = 0.0f;
      while (events.TryDequeue(out var evt, out _))
      {
        if (evt is SiteEvent siteEvent)
        {
          sweepPos = siteEvent.Position.Y;
          var (leftCircleEvent, rightCircleEvent) = AddArcToBeachLine(beachLine, siteEvent);

          if (leftCircleEvent != null)
            events.Enqueue(leftCircleEvent, ((CircleEvent)leftCircleEvent).SweepPos);
          if (rightCircleEvent != null)
            events.Enqueue(rightCircleEvent, ((CircleEvent)rightCircleEvent).SweepPos);
        }
        else if (evt is CircleEvent intersectionEvent)
        {
          if (!intersectionEvent.IsValid)
            continue;
          sweepPos = intersectionEvent.SweepPos;

          var (leftCircleEvent, rightCircleEvent) = RemoveArcFromBeachLine(beachLine, intersectionEvent, completeEdges);

          if (leftCircleEvent != null)
            events.Enqueue(leftCircleEvent, ((CircleEvent)leftCircleEvent).SweepPos);
          if (rightCircleEvent != null)
            events.Enqueue(rightCircleEvent, ((CircleEvent)rightCircleEvent).SweepPos);
        }
        else
        {
          throw new InvalidOperationException("Invalid event encountered");
        }

        //DrawState("diagram.png", points, (int)extents.Z, (int)extents.W, sweepPos, beachLine, completeEdges);
      }

      // Finish edges
      for (int i = 0; i < beachLine.Count; ++i)
      {
        if (beachLine[i].Type == BeachLineItemType.Edge)
        {
          var start = beachLine[i].Edge.Start;
          // TODO: actually work out the proper end at the bounds of the world
          var end = start + beachLine[i].Edge.Direction * 10000.0f;
          completeEdges.Add(new Edge { Start = start, End = end });
        }
      }
      beachLine.Clear();

      DrawState("diagram.png", points, (int)extents.Z, (int)extents.W, sweepPos, beachLine, completeEdges);
      return ConvertToResult(completeEdges, extents);
    }

    private static VoronoiDiagram ConvertToResult(List<Edge> completeEdges, Vector4 extents)
    {
      // Clamp edges to extents
      for (int i = 0; i < completeEdges.Count; ++i)
        completeEdges[i] = ClampEdge(completeEdges[i], extents);

      // Convert to a voronoi diagram
      var vertices = new Dictionary<Vector2, VoronoiDiagram.Vertex>();
      var edges = new List<VoronoiDiagram.Edge>();

      foreach (var edge in completeEdges)
      {
        VoronoiDiagram.Vertex vertexA, vertexB;

        if (!vertices.TryGetValue(edge.Start, out vertexA))
        {
          vertexA = new VoronoiDiagram.Vertex { Position = edge.Start };
          vertices.Add(vertexA.Position, vertexA);
        }

        if (!vertices.TryGetValue(edge.End, out vertexB))
        {
          vertexB = new VoronoiDiagram.Vertex { Position = edge.End };
          vertices.Add(vertexB.Position, vertexB);
        }

        var voronoiEdge = new VoronoiDiagram.Edge { a = vertexA, b = vertexB };

        vertexA.Edges.Add(voronoiEdge);
        vertexB.Edges.Add(voronoiEdge);
        edges.Add(voronoiEdge);
      }

      return new VoronoiDiagram { Vertices = vertices.Select(x => x.Value).ToArray(), Edges = edges.ToArray() };
    }

    private static bool VectorLess(Vector2 a, Vector2 b)
    {
      return a.Y > b.Y || (a.Y == b.Y) && (a.X > b.X);
    }

    private static Edge ClampEdge(Edge edge, Vector4 extents)
    {
      return new Edge
      {
        Start = new Vector2(Math.Clamp(edge.Start.X, extents.X, extents.Z), Math.Clamp(edge.Start.Y, extents.Y, extents.W)),
        End = new Vector2(Math.Clamp(edge.End.X, extents.X, extents.Z), Math.Clamp(edge.End.Y, extents.Y, extents.W))
      };
    }

    private static (IEvent, IEvent) RemoveArcFromBeachLine(List<BeachLineItem> beachLine, CircleEvent intersectionEvent, List<Edge> completeEdges)
    {
      int i;
      for (i = 0; i < beachLine.Count; ++i)
      {
        if (beachLine[i].Id == intersectionEvent.ArcId)
          break;
      }

      if (i == beachLine.Count)
        return (null, null);
        //throw new Exception("fuck");

      var squeezedArc = beachLine[i];

      var leftEdge = i > 0 ? beachLine[i - 1] : null;
      var rightEdge = i+1 < beachLine.Count ? beachLine[i + 1] : null;

      var leftArc = i - 2 >= 0 ? beachLine[i - 2] : null;
      var rightArc = i + 2 < beachLine.Count ? beachLine[i + 2] : null;

      var circleCenter = intersectionEvent.IntersectionPoint;

      var edgeA = new Edge { Start = leftEdge.Edge.Start, End = circleCenter };
      var edgeB = new Edge { Start = circleCenter, End = rightEdge.Edge.Start };

      // TODO: handle extendsUpwardsForever
      if (leftEdge.Edge.extendsUpwardsForever)
      {
      }

      completeEdges.Add(edgeA);
      completeEdges.Add(edgeB);

      var adjacentArcOffset = leftArc.Arc.Focus - rightArc.Arc.Focus;
      var newEdgeDirection = Vector2.Normalize(new Vector2(adjacentArcOffset.Y, -adjacentArcOffset.X));

      beachLine.RemoveRange(i - 1, 3);
      beachLine.Insert(i - 1, new BeachLineItem { Type = BeachLineItemType.Edge, Edge = new HalfEdge { Start = circleCenter, Direction = newEdgeDirection } });

      // TODO: add circle event
      var a = beachLine[i - 2];
      var b = beachLine[i];
      var leftCircleEvent = AddCircleEvent(beachLine, i - 2);
      var rightCircleEvent = AddCircleEvent(beachLine, i);

      return (leftCircleEvent, rightCircleEvent);
    }

    private static (IEvent, IEvent) AddArcToBeachLine(List<BeachLineItem> beachLine, SiteEvent siteEvent)
    {
      // TODO: assertions about types?
      // Update sweep pos
      float sweepPos = siteEvent.Position.Y;

      // Get arc to replace
      var arcAbovePos = FindArcAbove(beachLine, siteEvent.Position.X, siteEvent.Position.Y);
      var arcAbove = beachLine[arcAbovePos];

      // Find intersection with arc above
      float intersectionY = GetParabolaY(siteEvent.Position.X, arcAbove.Arc.Focus, sweepPos);

      // Create new arcs (arcAbove is the left arc)
      var newArc = new Arc { Focus = siteEvent.Position };
      var rightArc = new Arc { Focus = arcAbove.Arc.Focus };

      // Create new edges
      var edgeStart = new Vector2(siteEvent.Position.X, intersectionY);
      var focusOffset = newArc.Focus - arcAbove.Arc.Focus;
      var edgeDir = Vector2.Normalize(new Vector2(focusOffset.Y, -focusOffset.X));

      var leftEdge = new HalfEdge { Start = edgeStart, Direction = -edgeDir };
      var rightEdge = new HalfEdge { Start = edgeStart, Direction = edgeDir };

      // Insert (after arcAbove, which is the left side of the split arc) the sequence: leftSplit, leftEdge, newArc, rightEdge, rightSplit
      int insertPos = arcAbovePos;
      beachLine.Insert(++insertPos, new BeachLineItem { Type = BeachLineItemType.Edge, Edge = leftEdge });
      beachLine.Insert(++insertPos, new BeachLineItem { Type = BeachLineItemType.Arc, Arc = newArc });
      beachLine.Insert(++insertPos, new BeachLineItem { Type = BeachLineItemType.Edge, Edge = rightEdge });
      beachLine.Insert(++insertPos, new BeachLineItem { Type = BeachLineItemType.Arc, Arc = rightArc });

      // TODO: work out circle events
      var leftCircleEvent = AddCircleEvent(beachLine, arcAbovePos);
      var rightCircleEvent = AddCircleEvent(beachLine, insertPos);

      return (leftCircleEvent, rightCircleEvent);
    }

    private static IEvent AddCircleEvent(List<BeachLineItem> beachLine, int arcIndex)
    {
      var arc = beachLine[arcIndex];
      if (arc.Type != BeachLineItemType.Arc)
        throw new Exception();

      BeachLineItem leftEdge = (arcIndex > 0 ? beachLine[arcIndex - 1] : null);
      BeachLineItem rightEdge = (arcIndex + 1 < beachLine.Count ? beachLine[arcIndex + 1] : null);

      if (leftEdge == null || rightEdge == null)
        return null;

      if (!TryIntersectEdgeEdge(leftEdge.Edge, rightEdge.Edge, out var intersection))
        return null;

      var circleCentreOffset = arc.Arc.Focus - intersection;
      float circleRadius = circleCentreOffset.Length();
      float circleEventY = intersection.Y + circleRadius;

      if (arc.Arc.CircleEvent != null)
      {
        // If we already have one that's lower down, then just don't add this one beacuse it'll reference a deleted arc when it gets processed
        if (arc.Arc.CircleEvent.SweepPos >= circleEventY)
        {
          //return null;
        }
        else
        {
        }
        arc.Arc.CircleEvent.IsValid = false;
      }

      // Add new circle event
      var newEvent = new CircleEvent { SweepPos = circleEventY, ArcId = arc.Id, IntersectionPoint = intersection };
      arc.Arc.CircleEvent = newEvent;
      return newEvent;
    }

    /// <summary>
    /// Draw the current state to an image
    /// </summary>
    private static void DrawState(string filename, IEnumerable<Vector2> points, int width, int height, float sweepPos, List<BeachLineItem> beachLine, List<Edge> completedEdges)
    {
      using var image = new Image<Rgba32>(width, height);

      var redPen = Pens.Solid(Color.Red, 5.0f);
      var bluePen = Pens.Solid(Color.Blue, 5.0f);
      var blackPen = Pens.Solid(Color.Black, 1.0f);
      var orangePen = Pens.Solid(Color.Orange, 2.0f);

      image.Mutate(x => x.Fill(Color.White));

      // Draw parabolas
      foreach (var point in points)
      {
        if (point.Y > sweepPos)
          break;

        for (int x = 0; x < width; ++x)
        {
          float y = GetParabolaY(x, point, sweepPos);
          if (y >= 0 && y < height)
            image[x, (int)y] = Color.LightGrey;
        }
      }

      // Draw completed edges
      foreach (var edge in completedEdges)
      {
        image.Mutate(x => x.DrawLines(Pens.Solid(Color.Blue, 3.0f), new PointF(edge.Start.X, edge.Start.Y), new PointF(edge.End.X, edge.End.Y)));
      }

      // Draw beachline
      int xCur = 0;
      // TODO: types
      Arc? lastArc = null;
      for (int curPos = 0; curPos < beachLine.Count; ++curPos)
      {
        if (beachLine[curPos].Type == BeachLineItemType.Arc)
        {
          var curArc = beachLine[curPos].Arc;

          // Handle case where focus is on the sweep line (arc is straight line up)
          if (curArc.Focus.Y == sweepPos)
          {
            if (lastArc.HasValue)
            {
              var topY = GetParabolaY(curArc.Focus.X, lastArc.Value.Focus, sweepPos);
              image.Mutate(x => x.DrawLines(Pens.Solid(Color.Purple, 1.0f), new PointF(curArc.Focus.X, curArc.Focus.Y), new PointF(curArc.Focus.X, topY)));
            }
          }
          else
          {
            // Work out end of arc
            float arcEnd;

            if (curPos + 1 == beachLine.Count)
            {
              arcEnd = (float)width;
            }
            else
            {
              // Get the next edge
              // TODO: check types... and generally improve this a lot
              var edge = beachLine[curPos + 1].Edge;

              // Intersection is the end of this arc
              // TODO: can this ever be false for other reasons?
              if (!TryIntersectEdgeArc(curArc, edge, sweepPos, out var intersection))
                throw new Exception("Blah");
                //intersection = edge.Start;

              arcEnd = intersection.X;
            }

            while (xCur < (int)arcEnd && xCur < width)
            {
              float y = GetParabolaY((float)xCur, curArc.Focus, sweepPos);
              int yInt = (int)y;

              if (yInt >= 0 && yInt < height)
              {
                image[xCur, yInt] = Color.Purple;
              }

              xCur++;
            }

            lastArc = curArc;
          }
        }
        else if (beachLine[curPos].Type == BeachLineItemType.Edge)
        {
          var edge = beachLine[curPos].Edge;

          var leftArc = beachLine[curPos - 1].Arc;
          var rightArc = beachLine[curPos - 1].Arc;

          var arc = edge.Direction.X < 0.0f ? leftArc : rightArc;

          if (!TryIntersectEdgeArc(arc, edge, sweepPos, out var intersection))
            throw new Exception();

          image.Mutate(x => x.DrawLines(Pens.Solid(Color.Orange, 3.0f), new PointF(edge.Start.X, edge.Start.Y), new PointF(intersection.X, intersection.Y)));
        }
      }

      /*
      // Draw beach line, the pairs always go (arc, edge), (edge, arc), etc and start with an arc
      var beachLinePairs = beachLine.Zip(beachLine.Skip(1)).ToList();

      // Add an extra edge just off the edge of the image so that the loop includes the last arc
      beachLinePairs.Add((beachLinePairs.Last().Second,
        new BeachLineItem { Type = BeachLineItemType.Edge,
          Edge = new HalfEdge { Start = new Vector2((float)width+1.0f, (float)height), Direction = new Vector2(0, 1) } }));

      int xCur = 0;
      foreach (var cur in beachLinePairs)
      {
        if (cur.First.Type == BeachLineItemType.Arc)
        {
          // If the arc's focus is on the sweep line then just draw a line up
          if (cur.First.Arc.Focus.Y == sweepPos)
          {
            int arcPos = FindArcAbove(beachLine, cur.First.Arc.Focus.X, sweepPos);
            var arc = beachLine[arcPos].Arc;
            float intersect = GetParabolaY(cur.First.Arc.Focus.X, arc.Focus, sweepPos);
            image.Mutate(x => x.DrawLines(Pens.Solid(Color.Purple, 1.0f), new PointF(cur.First.Arc.Focus.X, cur.First.Arc.Focus.Y), new PointF(cur.First.Arc.Focus.X, intersect)));
          }
          else
          {
            // TODO: check types
            // TODO: these might not actually intersect... think about directions.
            if (!TryIntersectEdgeArc(cur.First.Arc, cur.Second.Edge, sweepPos, out var intersection))
              intersection = cur.Second.Edge.Start;

            while (xCur < width && xCur < intersection.X)
            {
              float y = GetParabolaY((float)xCur, cur.First.Arc.Focus, sweepPos);
              int yInt = (int)y;
              if (yInt >= 0 && yInt < height)
                image[xCur, yInt] = Color.Purple;
              xCur++;
            }
          }
        }
        else
        {

        }
      }
      */

      image.Mutate(x => x.DrawLines(blackPen, new PointF(0.0f, sweepPos), new PointF((float)width, sweepPos)));

      foreach (var point in points)
        image.Mutate(x => x.DrawLines(redPen, new PointF(point.X, point.Y), new PointF(point.X, point.Y)));

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

    private static int FindArcAbove(List<BeachLineItem> beachLine, float x, float y)
    {
      // Should never happen because we add an item to the beachline at the start
      if (beachLine.Count == 0)
        throw new InvalidOperationException("Beachline should never be empty");

      // TODO: probably not the best way
      for (int curPos = 0; curPos < beachLine.Count; ++curPos)
      {
        if (beachLine[curPos].Type == BeachLineItemType.Arc)
        {
          var curArc = beachLine[curPos].Arc;

          // If this is the last arc just return it
          if (curPos + 1 == beachLine.Count)
          {
            return curPos;
          }

          // If this isn't the first arc, there'll be an edge before this one that we can use to get arcMin
          float arcMin = 0;
          if (curPos > 0)
          {
            var edgeBefore = beachLine[curPos - 1].Edge;
            // TODO: should this ever fail?
            if (TryIntersectEdgeArc(curArc, edgeBefore, y, out var prevIntersection))
              arcMin = prevIntersection.X;
          }

          // If this isn't the last arc, there'll be an edge after that we can use to get arcMax
          float arcMax = float.MaxValue;
          if (curPos + 1 < beachLine.Count)
          {
            var edgeAfter = beachLine[curPos + 1].Edge;
            // TODO: should this ever fail?
            if (TryIntersectEdgeArc(curArc, edgeAfter, y, out var nextIntersection))
              arcMax = nextIntersection.X;
          }

          // If this x lies between arcMin and arcMax, this is the right arc
          if (arcMin <= x && x <= arcMax)
            return curPos;
        }
      }

      throw new Exception();
    }

    private static bool TryIntersectEdgeEdge(HalfEdge e1, HalfEdge e2, out Vector2 intersection)
    {
      // TODO: work out
      float dx = e2.Start.X - e1.Start.X;
      float dy = e2.Start.Y - e1.Start.Y;
      float det = e2.Direction.X * e1.Direction.Y - e2.Direction.Y * e1.Direction.X;
      float u = (dy * e2.Direction.X - dx * e2.Direction.Y) / det;
      float v = (dy * e1.Direction.X - dx * e1.Direction.Y) / det;

      intersection = Vector2.Zero;

      if ((u < 0.0f) && !e1.extendsUpwardsForever)
        return false;
      if ((v < 0.0f) && !e2.extendsUpwardsForever)
        return false;
      if ((u == 0.0f) && (v == 0.0f) && !e1.extendsUpwardsForever && !e2.extendsUpwardsForever)
        return false;

      intersection = new Vector2(e1.Start.X + e1.Direction.X * u, e1.Start.Y + e1.Direction.Y * u);
      return true;
    }

    private static bool TryIntersectEdgeArc(Arc arc, HalfEdge edge, float sweepPos, out Vector2 intersection)
    {
      // If the edge is a vertical line
      if (edge.Direction.X == 0.0f)
      {
        // If the arc is also a vertical line because the sweep line is on its focus
        if (arc.Focus.Y == sweepPos)
        {
          // Will get set even if we return false, because we have to set it to something anyway
          intersection = arc.Focus;
          return edge.Start.X == arc.Focus.X;
        }

        // Otherwise they definitely intersect and we can just use GetParabolaY
        intersection = new Vector2(edge.Start.X, GetParabolaY(edge.Start.X, arc.Focus, sweepPos));
        return true;
      }

      // Work out m and c in y = mx + k for the line
      float m = edge.Direction.Y / edge.Direction.X;
      // k = y - mx
      float k = edge.Start.Y - m * edge.Start.X;

      // If the arc is a vertical line because the sweep line is on its focus
      if (arc.Focus.Y == sweepPos)
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
      float knownY = 0.5f * arc.Focus.Y + 0.5f * sweepPos;

      // https://jacquesheunis.com/post/fortunes-algorithm/
      float a = 1.0f / (2.0f * (arc.Focus.Y - sweepPos));
      float b = -m - 2.0f * a * arc.Focus.X;
      float c = a * arc.Focus.X * arc.Focus.X + knownY - k;

      float discriminant = b * b - 4.0f * a * c;

      if (Math.Abs(discriminant) < 0.001f)
        discriminant = 0.0f;

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

      if ((x1Dot >= 0.0f) && (x2Dot < 0.0f))
        x = x1;
      else if ((x1Dot < 0.0f) && (x2Dot >= 0.0f))
        x = x2;
      else if ((x1Dot >= 0.0f) && (x2Dot >= 0.0f))
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

      float y = GetParabolaY(x, arc.Focus, sweepPos);

      intersection = new Vector2(x, y);
      return true;
    }
  }
}
