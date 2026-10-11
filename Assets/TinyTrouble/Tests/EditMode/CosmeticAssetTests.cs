using System.IO;
using System.Linq;
using NUnit.Framework;
using TinyTrouble.EditorTools;
using UnityEditor;

namespace TinyTrouble.Tests
{
    /// <summary>Проверяет реальные ссылки товаров и безопасное повторное обновление ассетов.</summary>
    public class CosmeticAssetTests
    {
        /// <summary>Каждая карточка имеет собственное изображение и необходимую модель или материал.</summary>
        [Test] public void SavedCatalogHasVisualsForEveryItem()
        {
            var catalog = CosmeticAssetBuilder.BuildCatalog();
            CosmeticAssetBuilder.BuildVisuals(catalog);
            Assert.That(catalog.Validate(true), Is.Empty);
            foreach (var item in catalog.Items) Assert.That(item.Icon, Is.Not.Null, item.Id);
            Assert.That(catalog.Items.Where(i => i.BasicColorIndex < 0).Select(i => AssetDatabase.GetAssetPath(i.Icon)).Distinct().Count(), Is.EqualTo(27));
        }
        /// <summary>Повторная подготовка сохраняет GUID и не переписывает пользовательского героя.</summary>
        [Test] public void BuildVisualsTwicePreservesGuidsAndUserRig()
        {
            var rig = File.ReadAllBytes("Assets/TinyTrouble/Prefabs/LobbyCharacter.prefab");
            var catalog = CosmeticAssetBuilder.BuildCatalog();
            CosmeticAssetBuilder.BuildVisuals(catalog);
            Assert.That(catalog.Validate(true), Is.Empty);
            var paths = catalog.Items.Select(i => AssetDatabase.GetAssetPath(i.Icon)).ToArray();
            var guids = paths.Select(AssetDatabase.AssetPathToGUID).ToArray();
            CosmeticAssetBuilder.BuildVisuals(catalog);
            Assert.That(paths.Select(AssetDatabase.AssetPathToGUID), Is.EqualTo(guids));
            Assert.That(File.ReadAllBytes("Assets/TinyTrouble/Prefabs/LobbyCharacter.prefab"), Is.EqualTo(rig));
            Assert.That(catalog.Items.Count, Is.EqualTo(31));
        }
    }
}
