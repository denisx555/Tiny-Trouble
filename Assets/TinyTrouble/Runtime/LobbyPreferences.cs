using UnityEngine;

namespace TinyTrouble
{
    /// <summary>
    /// Хранит выбранный цвет и громкость между запусками игры.
    /// </summary>
    /// <remarks>
    /// PlayerPrefs — встроенное хранилище Unity для небольших настроек на компьютере игрока.
    /// </remarks>
    public static class LobbyPreferences
    {
        /// <summary>
        /// Ключи — названия, по которым мы записываем и находим громкость и цвет.
        /// </summary>
        const string VolumeKey = "TinyTrouble.Volume", ColorKey = "TinyTrouble.HoodieColor";
        /// <summary>
        /// Приводит громкость к допустимому диапазону.
        /// </summary>
        /// <param name="value">Громкость: 0 означает тишину, 1 — 100%.</param>
        /// <returns>Число от 0 до 1. Для NaN или бесконечности — 1.</returns>
        /// <remarks>
        /// NaN означает, что значение не является обычным числом.
        /// </remarks>
        static float NormalizeVolume(float value) => float.IsNaN(value) || float.IsInfinity(value) ? 1f : Mathf.Clamp01(value);
        /// <summary>
        /// Проверяет, что номер цвета входит в наши четыре варианта.
        /// </summary>
        /// <param name="value">0 — жёлтый, 1 — голубой, 2 — красный, 3 — фиолетовый.</param>
        /// <returns>Допустимый номер цвета или 0, если передан неизвестный номер.</returns>
        static int NormalizeColor(int value) => value >= 0 && value < 4 ? value : 0;
        /// <summary>
        /// Читает сохранённую громкость и проверяет её значение.
        /// </summary>
        /// <returns>Громкость от 0 до 1; если её ещё не сохраняли — 1, то есть 100%.</returns>
        public static float LoadVolume() => NormalizeVolume(PlayerPrefs.GetFloat(VolumeKey, 1f));
        /// <summary>
        /// Читает сохранённый номер цвета толстовки.
        /// </summary>
        /// <returns>Номер от 0 до 3; если записи нет или она неверна — 0, жёлтый цвет.</returns>
        public static int LoadHoodieColor() => NormalizeColor(PlayerPrefs.GetInt(ColorKey, 0));
        /// <summary>
        /// Проверяет громкость и сразу записывает настройку на диск.
        /// </summary>
        /// <param name="value">Новое значение громкости от ползунка.</param>
        /// <returns>Фактически сохранённое число от 0 до 1.</returns>
        public static float SaveVolume(float value)
        {
            value = NormalizeVolume(value);
            PlayerPrefs.SetFloat(VolumeKey, value); PlayerPrefs.Save();
            return value;
        }
        /// <summary>
        /// Проверяет и сохраняет выбранный номер цвета.
        /// </summary>
        /// <param name="value">Номер выбранного образца цвета.</param>
        /// <returns>Фактически сохранённый номер от 0 до 3.</returns>
        public static int SaveHoodieColor(int value)
        {
            value = NormalizeColor(value);
            PlayerPrefs.SetInt(ColorKey, value); PlayerPrefs.Save();
            return value;
        }
    }
}
