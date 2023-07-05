using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using Point = OpenCvSharp.Point;

namespace robot_location_detection
{
    public class RectangleCornerFinder
    {
        public static List<PointF> ConvexHull(List<PointF> points)
        {
            if (points.Count <= 1)
                return points;

            points.Sort((a, b) =>
            {
                int cmp = a.X.CompareTo(b.X);
                return cmp != 0 ? cmp : a.Y.CompareTo(b.Y);
            });

            List<PointF> hull = new List<PointF>();

            int start = 0, end = 0;

            // Build lower hull
            for (int i = 0; i < points.Count; i++)
            {
                PointF currentPoint = points[i];

                while (hull.Count >= 2 && CrossProduct(hull[hull.Count - 2], hull[hull.Count - 1], currentPoint) <= 0)
                {
                    hull.RemoveAt(hull.Count - 1);
                }

                hull.Add(currentPoint);
            }

            start = hull.Count + 1;

            // Build upper hull
            for (int i = points.Count - 2; i >= 0; i--)
            {
                PointF currentPoint = points[i];

                while (hull.Count >= start && CrossProduct(hull[hull.Count - 2], hull[hull.Count - 1], currentPoint) <= 0)
                {
                    hull.RemoveAt(hull.Count - 1);
                }

                hull.Add(currentPoint);
            }

            hull.RemoveAt(hull.Count - 1);

            return hull;
        }

        private static float CrossProduct(PointF A, PointF B, PointF C)
        {
            return (B.X - A.X) * (C.Y - A.Y) - (B.Y - A.Y) * (C.X - A.X);
        }

        public static List<Point[]> FindRectangleCorners(List<Point[]> pointList, out List<Point2f[]> resultCv)
        {
            List<Point[]> result = new List<Point[]>();
            resultCv = new List<Point2f[]>();

            foreach (Point[] points in pointList)
            {
                List<PointF> pointFs = new List<PointF>();
                foreach (Point point in points)
                {
                    pointFs.Add(new PointF(point.X, point.Y));
                }

                List<PointF> convexHull = ConvexHull(pointFs);

                PointF topLeft = convexHull[0];
                PointF topRight = convexHull[0];
                PointF bottomRight = convexHull[0];
                PointF bottomLeft = convexHull[0];

                foreach (PointF point in convexHull)
                {
                    if (point.X + point.Y < topLeft.X + topLeft.Y)
                    {
                        topLeft = point;
                    }
                    if (point.X - point.Y > topRight.X - topRight.Y)
                    {
                        topRight = point;
                    }
                    if (point.X + point.Y > bottomRight.X + bottomRight.Y)
                    {
                        bottomRight = point;
                    }
                    if (point.X - point.Y < bottomLeft.X - bottomLeft.Y)
                    {
                        bottomLeft = point;
                    }
                }

                result.Add(new Point[]
                {
                new Point((int)topLeft.X, (int)topLeft.Y),
                new Point((int)topRight.X, (int)topRight.Y),
                new Point((int)bottomRight.X, (int)bottomRight.Y),
                new Point((int)bottomLeft.X, (int)bottomLeft.Y)
                });

                resultCv.Add(new Point2f[]
                {
                new Point2f(topLeft.X, topLeft.Y),
                new Point2f(topRight.X, topRight.Y),
                new Point2f(bottomRight.X, bottomRight.Y),
                new Point2f(bottomLeft.X, bottomLeft.Y)
                });
            }

            return result;
        }
    }
}
