using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TinyTrouble.Tests
{
    /// <summary>Проверяет настоящие размеры Game View, прокрутку мышью и клавиатуру магазина.</summary>
    public class CosmeticNavigationTests : CosmeticLobbyTestScope
    {
        /// <summary>Сохранённый размер окна до проверки.</summary>
        GameViewSizeScope sizes;
        /// <summary>Открывает сцену и запоминает редакторские размеры.</summary>
        /// <returns>Ожидание загрузки.</returns>
        [UnitySetUp] public IEnumerator OpenLobby() { sizes = new GameViewSizeScope(); yield return SceneManager.LoadSceneAsync("Lobby"); yield return null; }
        /// <summary>Возвращает прежний размер редактора.</summary>
        [TearDown] public void RestoreSize() { sizes?.Dispose(); sizes = null; }
        /// <summary>Находит именованный прямоугольник внутри заданного родителя.</summary>
        /// <param name="parent">Область поиска.</param>
        /// <param name="name">Имя элемента.</param>
        /// <returns>Найденный прямоугольник.</returns>
        static RectTransform Find(Transform parent, string name) => parent.GetComponentsInChildren<RectTransform>(true).Single(t => t.name == name);
        /// <summary>Получает область элемента в пикселях экрана.</summary>
        /// <param name="rect">Элемент.</param>
        /// <returns>Область.</returns>
        static Rect Bounds(RectTransform rect) { var corners = new Vector3[4]; rect.GetWorldCorners(corners); return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y); }
        /// <summary>Проверяет полное попадание элемента внутрь области.</summary>
        /// <param name="inner">Элемент.</param>
        /// <param name="outer">Вмещающая область.</param>
        /// <param name="name">Имя для объяснения провала.</param>
        static void Inside(Rect inner, Rect outer, string name)
        { Assert.That(inner.xMin, Is.GreaterThanOrEqualTo(outer.xMin - 1), name); Assert.That(inner.yMin, Is.GreaterThanOrEqualTo(outer.yMin - 1), name); Assert.That(inner.xMax, Is.LessThanOrEqualTo(outer.xMax + 1), name); Assert.That(inner.yMax, Is.LessThanOrEqualTo(outer.yMax + 1), name); }
        /// <summary>Все категории и закреплённые действия помещаются в 13 размерах окна.</summary>
        /// <returns>Ожидание изменения настоящего окна.</returns>
        [UnityTest] public IEnumerator ShopAndInventoryFitAllSupportedSizes()
        {
            var values = new[] { new Vector2Int(720, 1280), new Vector2Int(1920, 1080), new Vector2Int(1024, 1024), new Vector2Int(390, 844), new Vector2Int(844, 390), new Vector2Int(1024, 768), new Vector2Int(1050, 1000), new Vector2Int(1051, 1000), new Vector2Int(1100, 1000), new Vector2Int(1440, 1000), new Vector2Int(1450, 1000), new Vector2Int(2560, 1080), new Vector2Int(1280, 720) };
            var controller = Object.FindObjectOfType<LobbyController>();
            foreach (var size in values)
            {
                sizes.Set(size.x, size.y); for (var i = 0; i < 8; i++) yield return null;
                var screen = new Rect(0, 0, Screen.width, Screen.height);
                foreach (var tab in new[] { 1, 2 })
                {
                    controller.SelectTab(tab); yield return null; Canvas.ForceUpdateCanvases();
                    var panel = controller.GetComponentsInChildren<CosmeticPanel>().Single(); var root = (RectTransform)panel.transform;
                    Inside(Bounds(root), screen, size + " panel");
                    foreach (var button in panel.GetComponentsInChildren<Button>().Where(b => b.name.StartsWith("Category"))) Inside(Bounds((RectTransform)button.transform), Bounds(root), button.name);
                    var list = Bounds(Find(root, "CatalogScroll")); var action = Bounds(Find(root, "ActionButton")); var description = Bounds(Find(root, "Description"));
                    Assert.That(list.Overlaps(action), Is.False, size + " list/action"); Assert.That(list.Overlaps(description), Is.False, size + " list/description");
                    Assert.That(Bounds(Find(controller.transform, "CosmeticObjectImage")).Overlaps(Bounds(root)), Is.False, size + " preview/panel");
                    foreach (var text in panel.GetComponentsInChildren<Text>().Where(t => !t.GetComponentInParent<CosmeticCard>()))
                        Assert.That(text.preferredHeight, Is.LessThanOrEqualTo(text.rectTransform.rect.height + 1), size + " " + text.name);
                    foreach (var name in new[] { "TabPlay", "TabCharacter", "TabShop", "TabSettings", "BalanceBadge" }) Inside(Bounds(Find(controller.transform, name)), screen, size + " " + name);
                }
            }
        }
        /// <summary>Клавиатура прокручивает далёкую карточку и сохраняет доступный фокус после покупки.</summary>
        /// <returns>Ожидание раскладки и событий.</returns>
        [UnityTest] public IEnumerator KeyboardScrollAndRefreshKeepVisibleFocus()
        {
            sizes.Set(390, 844); for (var i = 0; i < 8; i++) yield return null;
            var controller = Object.FindObjectOfType<LobbyController>(); controller.SelectTab(2); var shop = controller.GetComponentsInChildren<CosmeticPanel>().Single(); shop.SelectCategory(CosmeticCategory.Weapon); yield return null;
            var cards = shop.GetComponentsInChildren<CosmeticCard>(); var last = cards.Last();
            for (var i = 0; i < 20 && EventSystem.current.currentSelectedGameObject != last.gameObject; i++)
            { controller.AdvanceFocus(false); yield return null; Assert.That(EventSystem.current.currentSelectedGameObject.GetComponent<Selectable>().IsInteractable(), Is.True); }
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(last.gameObject));
            Inside(Bounds((RectTransform)last.transform), Bounds(shop.GetComponentInChildren<ScrollRect>().viewport), "focused card");
            ExecuteEvents.Execute(last.gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler); yield return null;
            shop.PerformAction(); yield return null;
            Assert.That(controller.Cosmetics.Snapshot.OwnedIds, Does.Contain(last.ItemId));
            Assert.That(EventSystem.current.currentSelectedGameObject.activeInHierarchy, Is.True);
            controller.AdvanceFocus(true); yield return null; Assert.That(EventSystem.current.currentSelectedGameObject.GetComponent<Selectable>().IsInteractable(), Is.True);
            var category = shop.GetComponentsInChildren<Button>().Single(b => b.name == "Category0"); EventSystem.current.SetSelectedGameObject(category.gameObject);
            ExecuteEvents.Execute(category.gameObject, new AxisEventData(EventSystem.current) { moveDir = MoveDirection.Down, moveVector = Vector2.down }, ExecuteEvents.moveHandler); yield return null;
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.Not.EqualTo(category.gameObject));
            shop.SelectCategory(CosmeticCategory.Clothes); yield return null; Assert.That(EventSystem.current.currentSelectedGameObject.activeInHierarchy, Is.True);
        }
        /// <summary>Колесо над настоящим списком прокручивает карточки, сохраняя расстояние до героя.</summary>
        /// <returns>Ожидание прокрутки и камеры.</returns>
        [UnityTest] public IEnumerator WheelOnCatalogDoesNotZoomHero()
        {
            sizes.Set(390, 844); for (var i = 0; i < 8; i++) yield return null;
            var controller = Object.FindObjectOfType<LobbyController>(); controller.SelectTab(2); var shop = controller.GetComponentsInChildren<CosmeticPanel>().Single(); shop.SelectCategory(CosmeticCategory.Weapon); yield return null;
            var scroll = shop.GetComponentInChildren<ScrollRect>(); Canvas.ForceUpdateCanvases(); var before = Camera.main.transform.position; var position = scroll.content.anchoredPosition;
            var data = new PointerEventData(EventSystem.current) { pointerId = -1, position = Bounds(scroll.viewport).center, scrollDelta = Vector2.down * 3 };
            var hits = new System.Collections.Generic.List<RaycastResult>(); EventSystem.current.RaycastAll(data, hits); Assert.That(hits.Count, Is.GreaterThan(0));
            Assert.That(hits[0].gameObject.GetComponentInParent<ScrollRect>(), Is.SameAs(scroll)); ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, data, ExecuteEvents.scrollHandler); yield return null;
            Assert.That(Vector2.Distance(position, scroll.content.anchoredPosition), Is.GreaterThan(1)); Assert.That(Vector3.Distance(before, Camera.main.transform.position), Is.LessThan(.001f));
        }
        /// <summary>Изменение окна при танце, питомце и крупном плане сохраняет отдельные области.</summary>
        /// <returns>Ожидание кадров изменения размеров.</returns>
        [UnityTest] public IEnumerator ResizeDuringDancePetPreviewAndZoomKeepsAreasSeparate()
        {
            var controller = Object.FindObjectOfType<LobbyController>(); controller.SelectTab(2); var shop = controller.GetComponentsInChildren<CosmeticPanel>().Single();
            var layout = controller.GetComponent<LobbyLayout>(); layout.ChangeZoom(20);
            foreach (var id in new[] { "robot", "pet-kitten", "trail-rainbow" })
            {
                var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<CosmeticCatalog>("Assets/TinyTrouble/Cosmetics/Catalog.asset"); catalog.TryGet(id, out var item); shop.SelectCategory(item.Category); shop.SelectItem(id);
                foreach (var size in new[] { new Vector2Int(1024, 1024), new Vector2Int(390, 844), new Vector2Int(844, 390) })
                {
                    sizes.Set(size.x, size.y); for (var i = 0; i < 12; i++) yield return null;
                    Assert.That(Bounds(Find(controller.transform, "CosmeticObjectImage")).Overlaps(Bounds((RectTransform)shop.transform)), Is.False);
                    Inside(Bounds((RectTransform)shop.transform), new Rect(0, 0, Screen.width, Screen.height), id);
                }
            }
        }
    }
}
