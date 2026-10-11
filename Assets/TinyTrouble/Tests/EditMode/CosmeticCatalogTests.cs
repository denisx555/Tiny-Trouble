using System.Linq;
using NUnit.Framework;
using UnityEngine;
using TinyTrouble.EditorTools;

namespace TinyTrouble.Tests
{
    /// <summary>Находит пропущенные товары, неверные цены и несовместимые места выбора.</summary>
    public class CosmeticCatalogTests
    {
        /// <summary>Проверяет весь каталог и новые цены, включая повторную подготовку.</summary>
        [Test] public void CatalogHasStableIdsAndNewPrices()
        {
            var catalog = CosmeticAssetBuilder.BuildCatalog();
            Assert.That(catalog.Items.Count, Is.EqualTo(37));
            Assert.That(catalog.Items.Count(i => i.Price > 0), Is.EqualTo(27));
            Assert.That(catalog.Items.Select(i => i.Id).Distinct().Count(), Is.EqualTo(37));
            Assert.That(catalog.Items.Select(i => i.Category).Distinct().Count(), Is.EqualTo(8));
            Assert.That(catalog.Items.Select(i => i.Slot).Distinct().Count(), Is.EqualTo(10));
            var prices = new[] { 400, 350, 500, 300, 400, 600, 400, 400 };
            foreach (var item in catalog.Items)
                Assert.That(item.Price, Is.EqualTo(item.BasicColorIndex >= 0 || item.Id.EndsWith("-gray") ? 0 : prices[(int)item.Category]), item.Id);
            Assert.That(catalog.Validate(false), Is.Empty);
            Assert.That(CosmeticAssetBuilder.BuildCatalog().Items.Count, Is.EqualTo(37));
            Assert.That(catalog.TryGet("stripes", out var stripes), Is.True);
            Assert.That(stripes.Price, Is.EqualTo(400));
        }
        /// <summary>Проверяет, что повреждённое определение не принимается за правильное.</summary>
        [Test] public void RejectsDuplicateIdNegativePriceAndWrongSlot()
        {
            var catalog = ScriptableObject.CreateInstance<CosmeticCatalog>();
            try
            {
                catalog.Configure(new[] { new CosmeticItem { Id = "a", Category = CosmeticCategory.Weapon, Slot = CosmeticSlot.Pistol, Price = 350 } });
                Assert.That(catalog.Validate(false), Is.Empty);
                catalog.Configure(new[] { new CosmeticItem { Id = "a", Price = -1, Slot = CosmeticSlot.Pistol }, new CosmeticItem { Id = "a" } });
                Assert.That(catalog.Validate(false).Length, Is.GreaterThanOrEqualTo(3));
                Assert.That(catalog.TryGet("missing", out _), Is.False);
            }
            finally { Object.DestroyImmediate(catalog); }
        }
    }
}
