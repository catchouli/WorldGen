using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Diagnostics;
using System.Numerics;
using Utils;

namespace Algorithms.Voronoi
{
  public partial class FortunesAlgorithm
  {
    /// <summary>
    /// Draw the current state to an image
    /// </summary>
    /// <param name="filename">The output filename</param>
    /// <param name="width">The width of the file</param>
    /// <param name="height">The height of the file</param>
    /// <param name="sweepPos">The current sweep pos</param>
    /// <param name="points">The input points</param>
    /// <param name="beachLine">The beach line</param>
    /// <param name="completedEdges">The completed edges</param>
    private static void DrawBeachLine(string filename, int width, int height, float sweepPos, IEnumerable<Vector2> points, List<IBeachLineItem> beachLine, List<CompletedEdge> completedEdges)
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
          // Draw line straight up edge case
          if (point.Y != sweepPos)
          {
            if (!TryGetParabolaY(x, point, sweepPos, out var y))
              throw new InternalErrorException();
            if (y >= 0 && y < height)
              image[x, (int)y] = Color.LightGrey;
          }
        }
      }

      // Draw completed edges
      foreach (var edge in completedEdges)
      {
        // TODO: line lengths
        image.Mutate(x => x.DrawLines(Pens.Solid(Color.Blue, 3.0f), new PointF(edge.Start.X, edge.Start.Y), new PointF(edge.End.X, edge.End.Y)));
      }

      // Draw beachline
      int xCur = 0;
      Arc lastArc = null;
      for (int curPos = 0; curPos < beachLine.Count; ++curPos)
      {
        if (beachLine[curPos] is Arc curArc)
        {
          // Handle case where focus is on the sweep line (arc is straight line up)
          if (curArc.Focus.Y == sweepPos)
          {
            if (lastArc != null)
            {
              if (!TryGetParabolaY(curArc.Focus.X, lastArc.Focus, sweepPos, out var topY))
                throw new InternalErrorException();
              image.Mutate(x => x.DrawLines(Pens.Solid(Color.Purple, 1.0f), new PointF(curArc.Focus.X, curArc.Focus.Y), new PointF(curArc.Focus.X, topY)));
            }
          }
          else
          {
            // Work out end of arc
            float arcEnd;

            if (curPos + 1 == beachLine.Count)
            {
              arcEnd = width;
            }
            else
            {
              // Get the next edge
              // TODO: check types... and generally improve this a lot
              var edge = beachLine[curPos + 1] as HalfEdge;
              Debug.Assert(edge != null);

              // Intersection is the end of this arc
              // TODO: can this ever be false for other reasons?
              if (!TryIntersectArcHalfEdge(curArc, edge, sweepPos, out var intersection))
                throw new Exception("Blah");
              //intersection = edge.Start;

              arcEnd = intersection.X;
            }

            while (xCur < (int)arcEnd && xCur < width)
            {
              if (!TryGetParabolaY(xCur, curArc.Focus, sweepPos, out var y))
                throw new InternalErrorException();

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
        else if (beachLine[curPos] is HalfEdge edge)
        {
          var leftArc = beachLine[curPos - 1] as Arc;
          Debug.Assert(leftArc != null);
          var rightArc = beachLine[curPos - 1] as Arc;
          Debug.Assert(rightArc != null);

          var arc = edge.Direction.X < 0.0f ? leftArc : rightArc;

          if (TryIntersectArcHalfEdge(arc, edge, sweepPos, out var intersection))
            image.Mutate(x => x.DrawLines(Pens.Solid(Color.Orange, 3.0f), new PointF(edge.Start.X, edge.Start.Y), new PointF(intersection.X, intersection.Y)));
        }
      }

      image.Mutate(x => x.DrawLines(blackPen, new PointF(0.0f, sweepPos), new PointF(width, sweepPos)));

      foreach (var point in points)
        image.Mutate(x => x.DrawLines(redPen, new PointF(point.X, point.Y), new PointF(point.X, point.Y)));

      image.Save(filename);
    }
  }
}
