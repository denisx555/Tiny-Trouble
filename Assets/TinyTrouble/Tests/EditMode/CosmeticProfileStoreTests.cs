using System;
using System.Collections.Generic;
using NUnit.Framework;
using TinyTrouble.EditorTools;
using UnityEngine;

namespace TinyTrouble.Tests
{
    /// <summary>Подставное хранилище отделяет проверку от настоящих монет пользователя.</summary>
    internal sealed class MemoryCosmeticPreferences : ICosmeticPreferences
    {
        /// <summary>Документы и числа представлены строками только внутри проверки.</summary>
        public readonly Dictionary<string, string> Values = new Dictionary<string, string>();
        /// <summary>Следующая передача на диск вызывает ошибку.</summary>
        public bool FailFlush;
        /// <inheritdoc />
        public bool HasKey(string key) => Values.ContainsKey(key);
        /// <inheritdoc />
        public string GetString(string key) => Values.TryGetValue(key, out var value) ? value : "";
        /// <inheritdoc />
        public int GetInt(string key) => int.TryParse(GetString(key), out var value) ? value : 0;
        /// <inheritdoc />
        public void SetString(string key, string value) { Values[key] = value; }
        /// <inheritdoc />
        public void SetInt(string key, int value) { Values[key] = value.ToString(); }
        /// <inheritdoc />
        public void DeleteKey(string key) { Values.Remove(key); }
        /// <inheritdoc />
        public void Flush() { if (FailFlush) throw new InvalidOperationException("Проверочная ошибка диска"); }
    }
    /// <summary>Проверяет выдачу стартовых денег, перенос цвета и сохранность документа при ошибках.</summary>
    public class CosmeticProfileStoreTests
    {
        /// <summary>Проверяет первый запуск и отсутствие повторного пополнения после покупки.</summary>
        [Test] public void FirstLoadMigratesColorAndGrantsOnce()
        {
            var prefs = new MemoryCosmeticPreferences();
            prefs.SetString("TinyTrouble.Volume", ".37");
            var catalog = CosmeticAssetBuilder.BuildCatalog();
            var store = new CosmeticProfileStore(prefs);
            var first = store.Load(catalog, 2);
            Assert.That(first.CanWrite, Is.True);
            Assert.That(first.Profile.Balance, Is.EqualTo(1000));
            Assert.That(first.Profile.OwnedIds, Is.Empty);
            Assert.That(first.Profile.EquippedIds[0], Is.EqualTo("base-red"));
            first.Profile.Balance = 600;
            store.Save(first.Profile, null);
            Assert.That(store.Load(catalog, 0).Profile.Balance, Is.EqualTo(600));
            Assert.That(prefs.GetString("TinyTrouble.Volume"), Is.EqualTo(".37"));
        }
        /// <summary>Сохраняет неизвестные покупки, но исправляет недоступный выбор.</summary>
        [Test] public void NormalizeRetainsUnknownOwnedIdsAndRepairsSlots()
        {
            var prefs = new MemoryCosmeticPreferences();
            var profile = new CosmeticProfile { Balance = -1, OwnedIds = new[] { "lost-id", "lost-id", "rifle-lime" } };
            profile.EquippedIds[0] = "space";
            profile.EquippedIds[1] = "rifle-lime";
            profile.EquippedIds[2] = "rifle-lime";
            prefs.SetString(CosmeticProfileStore.ProfileKey, JsonUtility.ToJson(profile));
            var result = new CosmeticProfileStore(prefs).Load(CosmeticAssetBuilder.BuildCatalog(), 0);
            Assert.That(result.CanWrite, Is.True);
            Assert.That(result.Profile.Balance, Is.Zero);
            Assert.That(result.Profile.OwnedIds, Is.EquivalentTo(new[] { "lost-id", "rifle-lime" }));
            Assert.That(result.Profile.EquippedIds[0], Is.EqualTo("base-yellow"));
            Assert.That(result.Profile.EquippedIds[1], Is.EqualTo("pistol-gray"));
            Assert.That(result.Profile.EquippedIds[2], Is.EqualTo("rifle-lime"));
        }
        /// <summary>Повреждение не выдаёт новый стартовый запас и не уничтожает исходную строку.</summary>
        /// <param name="source">Повреждённый либо несовместимый документ.</param>
        [TestCase("сломано")]
        [TestCase("{\"Version\":99,\"Balance\":456}")]
        [TestCase("{}")]
        public void CorruptAndUnknownVersionAreReadOnly(string source)
        {
            var prefs = new MemoryCosmeticPreferences();
            prefs.SetString(CosmeticProfileStore.ProfileKey, source);
            var store = new CosmeticProfileStore(prefs);
            var result = store.Load(CosmeticAssetBuilder.BuildCatalog(), 1);
            Assert.That(result.CanWrite, Is.False);
            Assert.That(result.Profile.Balance, Is.Zero);
            Assert.That(prefs.GetString(CosmeticProfileStore.ProfileKey), Is.EqualTo(source));
            Assert.That(prefs.GetString(CosmeticProfileStore.RecoveryKey), Is.EqualTo(source));
            prefs.SetString(CosmeticProfileStore.RecoveryKey, "первая копия");
            store.Load(CosmeticAssetBuilder.BuildCatalog(), 1);
            Assert.That(prefs.GetString(CosmeticProfileStore.RecoveryKey), Is.EqualTo("первая копия"));
        }
        /// <summary>Ошибка записи возвращает прежний профиль и цвет даже при первом сохранении.</summary>
        /// <param name="existing">Был ли профиль до операции.</param>
        [TestCase(true)] [TestCase(false)]
        public void FailedFlushRestoresProfileAndLegacyColor(bool existing)
        {
            var prefs = new MemoryCosmeticPreferences();
            if (existing) prefs.SetString(CosmeticProfileStore.ProfileKey, "прежняя строка");
            prefs.SetInt(CosmeticProfileStore.ColorKey, 2);
            prefs.FailFlush = true;
            Assert.Throws<InvalidOperationException>(() => new CosmeticProfileStore(prefs).Save(new CosmeticProfile { Balance = 300 }, 1));
            Assert.That(prefs.HasKey(CosmeticProfileStore.ProfileKey), Is.EqualTo(existing));
            if (existing) Assert.That(prefs.GetString(CosmeticProfileStore.ProfileKey), Is.EqualTo("прежняя строка"));
            Assert.That(prefs.GetInt(CosmeticProfileStore.ColorKey), Is.EqualTo(2));
        }
        /// <summary>Неудачное создание профиля не разрешает покупать незаписанными деньгами.</summary>
        [Test] public void FirstSaveFailureIsReadOnlyAndLeavesNoProfile()
        {
            var prefs = new MemoryCosmeticPreferences { FailFlush = true };
            var result = new CosmeticProfileStore(prefs).Load(CosmeticAssetBuilder.BuildCatalog(), 0);
            Assert.That(result.CanWrite, Is.False);
            Assert.That(prefs.HasKey(CosmeticProfileStore.ProfileKey), Is.False);
            Assert.That(result.Error, Is.Not.Null.And.Not.Empty);
        }
    }
}
