using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TinyTrouble.Tests
{
    /// <summary>Проверяет примерку настоящих моделей, движение следов и освобождение камеры.</summary>
    public class CosmeticPreviewTests
    {
        /// <summary>Объекты отдельного тестового просмотра.</summary>
        GameObject root, hero, stage, anchor, cameraObject;
        /// <summary>Рабочий каталог и сохранённый вид.</summary>
        CosmeticCatalog catalog;
        /// <summary>Компоненты примерки и личного материала.</summary>
        CosmeticPreview preview;
        /// <summary>Исходный снимок для проверок неизменности.</summary>
        CosmeticProfile profile;
        /// <summary>Создаёт изолированный просмотр без загрузки лобби и записи настоящего профиля.</summary>
        [SetUp] public void CreatePreview()
        {
            catalog = AssetDatabase.LoadAssetAtPath<CosmeticCatalog>("Assets/TinyTrouble/Cosmetics/Catalog.asset");
            root = new GameObject("TestCosmeticPreview");
            hero = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TinyTrouble/Prefabs/LobbyCharacter.prefab"));
            hero.GetComponent<CharacterLife>().enabled = false; hero.GetComponent<CharacterIdle>().enabled = false;
            var appearance = hero.AddComponent<CharacterAppearance>(); appearance.Configure(hero.GetComponentsInChildren<Renderer>().Where(r => r.name.StartsWith("Hoodie")).ToArray());
            var dance = hero.GetComponent<CharacterDance>(); dance.Configure(hero.GetComponent<CharacterLife>());
            stage = new GameObject("Stage"); anchor = new GameObject("PetAnchor"); cameraObject = new GameObject("PreviewCamera");
            var camera = cameraObject.AddComponent<Camera>(); camera.enabled = false;
            var target = new GameObject("Image", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage)).GetComponent<RawImage>(); target.transform.SetParent(root.transform, false);
            target.rectTransform.sizeDelta = new Vector2(256, 256);
            preview = root.AddComponent<CosmeticPreview>(); preview.Configure(catalog, appearance, dance, hero.transform, camera, target, stage.transform, anchor.transform);
            profile = new CosmeticProfile { Balance = 600, OwnedIds = new[] { "stripes", "pet-kitten" } };
            profile.EquippedIds[0] = "stripes"; profile.EquippedIds[7] = "pet-kitten"; preview.Restore(profile);
        }
        /// <summary>Удаляет объекты и их личные ресурсы после каждого теста.</summary>
        /// <returns>Ожидание отложенного удаления Unity.</returns>
        [UnityTearDown] public IEnumerator RemovePreview()
        { Object.Destroy(root); Object.Destroy(hero); Object.Destroy(stage); Object.Destroy(anchor); Object.Destroy(cameraObject); yield return null; }
        /// <summary>Примерка одежды, предмета и танца не меняет данные пользователя.</summary>
        [Test] public void PreviewNeverChangesProfileOrHoodiePreference()
        {
            var before = JsonUtility.ToJson(profile); var had = PlayerPrefs.HasKey(CosmeticProfileStore.ColorKey); var color = PlayerPrefs.GetInt(CosmeticProfileStore.ColorKey);
            foreach (var id in new[] { "space", "pistol-ice", "robot" }) { catalog.TryGet(id, out var item); preview.Show(item); }
            preview.Close(profile);
            Assert.That(JsonUtility.ToJson(profile), Is.EqualTo(before)); Assert.That(PlayerPrefs.HasKey(CosmeticProfileStore.ColorKey), Is.EqualTo(had)); Assert.That(PlayerPrefs.GetInt(CosmeticProfileStore.ColorKey), Is.EqualTo(color));
            Assert.That(hero.GetComponent<CharacterAppearance>().AppliedId, Is.EqualTo("stripes"));
        }
        /// <summary>Новая карточка заменяет прежний предмет, а выход возвращает сохранённого питомца.</summary>
        /// <returns>Ожидание удаления предыдущих объектов.</returns>
        [UnityTest] public IEnumerator SwitchAndCloseRestoreClothingPetAndClearOldObjects()
        {
            var position = hero.transform.position; var rotation = hero.transform.rotation;
            foreach (var id in new[] { "pet-puppy", "pistol-ice", "robot", "trail-stars" })
            { catalog.TryGet(id, out var item); preview.Show(item); yield return null; Assert.That(stage.transform.childCount, Is.LessThanOrEqualTo(1)); Assert.That(anchor.transform.childCount, Is.EqualTo(1)); }
            preview.Close(profile); yield return null;
            Assert.That(stage.transform.childCount, Is.Zero); Assert.That(anchor.transform.GetChild(0).name, Does.StartWith("pet-kitten"));
            Assert.That(hero.transform.position, Is.EqualTo(position)); Assert.That(hero.transform.rotation, Is.EqualTo(rotation));
        }
        /// <summary>След меняет положение частиц и точно повторяет цикл 2,4 секунды.</summary>
        [Test] public void TrailMovesAndRepeatsAtTwoPointFourSeconds()
        {
            catalog.TryGet("trail-stars", out var item); var sample = Object.Instantiate(item.PreviewPrefab);
            try
            {
                var trail = sample.AddComponent<CosmeticTrailPreview>(); trail.Configure(item);
                var part = sample.transform.GetChild(0); var start = part.localPosition; var heroPosition = hero.transform.position;
                trail.Advance(.6f); Assert.That(Vector3.Distance(start, part.localPosition), Is.GreaterThan(.01f));
                trail.Advance(1.8f); Assert.That(Vector3.Distance(start, part.localPosition), Is.LessThan(.0001f));
                Assert.That(hero.transform.position, Is.EqualTo(heroPosition));
            }
            finally { Object.DestroyImmediate(sample); }
        }
        /// <summary>Просмотр переиспользует камеру и освобождает изображение при уничтожении.</summary>
        /// <returns>Ожидание освобождения ресурсов.</returns>
        [UnityTest] public IEnumerator CloseAndDestroyReleaseOwnedResources()
        {
            var camera = cameraObject.GetComponent<Camera>(); catalog.TryGet("pistol-ice", out var item);
            preview.Show(item); yield return null; var rt = camera.targetTexture; Assert.That(rt, Is.Not.Null);
            for (var i = 0; i < 10; i++) { preview.Close(profile); preview.Show(item); yield return null; Assert.That(camera.targetTexture, Is.SameAs(rt)); }
            Object.Destroy(preview); yield return null; Assert.That(camera.targetTexture, Is.Null); Assert.That(rt == null || !rt.IsCreated(), Is.True);
        }
    }
}
