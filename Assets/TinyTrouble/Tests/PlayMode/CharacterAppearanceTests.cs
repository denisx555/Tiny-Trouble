using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TinyTrouble.Tests
{
    /// <summary>Проверяет фактические материалы примерки, независимость копий и освобождение.</summary>
    public class CharacterAppearanceTests
    {
        /// <summary>Примерка не закрепляет одежду, не перекрашивает соседа и возвращает исходный материал после удаления компонента.</summary>
        /// <returns>Ожидание уничтожения временных материалов.</returns>
        [UnityTest] public IEnumerator ClothingPreviewDoesNotCommitOrAlterAnotherHero()
        {
            var first = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            var second = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            var original = new Material(Shader.Find("Standard")) { color = Color.yellow };
            var red = new Material(Shader.Find("Standard")) { color = Color.red };
            var space = new Material(Shader.Find("Standard")) { color = Color.blue };
            first.GetComponent<Renderer>().sharedMaterial = original;
            second.GetComponent<Renderer>().sharedMaterial = original;
            try
            {
                var appearance = first.AddComponent<CharacterAppearance>();
                appearance.Configure(new[] { first.GetComponent<Renderer>() });
                appearance.Configure(new[] { first.GetComponent<Renderer>() });
                appearance.Apply(new CosmeticItem { Id = "base-red", Category = CosmeticCategory.Clothes, Material = red });
                Assert.That(first.GetComponent<Renderer>().sharedMaterial.color, Is.EqualTo(Color.red));
                appearance.Preview(new CosmeticItem { Id = "space", Category = CosmeticCategory.Clothes, Material = space });
                Assert.That(first.GetComponent<Renderer>().sharedMaterial.color, Is.EqualTo(Color.blue));
                Assert.That(appearance.AppliedId, Is.EqualTo("base-red"));
                Assert.That(second.GetComponent<Renderer>().sharedMaterial.color, Is.EqualTo(Color.yellow));
                appearance.Restore();
                Assert.That(first.GetComponent<Renderer>().sharedMaterial.color, Is.EqualTo(Color.red));
                Object.Destroy(appearance); yield return null;
                Assert.That(first.GetComponent<Renderer>().sharedMaterial, Is.SameAs(original));
            }
            finally
            { Object.Destroy(first); Object.Destroy(second); Object.Destroy(original); Object.Destroy(red); Object.Destroy(space); }
        }
    }
}
