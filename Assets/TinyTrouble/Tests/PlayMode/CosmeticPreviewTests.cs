using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.EventSystems;

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
        /// <summary>След движется относительно падающего героя, а сам герой остаётся центром показа.</summary>
        /// <returns>Ожидание создания образца.</returns>
        [UnityTest] public IEnumerator TrailMovesAroundStationaryMiniature()
        {
            catalog.TryGet("trail-stars", out var item); preview.Show(item); yield return null;
            var trail = stage.GetComponentInChildren<CosmeticTrailPreview>();
            trail.enabled = false;
            var miniature = trail.transform.Find("FallingHero"); var before = miniature.localPosition;
            trail.Advance(.6f);
            Assert.That(miniature.localPosition, Is.EqualTo(before));
        }
        /// <summary>Мышь вращает только образец; колесо приближает его, а новый выбор возвращает общий вид.</summary>
        /// <returns>Ожидание обновления камеры после жестов.</returns>
        [UnityTest] public IEnumerator PointerTurnsAndZoomsPreviewWithoutChangingHero()
        {
            catalog.TryGet("rifle-lime", out var item); preview.Show(item); yield return null;
            var target = root.GetComponentInChildren<RawImage>();
            var down = target.GetComponent<IPointerDownHandler>();
            var drag = target.GetComponent<IDragHandler>();
            var scroll = target.GetComponent<IScrollHandler>();
            Assert.That(down, Is.Not.Null, "Область предмета должна принимать нажатия");
            Assert.That(drag, Is.Not.Null); Assert.That(scroll, Is.Not.Null);
            var eventRoot = new GameObject("TestPreviewEvents", typeof(EventSystem));
            try
            {
                var input = new PointerEventData(eventRoot.GetComponent<EventSystem>()) { pointerId = -1, button = PointerEventData.InputButton.Left, delta = new Vector2(64, 20) };
                var before = cameraObject.transform.position; var heroBefore = hero.transform.rotation;
                drag.OnDrag(input); yield return null;
                Assert.That(cameraObject.transform.position, Is.EqualTo(before), "Без нажатия вращения нет");
                down.OnPointerDown(input);
                down.OnPointerDown(new PointerEventData(eventRoot.GetComponent<EventSystem>()) { pointerId = -2, button = PointerEventData.InputButton.Right });
                drag.OnDrag(input); yield return null;
                Assert.That(Vector3.Distance(cameraObject.transform.position, before), Is.GreaterThan(.1f));
                Assert.That(hero.transform.rotation, Is.EqualTo(heroBefore));
                target.GetComponent<IPointerUpHandler>().OnPointerUp(input);
                var released = cameraObject.transform.position;
                drag.OnDrag(input); yield return null;
                Assert.That(cameraObject.transform.position, Is.EqualTo(released), "После отпускания вращение прекращается");
                down.OnPointerDown(input);
                input.delta = new Vector2(0, 32); drag.OnDrag(input); yield return null;
                Assert.That(cameraObject.transform.position.y, Is.LessThan(released.y - .1f), "Мышь вверх поворачивает предмет вверх, камера обходит его снизу");
                target.SendMessage("OnApplicationFocus", false);
                var unfocused = cameraObject.transform.position;
                drag.OnDrag(input); yield return null;
                Assert.That(cameraObject.transform.position, Is.EqualTo(unfocused), "После потери фокуса нужно новое нажатие");
                var center = stage.GetComponentsInChildren<Renderer>().Select(r => r.bounds).Aggregate((a, b) => { a.Encapsulate(b); return a; }).center;
                var distance = Vector3.Distance(cameraObject.transform.position, center);
                input.scrollDelta = new Vector2(0, 1); scroll.OnScroll(input); yield return null;
                Assert.That(Vector3.Distance(cameraObject.transform.position, center), Is.LessThan(distance));
                for (var i = 0; i < 100; i++) scroll.OnScroll(input);
                yield return null;
                Assert.That(Vector3.Distance(cameraObject.transform.position, center), Is.GreaterThan(distance * .25f));
                preview.Show(item); yield return null;
                Assert.That(Vector3.Distance(cameraObject.transform.position, before), Is.LessThan(.01f), "Новый товар снова целиком в кадре");
                input.scrollDelta = new Vector2(0, -1);
                for (var i = 0; i < 100; i++) scroll.OnScroll(input);
                yield return null;
                Assert.That(Vector3.Distance(cameraObject.transform.position, before), Is.LessThan(.01f), "Отдаление ограничено общим видом");
            }
            finally { Object.Destroy(eventRoot); }
        }
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
        /// <summary>След исходит от рук и ног падающего героя, движется назад и плавно исчезает.</summary>
        /// <returns>Ожидание создания героя и потоков.</returns>
        [UnityTest] public IEnumerator FallingTrailStartsAtLimbsAndFadesDownstream()
        {
            catalog.TryGet("trail-rainbow", out var item); preview.Show(item); yield return null;
            var trail = stage.GetComponentInChildren<CosmeticTrailPreview>(); trail.enabled = false;
            var miniature = trail.transform.Find("FallingHero");
            Assert.That(Vector3.Dot(miniature.up, Vector3.up), Is.LessThan(.65f), "Тело наклонено в позе падения");
            var posedFeet = miniature.GetComponentsInChildren<Renderer>().Where(r => r.name == "LeftSneaker" || r.name == "RightSneaker").Select(r => miniature.InverseTransformPoint(r.bounds.center).x).ToArray();
            var originalFeet = hero.GetComponentsInChildren<Renderer>().Where(r => r.name == "LeftSneaker" || r.name == "RightSneaker").Select(r => hero.transform.InverseTransformPoint(r.bounds.center).x).ToArray();
            Assert.That(Mathf.Abs(posedFeet[0] - posedFeet[1]), Is.GreaterThan(Mathf.Abs(originalFeet[0] - originalFeet[1]) + .1f), "Ноги разведены, обувь не пересекается");
            foreach (var name in new[] { "LeftHand", "RightHand", "LeftSneaker", "RightSneaker" })
            {
                var limb = miniature.GetComponentsInChildren<Renderer>().First(r => r.name == name);
                var flow = trail.transform.Find("Flow-" + name);
                Assert.That(flow, Is.Not.Null, "Источник следа: " + name);
                Assert.That(Vector3.Distance(flow.position, limb.bounds.center), Is.LessThan(.08f));
                var line = flow.GetComponentInChildren<LineRenderer>();
                Assert.That(line, Is.Not.Null);
                var start = line.GetPosition(0); var end = line.GetPosition(line.positionCount - 1);
                Assert.That(start.magnitude, Is.LessThan(.05f), "Лента начинается у конечности");
                Assert.That(end.y - start.y, Is.GreaterThan(1));
                Assert.That(line.colorGradient.Evaluate(1).a, Is.LessThan(.01f));
                Assert.That(line.colorGradient.Evaluate(.15f).a, Is.GreaterThan(.5f));
                var middle = line.GetPosition(line.positionCount / 2); trail.Advance(.3f);
                Assert.That(Vector3.Distance(middle, line.GetPosition(line.positionCount / 2)), Is.GreaterThan(.01f));
            }
            Assert.That(hero.transform.localRotation, Is.EqualTo(Quaternion.identity), "Главный герой не принимает позу образца");
        }
        /// <summary>След меняет положение частиц и точно повторяет цикл 2,4 секунды.</summary>
        [Test] public void TrailMovesAndRepeatsAtTwoPointFourSeconds()
        {
            catalog.TryGet("trail-stars", out var item); var sample = Object.Instantiate(item.PreviewPrefab);
            try
            {
                var trail = sample.AddComponent<CosmeticTrailPreview>(); trail.Configure(item);
                var part = sample.GetComponentInChildren<MeshRenderer>().transform; var start = part.localPosition; var heroPosition = hero.transform.position;
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
