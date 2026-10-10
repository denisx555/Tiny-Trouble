using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TinyTrouble.Tests
{
    public class LobbySceneTests
    {
        [Test] public void LobbySceneExistsAndIsFirstBuildScene()
        {
            Assert.That(File.Exists("Assets/Scenes/Lobby.unity"), Is.True, "Lobby scene отсутствует");
            Assert.That(EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                Is.EqualTo(new[] { "Assets/Scenes/Lobby.unity", "Assets/Scenes/SampleScene.unity" }));
        }
        [Test] public void SavedSceneHasWiredControlsAndCharacterPrefab()
        {
            Assert.That(File.Exists("Assets/Scenes/Lobby.unity"), Is.True, "Lobby scene отсутствует");
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene = EditorSceneManager.OpenScene("Assets/Scenes/Lobby.unity");
                var roots = scene.GetRootGameObjects();
                Assert.That(roots.SelectMany(r => r.GetComponentsInChildren<EventSystem>(true)).Count(), Is.EqualTo(1));
                Assert.That(roots.SelectMany(r => r.GetComponentsInChildren<Camera>(true)).Count(), Is.EqualTo(1));
                var controller = roots.SelectMany(r => r.GetComponentsInChildren<MonoBehaviour>(true))
                    .Single(m => m != null && m.GetType().Name == "LobbyController");
                var so = new SerializedObject(controller);
                foreach (var field in new[] { "panels", "tabs", "playButton", "volumeSlider", "volumeText", "colorButtons", "hoodieRenderers", "statusText" })
                {
                    var property = so.FindProperty(field);
                    Assert.That(property, Is.Not.Null, field);
                    if (property.isArray)
                    {
                        Assert.That(property.arraySize, Is.GreaterThan(0), field);
                        for (var i = 0; i < property.arraySize; i++) Assert.That(property.GetArrayElementAtIndex(i).objectReferenceValue, Is.Not.Null, field);
                    }
                    else Assert.That(property.objectReferenceValue, Is.Not.Null, field);
                }
                foreach (var button in roots.SelectMany(r => r.GetComponentsInChildren<Button>(true)))
                    Assert.That(button.onClick.GetPersistentEventCount(), Is.GreaterThan(0), button.name);
                Assert.That(PrefabUtility.IsPartOfPrefabInstance(GameObject.Find("LobbyCharacter")), Is.True);
                foreach (var t in roots.SelectMany(r => r.GetComponentsInChildren<Text>(true))) Assert.That(t.font, Is.Not.Null, t.name);
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }
    }
}
