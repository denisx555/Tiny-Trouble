using UnityEngine;

namespace TinyTrouble
{
    public static class LobbyPreferences
    {
        const string VolumeKey = "TinyTrouble.Volume", ColorKey = "TinyTrouble.HoodieColor";
        static float NormalizeVolume(float value) => float.IsNaN(value) || float.IsInfinity(value) ? 1f : Mathf.Clamp01(value);
        static int NormalizeColor(int value) => value >= 0 && value < 4 ? value : 0;
        public static float LoadVolume() => NormalizeVolume(PlayerPrefs.GetFloat(VolumeKey, 1f));
        public static int LoadHoodieColor() => NormalizeColor(PlayerPrefs.GetInt(ColorKey, 0));
        public static float SaveVolume(float value)
        {
            value = NormalizeVolume(value);
            PlayerPrefs.SetFloat(VolumeKey, value); PlayerPrefs.Save();
            return value;
        }
        public static int SaveHoodieColor(int value)
        {
            value = NormalizeColor(value);
            PlayerPrefs.SetInt(ColorKey, value); PlayerPrefs.Save();
            return value;
        }
    }
}
