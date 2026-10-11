using System;
using System.Collections;
using UnityEngine.TestTools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TinyTrouble.Tests
{
    /// <summary>На время теста сохраняет личные настройки, а затем возвращает их без потери ключей.</summary>
    public sealed class CosmeticPrefsScope : IDisposable
    {
        /// <summary>Четыре ключа, которые может менять лобби.</summary>
        static readonly string[] Keys = { CosmeticProfileStore.ProfileKey, CosmeticProfileStore.RecoveryKey, CosmeticProfileStore.ColorKey, "TinyTrouble.Volume" };
        /// <summary>Наличие каждого ключа до начала проверки.</summary>
        readonly bool[] exists = new bool[4];
        /// <summary>Исходные строки профиля и резервной копии.</summary>
        readonly string[] strings = new string[2];
        /// <summary>Исходный цвет и громкость.</summary>
        readonly int color;
        /// <summary>Громкость, сохранённая на диске.</summary>
        readonly float volume;
        /// <summary>Не повторяет восстановление при нескольких вызовах Dispose.</summary>
        bool disposed;
        /// <summary>Запоминает значения и готовит чистые настройки для теста.</summary>
        public CosmeticPrefsScope()
        {
            for (var i = 0; i < Keys.Length; i++) exists[i] = PlayerPrefs.HasKey(Keys[i]);
            strings[0] = PlayerPrefs.GetString(Keys[0]); strings[1] = PlayerPrefs.GetString(Keys[1]);
            color = PlayerPrefs.GetInt(Keys[2]); volume = PlayerPrefs.GetFloat(Keys[3]);
            foreach (var key in Keys) PlayerPrefs.DeleteKey(key);
        }
        /// <summary>Возвращает наличие и значение всех исходных ключей даже после неудачного теста.</summary>
        public void Dispose()
        {
            if (disposed) return; disposed = true;
            for (var i = 0; i < Keys.Length; i++)
            {
                if (!exists[i]) PlayerPrefs.DeleteKey(Keys[i]);
                else if (i < 2) PlayerPrefs.SetString(Keys[i], strings[i]);
                else if (i == 2) PlayerPrefs.SetInt(Keys[i], color);
                else PlayerPrefs.SetFloat(Keys[i], volume);
            }
            PlayerPrefs.Save();
        }
    }

    /// <summary>Общий порядок тестов лобби: сначала убрать слушателей сцены, затем вернуть настройки.</summary>
    public abstract class CosmeticLobbyTestScope
    {
        /// <summary>Сохранённые настройки пользователя до теста.</summary>
        CosmeticPrefsScope preferences;
        /// <summary>Исходная общая громкость Unity.</summary>
        float audio;
        /// <summary>Готовит изолированные настройки перед подготовкой производного теста.</summary>
        /// <returns>Подготовка до загрузки сцены производным тестом.</returns>
        [UnitySetUp] public IEnumerator IsolateCosmeticPreferences() { audio = AudioListener.volume; preferences = new CosmeticPrefsScope(); yield break; }
        /// <summary>Удаляет тестовые сцены и возвращает пользовательский профиль последним шагом очистки.</summary>
        /// <returns>Очистка после остальных шагов завершения теста.</returns>
        [UnityTearDown] public IEnumerator RestoreCosmeticPreferences()
        {
            try
            {
                for (var i = 0; i < SceneManager.sceneCount; i++)
                {
                    var scene = SceneManager.GetSceneAt(i);
                    if (scene.name == "Lobby" && scene.isLoaded) foreach (var root in scene.GetRootGameObjects()) UnityEngine.Object.DestroyImmediate(root);
                }
            }
            finally { preferences?.Dispose(); preferences = null; AudioListener.volume = audio; }
            yield break;
        }
    }
}
