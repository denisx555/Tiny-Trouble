using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace TinyTrouble.Tests
{
    /// <summary>Проверяет операции над настоящим сервисом и сохранением в изолированной памяти.</summary>
    public class CosmeticServiceTests
    {
        /// <summary>Сохранённые определения магазина.</summary>
        CosmeticCatalog catalog;
        /// <summary>Заменяет только внешний доступ к диску.</summary>
        MemoryCosmeticPreferences prefs;
        /// <summary>Вызывает обратное действие для проверки защиты от вложенной записи.</summary>
        sealed class ReentrantStore : ICosmeticProfileStore
        {
            /// <summary>Настоящие правила чтения и сериализации.</summary>
            readonly CosmeticProfileStore inner;
            /// <summary>Проверочное действие во время Save.</summary>
            public Action DuringSave;
            /// <summary>Подключает подставной диск.</summary>
            /// <param name="prefs">Доступ к памяти проверки.</param>
            public ReentrantStore(ICosmeticPreferences prefs) { inner = new CosmeticProfileStore(prefs); }
            /// <inheritdoc />
            public CosmeticLoadResult Load(CosmeticCatalog values, int color) => inner.Load(values, color);
            /// <inheritdoc />
            public void Save(CosmeticProfile profile, int? color)
            { var callback = DuringSave; DuringSave = null; callback?.Invoke(); inner.Save(profile, color); }
        }
        /// <summary>Подготавливает каталог и новый временный профиль.</summary>
        [SetUp] public void Setup()
        {
            catalog = AssetDatabase.LoadAssetAtPath<CosmeticCatalog>("Assets/TinyTrouble/Cosmetics/Catalog.asset");
            prefs = new MemoryCosmeticPreferences();
        }
        /// <summary>Создаёт сервис с достаточным тестовым балансом без функции пополнения игры.</summary>
        /// <param name="balance">Запас для конкретной проверки.</param>
        /// <returns>Сервис над временными ключами.</returns>
        CosmeticService Service(int balance = 1000)
        {
            var profile = new CosmeticProfile { Balance = balance };
            profile.EquippedIds[0] = "base-yellow";
            prefs.SetString(CosmeticProfileStore.ProfileKey, JsonUtility.ToJson(profile));
            return new CosmeticService(catalog, new CosmeticProfileStore(prefs), 0);
        }
        /// <summary>Покупка списывает новую цену ровно один раз и не надевает вещь.</summary>
        [Test] public void BuyDeductsNewPriceExactlyOnce()
        {
            var service = Service(); int changed = 0;
            service.Changed += () => changed++;
            Assert.That(service.Buy("stripes"), Is.EqualTo(CosmeticOperationResult.Success));
            Assert.That(service.Snapshot.Balance, Is.EqualTo(600));
            Assert.That(service.Snapshot.OwnedIds, Is.EqualTo(new[] { "stripes" }));
            Assert.That(service.Owns("stripes"), Is.True);
            Assert.That(service.Snapshot.EquippedIds[0], Is.EqualTo("base-yellow"));
            var saved = prefs.GetString(CosmeticProfileStore.ProfileKey);
            Assert.That(service.Buy("stripes"), Is.EqualTo(CosmeticOperationResult.AlreadyOwned));
            Assert.That(service.Snapshot.Balance, Is.EqualTo(600));
            Assert.That(prefs.GetString(CosmeticProfileStore.ProfileKey), Is.EqualTo(saved));
            Assert.That(changed, Is.EqualTo(1));
        }
        /// <summary>Недостаток денег и неизвестный код не меняют сохранение.</summary>
        [Test] public void InsufficientAndUnknownItemDoNotWrite()
        {
            var service = Service(350);
            var saved = prefs.GetString(CosmeticProfileStore.ProfileKey);
            Assert.That(service.Buy("pet-kitten"), Is.EqualTo(CosmeticOperationResult.InsufficientFunds));
            Assert.That(service.Buy("missing"), Is.EqualTo(CosmeticOperationResult.UnknownItem));
            Assert.That(service.Equip("space"), Is.EqualTo(CosmeticOperationResult.NotOwned));
            Assert.That(service.Snapshot.Balance, Is.EqualTo(350));
            Assert.That(prefs.GetString(CosmeticProfileStore.ProfileKey), Is.EqualTo(saved));
        }
        /// <summary>Замена скина пистолета сохраняет автомат и обе покупки пистолета.</summary>
        [Test] public void EquipUsesIndependentWeaponSlots()
        {
            var service = Service(5000);
            foreach (var id in new[] { "pistol-pop", "rifle-lime", "pistol-ice" })
                Assert.That(service.Buy(id), Is.EqualTo(CosmeticOperationResult.Success));
            foreach (var id in new[] { "pistol-pop", "rifle-lime", "pistol-ice" })
                Assert.That(service.Equip(id), Is.EqualTo(CosmeticOperationResult.Success));
            Assert.That(service.Snapshot.EquippedIds[1], Is.EqualTo("pistol-ice"));
            Assert.That(service.Snapshot.EquippedIds[2], Is.EqualTo("rifle-lime"));
            Assert.That(service.Snapshot.OwnedIds, Is.EquivalentTo(new[] { "pistol-pop", "rifle-lime", "pistol-ice" }));
            var reload = new CosmeticService(catalog, new CosmeticProfileStore(prefs), 0);
            Assert.That(reload.Snapshot.EquippedIds[1], Is.EqualTo("pistol-ice"));
            Assert.That(reload.Snapshot.EquippedIds[2], Is.EqualTo("rifle-lime"));
        }
        /// <summary>Одежда, питомец и след занимают независимые места; тот же выбор не сохраняется снова.</summary>
        [Test] public void FreeColorAndOtherSlotsRemainIndependent()
        {
            var service = Service(3000);
            Assert.That(service.Equip("base-blue"), Is.EqualTo(CosmeticOperationResult.Success));
            Assert.That(prefs.GetInt(CosmeticProfileStore.ColorKey), Is.EqualTo(1));
            foreach (var id in new[] { "pet-kitten", "trail-stars" }) { service.Buy(id); service.Equip(id); }
            Assert.That(service.Snapshot.EquippedIds[0], Is.EqualTo("base-blue"));
            Assert.That(service.Snapshot.EquippedIds[7], Is.EqualTo("pet-kitten"));
            Assert.That(service.Snapshot.EquippedIds[8], Is.EqualTo("trail-stars"));
            var saved = prefs.GetString(CosmeticProfileStore.ProfileKey);
            Assert.That(service.Equip("base-blue"), Is.EqualTo(CosmeticOperationResult.AlreadyEquipped));
            Assert.That(prefs.GetString(CosmeticProfileStore.ProfileKey), Is.EqualTo(saved));
        }
        /// <summary>Неудачная запись не публикует покупку и не расходует монеты.</summary>
        [Test] public void WriteFailureNeverPublishes()
        {
            var service = Service(); int changed = 0;
            service.Changed += () => changed++;
            prefs.FailFlush = true;
            Assert.That(service.Buy("stripes"), Is.EqualTo(CosmeticOperationResult.SaveFailed));
            Assert.That(service.Snapshot.Balance, Is.EqualTo(1000));
            Assert.That(service.Snapshot.OwnedIds, Is.Empty);
            Assert.That(changed, Is.Zero);
            Assert.That(service.Error, Is.Not.Null.And.Not.Empty);
        }
        /// <summary>Повреждённый профиль блокирует покупку и сохранение выбора.</summary>
        [Test] public void ReadOnlyNeverPublishes()
        {
            prefs.SetString(CosmeticProfileStore.ProfileKey, "сломано");
            var blocked = new CosmeticService(catalog, new CosmeticProfileStore(prefs), 0);
            Assert.That(blocked.Buy("stripes"), Is.EqualTo(CosmeticOperationResult.ReadOnly));
            Assert.That(blocked.Equip("base-blue"), Is.EqualTo(CosmeticOperationResult.ReadOnly));
        }
        /// <summary>Вложенная покупка во время Save не обходит защиту операции.</summary>
        [Test] public void ReentrancyNeverPublishesNestedPurchase()
        {
            var store = new ReentrantStore(prefs);
            var service = new CosmeticService(catalog, store, 0);
            var nested = CosmeticOperationResult.Success;
            store.DuringSave = () => nested = service.Buy("space");
            Assert.That(service.Buy("stripes"), Is.EqualTo(CosmeticOperationResult.Success));
            Assert.That(nested, Is.EqualTo(CosmeticOperationResult.Busy));
            var copy = service.Snapshot;
            copy.Balance = 999999; copy.OwnedIds[0] = "space";
            Assert.That(service.Snapshot.Balance, Is.EqualTo(600));
            Assert.That(service.Snapshot.OwnedIds, Is.EqualTo(new[] { "stripes" }));
        }
    }
}
