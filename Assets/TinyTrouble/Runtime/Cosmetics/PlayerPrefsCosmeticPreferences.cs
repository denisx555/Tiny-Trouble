using UnityEngine;

namespace TinyTrouble
{
    /// <summary>Сохраняет настройки средствами Unity; проверочный код использует другой класс.</summary>
    public sealed class PlayerPrefsCosmeticPreferences : ICosmeticPreferences
    {
        /// <inheritdoc />
        public bool HasKey(string key) => PlayerPrefs.HasKey(key);
        /// <inheritdoc />
        public string GetString(string key) => PlayerPrefs.GetString(key);
        /// <inheritdoc />
        public int GetInt(string key) => PlayerPrefs.GetInt(key);
        /// <inheritdoc />
        public void SetString(string key, string value) => PlayerPrefs.SetString(key, value);
        /// <inheritdoc />
        public void SetInt(string key, int value) => PlayerPrefs.SetInt(key, value);
        /// <inheritdoc />
        public void DeleteKey(string key) => PlayerPrefs.DeleteKey(key);
        /// <inheritdoc />
        public void Flush() => PlayerPrefs.Save();
    }
}
