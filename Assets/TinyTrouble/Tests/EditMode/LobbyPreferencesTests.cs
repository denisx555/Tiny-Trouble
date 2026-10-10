using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace TinyTrouble.Tests
{
    /// <summary>
    /// Автоматически проверяет сохранение настроек и защиту от неправильных чисел.
    /// </summary>
    public class LobbyPreferencesTests
    {
        /// <summary>
        /// Те же названия настроек, что в игре.
        /// </summary>
        const string VolumeKey = "TinyTrouble.Volume", ColorKey = "TinyTrouble.HoodieColor";
        /// <summary>
        /// Были ли настройки игрока записаны до начала проверки.
        /// </summary>
        bool hadVolume, hadColor;
        /// <summary>
        /// Исходная громкость игрока для восстановления после проверки.
        /// </summary>
        float oldVolume;
        /// <summary>
        /// Исходный номер цвета игрока для восстановления после проверки.
        /// </summary>
        int oldColor;
        /// <summary>
        /// Запоминает настройки игрока и временно удаляет их перед каждой проверкой.
        /// </summary>
        [SetUp] public void Setup()
        {
            hadVolume = PlayerPrefs.HasKey(VolumeKey); oldVolume = PlayerPrefs.GetFloat(VolumeKey);
            hadColor = PlayerPrefs.HasKey(ColorKey); oldColor = PlayerPrefs.GetInt(ColorKey);
            PlayerPrefs.DeleteKey(VolumeKey); PlayerPrefs.DeleteKey(ColorKey);
        }
        /// <summary>
        /// Возвращает исходные настройки после каждой проверки.
        /// </summary>
        [TearDown] public void Restore()
        {
            if (hadVolume) PlayerPrefs.SetFloat(VolumeKey, oldVolume); else PlayerPrefs.DeleteKey(VolumeKey);
            if (hadColor) PlayerPrefs.SetInt(ColorKey, oldColor); else PlayerPrefs.DeleteKey(ColorKey);
            PlayerPrefs.Save();
        }
        /// <summary>
        /// Находит класс настроек и вызывает его метод по имени.
        /// </summary>
        /// <param name="method">Имя метода настроек.</param>
        /// <param name="args">Значения, которые нужно передать методу.</param>
        /// <typeparam name="T">Ожидаемый тип результата: число float или int.</typeparam>
        /// <returns>Результат вызванного метода.</returns>
        /// <remarks>
        /// Reflection — поиск кода во время работы; так первый тест мог сообщить об отсутствии класса до его написания.
        /// </remarks>
        static T Call<T>(string method, params object[] args)
        {
            var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("TinyTrouble.LobbyPreferences")).FirstOrDefault(t => t != null);
            Assert.That(type, Is.Not.Null, "LobbyPreferences отсутствует");
            return (T)type.GetMethod(method, BindingFlags.Public | BindingFlags.Static).Invoke(null, args);
        }
        /// <summary>
        /// Без записанных настроек ожидает жёлтую толстовку и громкость 100%.
        /// </summary>
        [Test] public void MissingPreferencesUseYellowAndFullVolume()
        {
            Assert.That(Call<float>("LoadVolume"), Is.EqualTo(1f));
            Assert.That(Call<int>("LoadHoodieColor"), Is.EqualTo(0));
        }
        /// <summary>
        /// Проверяет запись, возвращённое число и повторное чтение громкости.
        /// </summary>
        /// <param name="input">Исходное число из TestCase — отдельного примера проверки.</param>
        /// <param name="expected">Ожидаемая громкость после проверки числа.</param>
        [TestCase(-1f, 0f)] [TestCase(2f, 1f)] [TestCase(.37f, .37f)]
        [TestCase(float.NaN, 1f)] [TestCase(float.PositiveInfinity, 1f)] [TestCase(float.NegativeInfinity, 1f)]
        public void SaveVolumeNormalizesAndPersists(float input, float expected)
        {
            Assert.That(Call<float>("SaveVolume", input), Is.EqualTo(expected).Within(.0001f));
            Assert.That(PlayerPrefs.GetFloat(VolumeKey), Is.EqualTo(expected).Within(.0001f));
            Assert.That(Call<float>("LoadVolume"), Is.EqualTo(expected).Within(.0001f));
        }
        /// <summary>
        /// Сохраняет неправильное число и проверяет безопасное чтение.
        /// </summary>
        /// <param name="input">Неправильное сохранённое значение.</param>
        /// <param name="expected">Допустимое число, которое должна вернуть игра.</param>
        [TestCase(-3f, 0f)] [TestCase(5f, 1f)] [TestCase(float.NaN, 1f)]
        public void CorruptSavedVolumeIsSafe(float input, float expected)
        {
            PlayerPrefs.SetFloat(VolumeKey, input);
            Assert.That(Call<float>("LoadVolume"), Is.EqualTo(expected));
        }
        /// <summary>
        /// Проверяет сохранение каждого из четырёх цветов.
        /// </summary>
        /// <param name="input">Допустимый номер цвета от 0 до 3.</param>
        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void ColorPersistsAcrossReads(int input)
        {
            Assert.That(Call<int>("SaveHoodieColor", input), Is.EqualTo(input));
            Assert.That(PlayerPrefs.GetInt(ColorKey), Is.EqualTo(input));
            Assert.That(Call<int>("LoadHoodieColor"), Is.EqualTo(input));
        }
        /// <summary>
        /// Проверяет замену неизвестного номера цвета на жёлтый.
        /// </summary>
        /// <param name="input">Номер, которого нет среди наших четырёх цветов.</param>
        [TestCase(-1)] [TestCase(99)] public void InvalidColorFallsBackToYellow(int input)
        {
            PlayerPrefs.SetInt(ColorKey, input);
            Assert.That(Call<int>("LoadHoodieColor"), Is.EqualTo(0));
            Assert.That(Call<int>("SaveHoodieColor", input), Is.EqualTo(0));
        }
    }
}
