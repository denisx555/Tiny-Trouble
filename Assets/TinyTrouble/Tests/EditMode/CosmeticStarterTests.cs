using System.Linq;
using NUnit.Framework;
using TinyTrouble.EditorTools;
using UnityEngine;

namespace TinyTrouble.Tests
{
    /// <summary>Проверяет бесплатный серый набор и сохранность прежних покупок.</summary>
    public class CosmeticStarterTests
    {
        /// <summary>Первый запуск сразу выбирает серые предметы во всех шести местах снаряжения.</summary>
        [Test] public void NewPlayerCanUseGrayEquipmentWithoutSpendingCoins()
        {
            var catalog = CosmeticAssetBuilder.BuildCatalog();
            var service = new CosmeticService(catalog, new CosmeticProfileStore(new MemoryCosmeticPreferences()), 2);
            var profile = service.Snapshot;
            foreach (var id in new[] { "pistol-gray", "rifle-gray", "shotgun-gray", "parachute-gray", "trail-gray", "pickaxe-gray" })
            {
                Assert.That(catalog.TryGet(id, out var item), Is.True, id);
                Assert.That(item.Price, Is.Zero, id);
                Assert.That(service.Owns(id), Is.True, id);
                Assert.That(profile.EquippedIds[(int)item.Slot], Is.EqualTo(id));
                Assert.That(service.Buy(id), Is.EqualTo(CosmeticOperationResult.AlreadyOwned));
            }
            Assert.That(profile.Balance, Is.EqualTo(1000));
            Assert.That(service.Snapshot.Balance, Is.EqualTo(1000));
            Assert.That(profile.OwnedIds, Is.Empty);
            Assert.That(profile.EquippedIds[0], Is.EqualTo("base-red"));
            foreach (var slot in new[] { 4, 5, 7 }) Assert.That(profile.EquippedIds[slot], Is.Null.Or.Empty);
        }

        /// <summary>Владелец цветного скина может бесплатно вернуться к серому после перезапуска.</summary>
        [Test] public void ExistingPurchasesSurviveAndGrayCanBeEquippedAgain()
        {
            var catalog = CosmeticAssetBuilder.BuildCatalog();
            var prefs = new MemoryCosmeticPreferences();
            var saved = new CosmeticProfile { Balance = 470, OwnedIds = new[] { "rifle-lime", "lost-id" } };
            saved.EquippedIds[0] = "base-purple"; saved.EquippedIds[2] = "rifle-lime";
            prefs.SetString(CosmeticProfileStore.ProfileKey, JsonUtility.ToJson(saved));
            var store = new CosmeticProfileStore(prefs);
            var service = new CosmeticService(catalog, store, 0);
            Assert.That(service.Snapshot.EquippedIds[2], Is.EqualTo("rifle-lime"));
            Assert.That(service.Equip("rifle-gray"), Is.EqualTo(CosmeticOperationResult.Success));
            var reopened = new CosmeticService(catalog, store, 0);
            Assert.That(reopened.Snapshot.EquippedIds[2], Is.EqualTo("rifle-gray"));
            Assert.That(reopened.Snapshot.Balance, Is.EqualTo(470));
            Assert.That(reopened.Snapshot.OwnedIds, Is.EquivalentTo(new[] { "rifle-lime", "lost-id" }));
            Assert.That(reopened.Snapshot.EquippedIds[0], Is.EqualTo("base-purple"));
        }
    }
}
