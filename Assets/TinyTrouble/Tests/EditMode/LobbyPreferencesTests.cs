using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace TinyTrouble.Tests
{
    public class LobbyPreferencesTests
    {
        const string VolumeKey = "TinyTrouble.Volume", ColorKey = "TinyTrouble.HoodieColor";
        bool hadVolume, hadColor;
        float oldVolume;
        int oldColor;
        [SetUp] public void Setup()
        {
            hadVolume = PlayerPrefs.HasKey(VolumeKey); oldVolume = PlayerPrefs.GetFloat(VolumeKey);
            hadColor = PlayerPrefs.HasKey(ColorKey); oldColor = PlayerPrefs.GetInt(ColorKey);
            PlayerPrefs.DeleteKey(VolumeKey); PlayerPrefs.DeleteKey(ColorKey);
        }
        [TearDown] public void Restore()
        {
            if (hadVolume) PlayerPrefs.SetFloat(VolumeKey, oldVolume); else PlayerPrefs.DeleteKey(VolumeKey);
            if (hadColor) PlayerPrefs.SetInt(ColorKey, oldColor); else PlayerPrefs.DeleteKey(ColorKey);
            PlayerPrefs.Save();
        }
        static T Call<T>(string method, params object[] args)
        {
            var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("TinyTrouble.LobbyPreferences")).FirstOrDefault(t => t != null);
            Assert.That(type, Is.Not.Null, "LobbyPreferences отсутствует");
            return (T)type.GetMethod(method, BindingFlags.Public | BindingFlags.Static).Invoke(null, args);
        }
        [Test] public void MissingPreferencesUseYellowAndFullVolume()
        {
            Assert.That(Call<float>("LoadVolume"), Is.EqualTo(1f));
            Assert.That(Call<int>("LoadHoodieColor"), Is.EqualTo(0));
        }
        [TestCase(-1f, 0f)] [TestCase(2f, 1f)] [TestCase(.37f, .37f)]
        [TestCase(float.NaN, 1f)] [TestCase(float.PositiveInfinity, 1f)] [TestCase(float.NegativeInfinity, 1f)]
        public void SaveVolumeNormalizesAndPersists(float input, float expected)
        {
            Assert.That(Call<float>("SaveVolume", input), Is.EqualTo(expected).Within(.0001f));
            Assert.That(PlayerPrefs.GetFloat(VolumeKey), Is.EqualTo(expected).Within(.0001f));
            Assert.That(Call<float>("LoadVolume"), Is.EqualTo(expected).Within(.0001f));
        }
        [TestCase(-3f, 0f)] [TestCase(5f, 1f)] [TestCase(float.NaN, 1f)]
        public void CorruptSavedVolumeIsSafe(float input, float expected)
        {
            PlayerPrefs.SetFloat(VolumeKey, input);
            Assert.That(Call<float>("LoadVolume"), Is.EqualTo(expected));
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void ColorPersistsAcrossReads(int input)
        {
            Assert.That(Call<int>("SaveHoodieColor", input), Is.EqualTo(input));
            Assert.That(PlayerPrefs.GetInt(ColorKey), Is.EqualTo(input));
            Assert.That(Call<int>("LoadHoodieColor"), Is.EqualTo(input));
        }
        [TestCase(-1)] [TestCase(99)] public void InvalidColorFallsBackToYellow(int input)
        {
            PlayerPrefs.SetInt(ColorKey, input);
            Assert.That(Call<int>("LoadHoodieColor"), Is.EqualTo(0));
            Assert.That(Call<int>("SaveHoodieColor", input), Is.EqualTo(0));
        }
    }
}
