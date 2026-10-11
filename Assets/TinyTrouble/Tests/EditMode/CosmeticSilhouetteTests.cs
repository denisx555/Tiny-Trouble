using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace TinyTrouble.Tests
{
    /// <summary>Проверяет, что скины одного оружия различаются формой, даже если убрать цвета.</summary>
    public class CosmeticSilhouetteTests
    {
        /// <summary>Возврат к одному корпусу для обеих расцветок должен обнаруживаться по боковому силуэту.</summary>
        /// <param name="first">Первый скин.</param>
        /// <param name="second">Второй скин того же оружия.</param>
        [TestCase("pistol-pop", "pistol-ice")]
        [TestCase("rifle-lime", "rifle-space")]
        [TestCase("shotgun-fire", "shotgun-wave")]
        public void SameSlotSkinsHaveDistinctSideSilhouettes(string first, string second)
        {
            var a = Silhouette(first); var b = Silhouette(second); var union = 0; var difference = 0;
            for (var i = 0; i < a.Length; i++)
            { if (a[i] || b[i]) union++; if (a[i] != b[i]) difference++; }
            Assert.That(union, Is.GreaterThan(100), "Обе модели должны занимать заметную часть образца");
            Assert.That(difference / (float)union, Is.GreaterThan(.12f), "Одной смены цвета недостаточно: формы оружия должны различаться");
        }
        /// <summary>Проецирует настоящие треугольники модели в чёрно-белый образец без материалов.</summary>
        /// <param name="id">Имя сохранённого шаблона.</param>
        /// <returns>Занятые точки бокового вида.</returns>
        static bool[] Silhouette(string id)
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TinyTrouble/Cosmetics/Models/" + id + ".prefab");
            Assert.That(root, Is.Not.Null);
            var mask = new bool[80 * 48];
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>())
            {
                var vertices = filter.sharedMesh.vertices; var indices = filter.sharedMesh.triangles;
                for (var t = 0; t < indices.Length; t += 3)
                {
                    Vector2 a = filter.transform.TransformPoint(vertices[indices[t]]);
                    Vector2 b = filter.transform.TransformPoint(vertices[indices[t + 1]]);
                    Vector2 c = filter.transform.TransformPoint(vertices[indices[t + 2]]);
                    var area = Cross(b - a, c - a); if (Mathf.Abs(area) < .000001f) continue;
                    for (var y = 0; y < 48; y++) for (var x = 0; x < 80; x++)
                    {
                        var p = new Vector2(-1.7f + (x + .5f) * 3.4f / 80, -.9f + (y + .5f) * 1.8f / 48);
                        var u = Cross(b - a, p - a) / area; var v = Cross(p - a, c - a) / area;
                        if (u >= 0 && v >= 0 && u + v <= 1) mask[y * 80 + x] = true;
                    }
                }
            }
            return mask;
        }
        /// <summary>Вычисляет площадь со знаком для проверки попадания точки в треугольник.</summary>
        /// <param name="a">Первая сторона.</param>
        /// <param name="b">Вторая сторона.</param>
        /// <returns>Удвоенная площадь.</returns>
        static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;
    }
}
