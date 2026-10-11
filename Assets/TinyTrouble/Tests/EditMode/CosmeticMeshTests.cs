using NUnit.Framework;
using TinyTrouble.EditorTools;
using UnityEngine;

namespace TinyTrouble.Tests
{
    /// <summary>Проверяет, что вогнутые лезвия имеют сплошные стороны с правильным направлением поверхности.</summary>
    public class CosmeticMeshTests
    {
        /// <summary>Обе стороны ледяного лезвия видны снаружи, а треугольники точно покрывают его силуэт.</summary>
        [Test] public void ConcaveClockwiseBladeHasCompleteOutwardCaps()
        {
            var outline = new[] { new Vector2(-.68f, -.22f), new Vector2(-.48f, .18f), new Vector2(-.14f, .33f), new Vector2(.18f, .28f), new Vector2(.57f, .08f), new Vector2(.72f, -.25f), new Vector2(.26f, -.03f), new Vector2(-.2f, .03f) };
            var mesh = CosmeticMeshBuilder.Extrude(outline, .2f);
            try
            {
                var vertices = mesh.vertices; var indices = mesh.triangles; var front = 0f; var back = 0f;
                for (var i = 0; i < indices.Length; i += 3)
                {
                    var a = vertices[indices[i]]; var b = vertices[indices[i + 1]]; var c = vertices[indices[i + 2]];
                    if (Mathf.Abs(a.z - b.z) > .001f || Mathf.Abs(b.z - c.z) > .001f) continue;
                    var normal = Vector3.Cross(b - a, c - a);
                    Assert.That(normal.z * a.z, Is.GreaterThan(0), "Лицевая сторона треугольника должна смотреть наружу");
                    if (a.z < 0) front += normal.magnitude / 2; else back += normal.magnitude / 2;
                }
                Assert.That(front, Is.EqualTo(.34775f).Within(.0001f));
                Assert.That(back, Is.EqualTo(.34775f).Within(.0001f));
            }
            finally { Object.DestroyImmediate(mesh); }
        }
    }
}
