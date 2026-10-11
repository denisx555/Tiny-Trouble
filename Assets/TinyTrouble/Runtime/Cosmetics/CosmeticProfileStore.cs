using System;
using System.Linq;
using UnityEngine;

namespace TinyTrouble
{
    /// <summary>Хранит профиль одним документом, проверяет его и откатывает неудачную запись.</summary>
    public sealed class CosmeticProfileStore : ICosmeticProfileStore
    {
        /// <summary>Ключ документа коллекции.</summary>
        public const string ProfileKey = "TinyTrouble.Cosmetics.Profile";
        /// <summary>Ключ первой резервной копии повреждённого документа.</summary>
        public const string RecoveryKey = ProfileKey + ".Recovery";
        /// <summary>Прежний ключ бесплатного цвета одежды.</summary>
        public const string ColorKey = "TinyTrouble.HoodieColor";
        /// <summary>Заменяемый способ чтения и записи.</summary>
        readonly ICosmeticPreferences preferences;
        /// <summary>Принимает настоящий или проверочный доступ к настройкам.</summary>
        /// <param name="preferences">Доступ к ключам.</param>
        public CosmeticProfileStore(ICosmeticPreferences preferences) { this.preferences = preferences; }
        /// <inheritdoc />
        public CosmeticLoadResult Load(CosmeticCatalog catalog, int legacyColor)
        {
            var fallback = new CosmeticProfile();
            fallback.EquippedIds[0] = BasicId(legacyColor);
            if (!preferences.HasKey(ProfileKey))
            {
                var first = fallback.Clone();
                first.Balance = 1000;
                try { Save(first, null); return new CosmeticLoadResult { Profile = first, CanWrite = true }; }
                catch (Exception) { return new CosmeticLoadResult { Profile = fallback, CanWrite = false, Error = "Не удалось создать сохранение. Покупки пока недоступны." }; }
            }
            var source = preferences.GetString(ProfileKey);
            try
            {
                // Нулевой номер до чтения отличает отсутствующее поле Version от версии 1.
                var profile = new CosmeticProfile { Version = 0, OwnedIds = null, EquippedIds = null };
                JsonUtility.FromJsonOverwrite(source, profile);
                if (profile.Version != 1 || profile.OwnedIds == null || profile.EquippedIds == null)
                    throw new FormatException("Формат сохранения неизвестен");
                Normalize(profile, catalog, legacyColor);
                return new CosmeticLoadResult { Profile = profile, CanWrite = true };
            }
            catch (Exception)
            {
                // Исходный документ не заменяется новым, а первая резервная копия не затирается.
                if (!preferences.HasKey(RecoveryKey))
                    try { preferences.SetString(RecoveryKey, source); preferences.Flush(); }
                    catch (Exception) { }
                return new CosmeticLoadResult { Profile = fallback, CanWrite = false, Error = "Не удалось прочитать коллекцию. Сохранение оставлено для восстановления." };
            }
        }
        /// <inheritdoc />
        public void Save(CosmeticProfile profile, int? legacyColor)
        {
            var json = JsonUtility.ToJson(profile);
            var hadProfile = preferences.HasKey(ProfileKey);
            var oldProfile = preferences.GetString(ProfileKey);
            var hadColor = preferences.HasKey(ColorKey);
            var oldColor = preferences.GetInt(ColorKey);
            try
            {
                preferences.SetString(ProfileKey, json);
                if (legacyColor.HasValue) preferences.SetInt(ColorKey, legacyColor.Value);
                preferences.Flush();
            }
            catch
            {
                // Откатываем память настроек; это не обещание защиты диска от потери питания.
                try
                {
                    if (hadProfile) preferences.SetString(ProfileKey, oldProfile); else preferences.DeleteKey(ProfileKey);
                    if (legacyColor.HasValue)
                    { if (hadColor) preferences.SetInt(ColorKey, oldColor); else preferences.DeleteKey(ColorKey); }
                }
                catch (Exception) { }
                throw;
            }
        }
        /// <summary>Возвращает постоянный код прежнего бесплатного цвета.</summary>
        /// <param name="color">Номер из прежнего меню.</param>
        /// <returns>Код одного из четырёх бесплатных цветов.</returns>
        static string BasicId(int color) => new[] { "base-yellow", "base-blue", "base-red", "base-purple" }[Mathf.Clamp(color, 0, 3)];
        /// <summary>Убирает дубликаты покупок и исправляет неразрешённые выборы.</summary>
        /// <param name="profile">Прочитанные данные.</param>
        /// <param name="catalog">Известные определения.</param>
        /// <param name="legacyColor">Бесплатный цвет для замены неверной одежды.</param>
        static void Normalize(CosmeticProfile profile, CosmeticCatalog catalog, int legacyColor)
        {
            profile.Balance = Math.Max(0, profile.Balance);
            profile.OwnedIds = profile.OwnedIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToArray();
            var choices = new string[10];
            for (var i = 0; i < Math.Min(10, profile.EquippedIds.Length); i++)
                if (catalog.TryGet(profile.EquippedIds[i], out var item) && (int)item.Slot == i &&
                    (item.BasicColorIndex >= 0 || profile.OwnedIds.Contains(item.Id))) choices[i] = item.Id;
            if (string.IsNullOrEmpty(choices[0])) choices[0] = BasicId(legacyColor);
            profile.EquippedIds = choices;
        }
    }
}
