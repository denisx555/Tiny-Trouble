using System.Linq;
using NUnit.Framework;
using TinyTrouble.EditorTools;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TinyTrouble.Tests
{
    /// <summary>Проверяет сохранённые ссылки и повторное безопасное добавление магазина.</summary>
    public class CosmeticSceneTests
    {
        /// <summary>Два обновления сохраняют ручной объект и положение героя без дубликатов интерфейса.</summary>
        [Test] public void UpgradeTwicePreservesManualObjectsAndLinks()
        {
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene = EditorSceneManager.OpenScene("Assets/Scenes/Lobby.unity");
                var hero = scene.GetRootGameObjects().Single(r => r.name == "LobbyCharacter"); var position = hero.transform.position;
                var manual = new GameObject("ManualTestObject"); manual.transform.position = new Vector3(4, 3, 2);
                var catalog = AssetDatabase.LoadAssetAtPath<CosmeticCatalog>(CosmeticAssetBuilder.Root + "/Catalog.asset");
                CosmeticUIBuilder.Upgrade(scene, catalog); CosmeticUIBuilder.Upgrade(scene, catalog);
                var roots = scene.GetRootGameObjects();
                Assert.That(roots.SelectMany(r => r.GetComponentsInChildren<LobbyController>(true)).Count(), Is.EqualTo(1));
                Assert.That(roots.SelectMany(r => r.GetComponentsInChildren<EventSystem>(true)).Count(), Is.EqualTo(1));
                var controller = roots.SelectMany(r => r.GetComponentsInChildren<LobbyController>(true)).Single(); var data = new SerializedObject(controller);
                Assert.That(data.FindProperty("tabs").arraySize, Is.EqualTo(4)); Assert.That(data.FindProperty("panels").arraySize, Is.EqualTo(4));
                foreach (var field in new[] { "catalog", "appearance", "preview", "shopPanel", "inventoryPanel", "balanceText" }) Assert.That(data.FindProperty(field).objectReferenceValue, Is.Not.Null, field);
                Assert.That(roots.SelectMany(r => r.GetComponentsInChildren<Camera>(true)).Count(c => c.name == "CosmeticPreviewCamera"), Is.EqualTo(1));
                Assert.That(hero.transform.position, Is.EqualTo(position)); Assert.That(manual.transform.position, Is.EqualTo(new Vector3(4, 3, 2)));
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }
    }
}
