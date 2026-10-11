using System.Collections.Generic;
using UnityEngine;

namespace TinyTrouble.EditorTools
{
    /// <summary>Создаёт собственные объёмные формы: скруглённые корпуса, лезвия и полый купол.</summary>
    public static class CosmeticMeshBuilder
    {
        /// <summary>Собирает корпус со скошенными краями и мягкими нормалями для освещения.</summary>
        /// <param name="size">Полный размер корпуса.</param>
        /// <param name="bevel">Ширина скругления края.</param>
        /// <returns>Сетка, которую сборщик ассетов сохраняет вместе с предметом.</returns>
        public static Mesh RoundedBox(Vector3 size, float bevel)
        {
            var half = size * .5f;
            bevel = Mathf.Clamp(bevel, .001f, Mathf.Min(half.x, half.y, half.z) * .8f);
            var inner = half - Vector3.one * bevel;
            var vertices = new List<Vector3>(); var normals = new List<Vector3>();
            var uv = new List<Vector2>(); var triangles = new List<int>();
            foreach (var normal in new[] { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back })
            {
                var u = Mathf.Abs(normal.y) > .5f ? Vector3.right : Vector3.up;
                var v = Vector3.Cross(normal, u);
                var extent = Vector3.Dot(half, Abs(normal));
                var a = Vector3.Dot(half, Abs(u)); var b = Vector3.Dot(half, Abs(v));
                var xs = new[] { -a, -a + bevel, a - bevel, a };
                var ys = new[] { -b, -b + bevel, b - bevel, b };
                var start = vertices.Count;
                for (var y = 0; y < 4; y++) for (var x = 0; x < 4; x++)
                {
                    var p = normal * extent + u * xs[x] + v * ys[y];
                    var core = new Vector3(Mathf.Clamp(p.x, -inner.x, inner.x), Mathf.Clamp(p.y, -inner.y, inner.y), Mathf.Clamp(p.z, -inner.z, inner.z));
                    var edge = (p - core).normalized;
                    vertices.Add(core + edge * bevel); normals.Add(edge); uv.Add(new Vector2(xs[x] / (a * 2) + .5f, ys[y] / (b * 2) + .5f));
                }
                for (var y = 0; y < 3; y++) for (var x = 0; x < 3; x++)
                { var i = start + y * 4 + x; triangles.AddRange(new[] { i, i + 1, i + 5, i, i + 5, i + 4 }); }
            }
            var mesh = Finish("RoundedBox", vertices, uv, triangles); mesh.SetNormals(normals); return mesh;
        }
        /// <summary>Создаёт замкнутую объёмную фигуру по выпуклому или вогнутому силуэту.</summary>
        /// <param name="outline">Точки простого силуэта в любом направлении обхода.</param>
        /// <param name="depth">Толщина фигуры.</param>
        /// <returns>Сетка с лицевой, обратной и боковыми поверхностями.</returns>
        public static Mesh Extrude(Vector2[] outline, float depth)
        {
            outline = (Vector2[])outline.Clone();
            var signedArea = 0f;
            for (var i = 0; i < outline.Length; i++) signedArea += Cross(outline[i], outline[(i + 1) % outline.Length]);
            if (signedArea < 0) System.Array.Reverse(outline);
            var cap = Triangulate(outline);
            var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var triangles = new List<int>();
            for (var side = -1; side <= 1; side += 2)
            {
                var start = vertices.Count;
                foreach (var p in outline) { vertices.Add(new Vector3(p.x, p.y, side * depth * .5f)); uv.Add(p + Vector2.one * .5f); }
                for (var i = 0; i < cap.Count; i += 3)
                    triangles.AddRange(side > 0 ? new[] { start + cap[i], start + cap[i + 1], start + cap[i + 2] } : new[] { start + cap[i], start + cap[i + 2], start + cap[i + 1] });
            }
            for (var i = 0; i < outline.Length; i++)
            {
                var a = outline[i]; var b = outline[(i + 1) % outline.Length]; var start = vertices.Count;
                vertices.AddRange(new[] { new Vector3(a.x, a.y, -depth / 2), new Vector3(a.x, a.y, depth / 2), new Vector3(b.x, b.y, depth / 2), new Vector3(b.x, b.y, -depth / 2) });
                uv.AddRange(new[] { Vector2.zero, Vector2.up, Vector2.one, Vector2.right });
                triangles.AddRange(new[] { start, start + 2, start + 1, start, start + 3, start + 2 });
            }
            return Finish("Extrusion", vertices, uv, triangles);
        }
        /// <summary>Отрезает маленькие треугольники по краям силуэта, не перекрывая его вогнутые участки.</summary>
        /// <param name="points">Силуэт, упорядоченный против часовой стрелки.</param>
        /// <returns>Треугольники лицевой стороны.</returns>
        static List<int> Triangulate(Vector2[] points)
        {
            var remaining = new List<int>(); var triangles = new List<int>();
            for (var i = 0; i < points.Length; i++) remaining.Add(i);
            while (remaining.Count > 3)
            {
                var cut = false;
                for (var i = 0; i < remaining.Count; i++)
                {
                    var a = remaining[(i + remaining.Count - 1) % remaining.Count]; var b = remaining[i]; var c = remaining[(i + 1) % remaining.Count];
                    if (Cross(points[b] - points[a], points[c] - points[b]) <= .000001f) continue;
                    var contains = false;
                    foreach (var p in remaining)
                        if (p != a && p != b && p != c && Cross(points[b] - points[a], points[p] - points[a]) >= -.000001f &&
                            Cross(points[c] - points[b], points[p] - points[b]) >= -.000001f && Cross(points[a] - points[c], points[p] - points[c]) >= -.000001f)
                        { contains = true; break; }
                    if (contains) continue;
                    triangles.AddRange(new[] { a, b, c }); remaining.RemoveAt(i); cut = true; break;
                }
                if (!cut) throw new System.ArgumentException("Силуэт лезвия пересекается или содержит повторяющиеся точки");
            }
            triangles.AddRange(remaining); return triangles;
        }
        /// <summary>Определяет сторону поворота двух направлений на плоскости.</summary>
        /// <param name="a">Первое направление.</param>
        /// <param name="b">Второе направление.</param>
        /// <returns>Положительное число для поворота против часовой стрелки.</returns>
        static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;
        /// <summary>Создаёт отдельную секцию открытого снизу купола с настоящей внутренней стороной.</summary>
        /// <param name="panel">Номер секции.</param>
        /// <param name="count">Количество секций купола.</param>
        /// <returns>Наружная и внутренняя поверхности ткани.</returns>
        public static Mesh Canopy(int panel, int count)
        {
            var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var triangles = new List<int>();
            const int rings = 10, columns = 4;
            for (var inside = 0; inside < 2; inside++)
            {
                var start = vertices.Count;
                for (var row = 0; row <= rings; row++) for (var col = 0; col <= columns; col++)
                {
                    var t = row / (float)rings; var angle = (panel + col / (float)columns) / count * Mathf.PI * 2;
                    var theta = t * Mathf.PI * .48f; var radius = Mathf.Sin(theta);
                    var puff = 1 + Mathf.Sin(col / (float)columns * Mathf.PI) * .035f;
                    vertices.Add(new Vector3(Mathf.Cos(angle) * radius * 1.14f * puff, Mathf.Cos(theta) * .65f - inside * .025f, Mathf.Sin(angle) * radius * .82f * puff));
                    uv.Add(new Vector2((panel + col / (float)columns) / count, t));
                }
                for (var row = 0; row < rings; row++) for (var col = 0; col < columns; col++)
                {
                    var i = start + row * (columns + 1) + col;
                    triangles.AddRange(inside == 0 ? new[] { i, i + 1, i + columns + 1, i + 1, i + columns + 2, i + columns + 1 } : new[] { i, i + columns + 1, i + 1, i + 1, i + columns + 1, i + columns + 2 });
                }
            }
            return Finish("CanopyPanel", vertices, uv, triangles);
        }
        /// <summary>Создаёт выпуклую пятиконечную звезду с объёмными гранями.</summary>
        /// <param name="radius">Расстояние от центра до луча.</param>
        /// <returns>Сетка звезды.</returns>
        public static Mesh Star(float radius)
        {
            var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var triangles = new List<int>();
            for (var side = -1; side <= 1; side += 2)
            {
                var start = vertices.Count; vertices.Add(new Vector3(0, 0, side * radius * .23f)); uv.Add(Vector2.one * .5f);
                for (var i = 0; i < 10; i++)
                { var angle = Mathf.PI * .5f + i * Mathf.PI / 5; var r = i % 2 == 0 ? radius : radius * .45f; vertices.Add(new Vector3(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r, 0)); uv.Add(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * .5f + Vector2.one * .5f); }
                for (var i = 0; i < 10; i++)
                    triangles.AddRange(side > 0 ? new[] { start, start + i + 1, start + (i + 1) % 10 + 1 } : new[] { start, start + (i + 1) % 10 + 1, start + i + 1 });
            }
            return Finish("Star", vertices, uv, triangles);
        }
        /// <summary>Собирает точки в сетку и вычисляет освещение и размер.</summary>
        /// <param name="name">Имя формы.</param>
        /// <param name="vertices">Точки поверхности.</param>
        /// <param name="uv">Координаты рисунка.</param>
        /// <param name="triangles">Треугольники между точками.</param>
        /// <returns>Готовая сетка.</returns>
        static Mesh Finish(string name, List<Vector3> vertices, List<Vector2> uv, List<int> triangles)
        { var mesh = new Mesh { name = name }; mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh; }
        /// <summary>Возвращает положительную длину каждой составляющей направления.</summary>
        /// <param name="value">Направление.</param>
        /// <returns>Положительные составляющие.</returns>
        static Vector3 Abs(Vector3 value) => new Vector3(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));
    }
}
