using Ensage;
using System;
using System.Collections.Generic;
using System.Linq;
using SharpDX;

namespace Cloey.Extensions
{
    public struct Segment
    {
        public bool IsOnSegment;
        public Vector2 LinePoint;
        public Vector2 SegmentPoint;

        internal Segment(bool isOnSegment, Vector2 segmentPoint, Vector2 linePoint)
        {
            IsOnSegment = isOnSegment;
            SegmentPoint = segmentPoint;
            LinePoint = linePoint;
        }
    }

    internal static class MathUtils
    {
        internal static readonly NavMeshPathfinding Pathfinding = new NavMeshPathfinding();

        internal static float Dist(this Entity from, Vector2 to, bool squared = false)
        {
            return !squared ? Vector2.Distance(from.Position.To2D(), to) : Vector2.DistanceSquared(from.Position.To2D(), to);
        }

        internal static float Dist(this Entity from, Vector3 to, bool squared = false)
        {
            return !squared ? Vector2.Distance(from.Position.To2D(), to.To2D()) : Vector2.DistanceSquared(from.Position.To2D(), to.To2D());
        }

        internal static float Dist(this Vector2 from, Vector2 to, bool squared = false)
        {
            return !squared ? Vector2.Distance(from, to) : Vector2.DistanceSquared(from, to);
        }

        internal static float Dist(this Vector3 from, Vector3 to, bool squared = false)
        {
            return !squared ? Vector2.Distance(from.To2D(), to.To2D()) : Vector2.DistanceSquared(from.To2D(), to.To2D());
        }

        internal static Vector2 To2D(this Vector3 pos)
        {
            return new Vector2(pos.X, pos.Y);
        }

        public static List<Vector2> To2D(this List<Vector3> path)
        {
            return path.Select(point => point.To2D()).ToList();
        }

        internal static Vector3 To3D(this Vector2 pos)
        {
            return new Vector3(pos.X, pos.Y, ObjectManager.LocalHero.Position.Z);
        }

        internal static Vector2 Normalized(this Vector2 pos)
        {
            return Vector2.Normalize(pos);
        }

        internal static Vector3 Normailized(this Vector3 pos)
        {
            return Vector3.Normalize(pos);
        }

        public static Vector2 Perpendicular(this Vector2 v)
        {
            return new Vector2(-v.Y, v.X);
        }

        public static Vector2 Perpendicular2(this Vector2 v)
        {
            return new Vector2(v.Y, -v.X);
        }

        public static Vector2 Rotated(this Vector2 v, float angle)
        {
            double num1 = Math.Cos(angle);
            double num2 = Math.Sin(angle);
            return new Vector2((float) (v.X * num1 - v.Y * num2), (float) (v.Y * num1 + v.X * num2));
        }

        public static Segment ProjectsOn(this Vector2 point, Vector2 segmentStart, Vector2 segmentEnd)
        {
            double x1 = point.X;
            float y1 = point.Y;
            float x2 = segmentStart.X;
            float y2 = segmentStart.Y;
            float x3 = segmentEnd.X;
            float y3 = segmentEnd.Y;
            double num1 = x2;
            float num2 =
                (float)
                (((x1 - num1) * (x3 - (double) x2) + (y1 - (double) y2) * (y3 - (double) y2)) /
                 (Math.Pow(x3 - (double) x2, 2.0) + Math.Pow(y3 - (double) y2, 2.0)));
            Vector2 vector2 = new Vector2(x2 + num2 * (x3 - x2), y2 + num2 * (y3 - y2));
            float num3 = num2 >= 0.0 ? num2 <= 1.0 ? num2 : 1f : 0.0f;
            int num4 = num3.CompareTo(num2) == 0 ? 1 : 0;
            Vector2 segmentPoint = num4 != 0 ? vector2 : new Vector2(x2 + num3 * (x3 - x2), y2 + num3 * (y3 - y2));
            Vector2 linePoint = vector2;
            return new Segment(num4 != 0, segmentPoint, linePoint);
        }

        public static float PathLength(this List<Vector2> path)
        {
            float num = 0.0f;
            for (int index = 0; index < path.Count - 1; ++index)
                num += path[index].Dist(path[index + 1]);
            return num;
        }

        internal static int CountInPath(Vector3 startpos, Vector3 endpos, float width, float range, out List<Unit> units, bool heroOnly = true)
        {
            var me = ObjectManager.LocalHero;
            var end = endpos.To2D();
            var start = startpos.To2D();
            var direction = (end - start).Normalized();
            var endposition = start + direction * start.Dist(end);

            IEnumerable<Unit> objinpath;
            objinpath = ObjectManager.GetEntities<Unit>()
                .Where(b => b.Team != me.Team && b.IsValidUnit())
                .Where(unit => ObjectManager.LocalHero.NetworkPosition.Dist(unit.Position) <= range)
                .Where(unit => !heroOnly || unit is Hero)
                .Select(unit => new {unit, seg = unit.Position.To2D().ProjectsOn(start, endposition)})
                .Select(x => new {t = x, segdist = x.unit.Position.To2D().Dist(x.seg.SegmentPoint)})
                .Where(x => x.t.unit.HullRadius + 35 + width > x.segdist && x.t.seg.IsOnSegment)
                .Select(x => x.t.unit);

            units = objinpath.ToList();
            return units.Count;
        }

        internal static Vector2 GetFirstWallPoint(Vector3 from, Vector3 to, float step = 25)
        {
            return GetFirstWallPoint(from.To2D(), to.To2D(), step);
        }

        internal static Vector2 GetFirstWallPoint(Vector2 from, Vector2 to, float step = 25)
        {
            var direction = (to - from).Normalized();

            for (float i = 0; i < from.Dist(to); i = i + step)
            {
                var point2D = from + direction * i;
                var pointAfter = from + direction * i + ObjectManager.LocalHero.HullRadius * 2; 

                var meshPoint = NavMesh.GetCellFlags(point2D.X, point2D.Y);
                var meshCheckPoint = NavMesh.GetCellFlags(pointAfter.X, pointAfter.Y);

                if (meshPoint.HasFlag(NavMeshCellFlags.InteractionBlocker) || meshPoint.HasFlag(NavMeshCellFlags.MovementBlocker))
                {
                    return from + direction * (i - step);
                }
            }

            return default(Vector2);
        }

        public static IEnumerable<Vector3> PredictRoute(this Unit target, int time, Vector3 targetPosition)
        {
            bool completed;
            var path =
                Pathfinding.CalculateStaticLongPath(target.NetworkPosition, targetPosition,
                    target.MovementSpeed * time * 4, true, out completed).ToList();

            return !completed ? new List<Vector3> { targetPosition } : path;
        }
    }
}
