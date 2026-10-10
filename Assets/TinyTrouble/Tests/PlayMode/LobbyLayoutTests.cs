#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TinyTrouble.Tests
{
    /// <summary>Проверяет настоящее лобби после изменения размера окна игры.</summary>
    public class LobbyLayoutTests
    {
        /// <summary>Окно Game и его исходный размер, который вернём после проверки.</summary>
        EditorWindow gameView;
        /// <summary>Свойство выбора размера окна Game.</summary>
        PropertyInfo selectedSize;
        /// <summary>Группа размеров и номер временного варианта.</summary>
        object sizeGroup;
        /// <summary>Исходный размер и количество встроенных вариантов Unity.</summary>
        int oldIndex, builtInCount;
        /// <summary>Число добавленных размеров для удаления после проверки.</summary>
        int addedCount;

        /// <summary>Сохраняет размер окна и открывает сохранённую сцену лобби.</summary>
        /// <returns>Ожидание загрузки сцены.</returns>
        [UnitySetUp] public IEnumerator Setup()
        {
            var assembly = typeof(Editor).Assembly;
            var gameViewType = assembly.GetType("UnityEditor.GameView");
            gameView = EditorWindow.GetWindow(gameViewType);
            selectedSize = gameViewType.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            oldIndex = (int)selectedSize.GetValue(gameView);
            var sizesType = assembly.GetType("UnityEditor.GameViewSizes");
            var sizes = typeof(ScriptableSingleton<>).MakeGenericType(sizesType).GetProperty("instance").GetValue(null);
            var groupType = sizesType.GetProperty("currentGroupType").GetValue(sizes);
            sizeGroup = sizesType.GetMethod("GetGroup").Invoke(sizes, new[] { groupType });
            builtInCount = (int)sizeGroup.GetType().GetMethod("GetBuiltinCount").Invoke(sizeGroup, null);
            yield return SceneManager.LoadSceneAsync("Lobby");
            yield return null;
        }

        /// <summary>Возвращает размер окна и удаляет временные варианты из списка Unity.</summary>
        [TearDown] public void Cleanup()
        {
            if (!gameView || sizeGroup == null) return;
            selectedSize.SetValue(gameView, oldIndex);
            var group = sizeGroup.GetType();
            for (var i = 0; i < addedCount; i++)
            {
                var count = (int)group.GetMethod("GetCustomCount").Invoke(sizeGroup, null);
                group.GetMethod("RemoveCustomSize").Invoke(sizeGroup, new object[] { builtInCount + count - 1 });
            }
            gameView.Repaint();
        }

        /// <summary>Меняет реальное разрешение Game View, а не передаёт размер прямо в проверяемый код.</summary>
        /// <param name="width">Ширина изображения игры.</param>
        /// <param name="height">Высота изображения игры.</param>
        void SetSize(int width, int height)
        {
            var assembly = typeof(Editor).Assembly;
            var type = assembly.GetType("UnityEditor.GameViewSize");
            var kind = Enum.Parse(assembly.GetType("UnityEditor.GameViewSizeType"), "FixedResolution");
            var size = Activator.CreateInstance(type, new[] { kind, (object)width, height, "Проверка лобби" });
            var group = sizeGroup.GetType();
            group.GetMethod("AddCustomSize").Invoke(sizeGroup, new[] { size });
            addedCount++;
            var count = (int)group.GetMethod("GetCustomCount").Invoke(sizeGroup, null);
            selectedSize.SetValue(gameView, builtInCount + count - 1);
            gameView.Repaint();
        }

        /// <summary>Находит объект интерфейса, включая скрытые вкладки.</summary>
        /// <param name="name">Имя объекта в сцене.</param>
        /// <returns>Прямоугольник объекта.</returns>
        static RectTransform Find(string name) => SceneManager.GetActiveScene().GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<RectTransform>(true)).Single(rect => rect.name == name);

        /// <summary>Получает область интерфейсного элемента в пикселях экрана.</summary>
        /// <param name="rect">Элемент интерфейса.</param>
        /// <returns>Границы элемента на экране.</returns>
        static Rect Bounds(RectTransform rect)
        {
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }

        /// <summary>Проверяет, что элемент полностью помещается в выбранной области.</summary>
        /// <param name="inner">Границы элемента.</param>
        /// <param name="outer">Доступная область.</param>
        /// <param name="label">Название для сообщения об ошибке.</param>
        static void Inside(Rect inner, Rect outer, string label)
        {
            Assert.That(inner.xMin, Is.GreaterThanOrEqualTo(outer.xMin - 1), label + " слева");
            Assert.That(inner.yMin, Is.GreaterThanOrEqualTo(outer.yMin - 1), label + " снизу");
            Assert.That(inner.xMax, Is.LessThanOrEqualTo(outer.xMax + 1), label + " справа");
            Assert.That(inner.yMax, Is.LessThanOrEqualTo(outer.yMax + 1), label + " сверху");
        }

        /// <summary>Проверяет все вкладки на широком, квадратном и вертикальном экране, затем повторяет широкий.</summary>
        /// <returns>Кадры ожидания обновления интерфейса и камеры.</returns>
        [UnityTest] public IEnumerator ResizingKeepsControlsAndHeroSeparated()
        {
            var sizes = new[] { new Vector2Int(720, 1280), new Vector2Int(1920, 1080), new Vector2Int(1024, 1024),
                new Vector2Int(390, 844), new Vector2Int(844, 390), new Vector2Int(1024, 768), new Vector2Int(1050, 1000),
                new Vector2Int(1051, 1000), new Vector2Int(1100, 1000),
                new Vector2Int(1440, 1000), new Vector2Int(1450, 1000), new Vector2Int(2560, 1080), new Vector2Int(1280, 720) };
            foreach (var size in sizes)
            {
                SetSize(size.x, size.y);
                for (var frame = 0; frame < 8; frame++) yield return null;
                Assert.That(Screen.width, Is.EqualTo(size.x), "Unity не применила ширину окна");
                Assert.That(Screen.height, Is.EqualTo(size.y), "Unity не применила высоту окна");
                Canvas.ForceUpdateCanvases();
                var screen = new Rect(0, 0, size.x, size.y);
                var headerNames = new[] { "Brand", "BrandCaption", "TabPlay", "TabCharacter", "TabSettings", "LobbyTag" };
                var header = headerNames.Select(Find).Where(rect => rect.gameObject.activeInHierarchy).ToArray();
                foreach (var rect in header) Inside(Bounds(rect), screen, size + " " + rect.name);
                for (var a = 0; a < header.Length; a++)
                    for (var b = a + 1; b < header.Length; b++)
                        Assert.That(Bounds(header[a]).Overlaps(Bounds(header[b])), Is.False, size + " " + header[a].name + " перекрывает " + header[b].name);
                foreach (var text in Find("Header").GetComponentsInChildren<Text>())
                    Assert.That(text.preferredHeight, Is.LessThanOrEqualTo(text.rectTransform.rect.height + 1), size + " обрезан текст " + text.name);
                foreach (var tab in new[] { "TabPlay", "TabCharacter", "TabSettings" })
                {
                    Find(tab).GetComponent<Button>().onClick.Invoke(); yield return null;
                    Canvas.ForceUpdateCanvases();
                    var panel = new[] { "PlayPanel", "CharacterPanel", "SettingsPanel" }.Select(Find).Single(rect => rect.gameObject.activeInHierarchy);
                    var blocks = new[] { "Header", "Intro", "PlayerTag", "Footer", "KeyboardHint", "RotationHint", panel.name }
                        .Select(Find).Where(rect => rect.gameObject.activeInHierarchy).ToArray();
                    foreach (var block in blocks) Inside(Bounds(block), screen, size + " " + block.name);
                    for (var a = 0; a < blocks.Length; a++)
                        for (var b = a + 1; b < blocks.Length; b++)
                            Assert.That(Bounds(blocks[a]).Overlaps(Bounds(blocks[b])), Is.False, size + " " + blocks[a].name + " перекрывает " + blocks[b].name);
                    Inside(Bounds(panel), screen, size + " " + panel.name);
                    var content = panel.GetComponentsInChildren<RectTransform>().Where(rect =>
                        (rect.parent == panel && rect.name != "Accent") || rect.name == "SelectionFrame").ToArray();
                    foreach (var rect in content) Inside(Bounds(rect), Bounds(panel), size + " " + rect.name);
                    for (var a = 0; a < content.Length; a++)
                        for (var b = a + 1; b < content.Length; b++)
                            Assert.That(Bounds(content[a]).Overlaps(Bounds(content[b])), Is.False, size + " " + content[a].name + " перекрывает " + content[b].name);
                    foreach (var text in panel.GetComponentsInChildren<Text>())
                        Assert.That(text.preferredHeight, Is.LessThanOrEqualTo(text.rectTransform.rect.height + 1), size + " обрезан текст " + text.name);
                    var hero = UnityEngine.Object.FindObjectOfType<CharacterIdle>();
                    var life = hero.GetComponent<CharacterLife>();
                    life.enabled = false;
                    var gestures = hero.GetComponent<CharacterGestures>();
                    var face = hero.GetComponent<CharacterFace>();
                    var cameraPosition = Camera.main.transform.position;
                    var cameraRotation = Camera.main.transform.rotation;
                    for (var angle = 0; angle < 4; angle++)
                    {
                        foreach (CharacterGesture gesture in Enum.GetValues(typeof(CharacterGesture)))
                        foreach (var progress in new[] { 0f, .25f, .375f, .5f, .75f, .9f, 1f })
                        {
                            gestures.ApplyPose(gesture, progress, 1, Mathf.PI * .5f);
                            face.Animate(.1f, gesture, progress, 1, true, Vector2.one, true, Camera.main);
                            var projected = hero.GetComponentsInChildren<Renderer>().SelectMany(renderer =>
                            {
                                var bounds = renderer.bounds;
                                return Enumerable.Range(0, 8).Select(index => Camera.main.WorldToScreenPoint(bounds.center + Vector3.Scale(bounds.extents,
                                    new Vector3((index & 1) == 0 ? -1 : 1, (index & 2) == 0 ? -1 : 1, (index & 4) == 0 ? -1 : 1))));
                            }).ToArray();
                            var heroBounds = Rect.MinMaxRect(projected.Min(p => p.x), projected.Min(p => p.y), projected.Max(p => p.x), projected.Max(p => p.y));
                            Inside(heroBounds, screen, size + " герой");
                            foreach (var obstacle in blocks)
                                Assert.That(heroBounds.Overlaps(Bounds(obstacle)), Is.False, size + " герой перекрывает " + obstacle.name + " при повороте " + angle * 90);
                            Assert.That(Camera.main.transform.position, Is.EqualTo(cameraPosition));
                            Assert.That(Camera.main.transform.rotation, Is.EqualTo(cameraRotation));
                        }
                        hero.Turn(90); yield return null;
                    }
                    life.enabled = true;
                    yield return null;
                    Assert.That(Bounds(panel).Overlaps(Bounds(Find("Header"))), Is.False, size + " панель перекрывает шапку");
                }
            }
        }

        /// <summary>Перестройка камеры в середине жеста использует ту же область, что и исходная поза.</summary>
        /// <returns>Ожидание применения размера и позднего обновления камеры.</returns>
        [UnityTest] public IEnumerator ResizingAtDifferentPosesUsesSameCamera()
        {
            SetSize(720, 1280);
            for (var i = 0; i < 8; i++) yield return null;
            var hero = UnityEngine.Object.FindObjectOfType<CharacterIdle>(); hero.enabled = false;
            hero.GetComponent<CharacterLife>().enabled = false;
            var gestures = hero.GetComponent<CharacterGestures>();
            var layout = UnityEngine.Object.FindObjectOfType<LobbyLayout>();
            gestures.ResetPose();
            layout.enabled = false; layout.enabled = true;
            yield return null;
            var before = Camera.main.transform.position;
            gestures.ApplyPose(CharacterGesture.Wave, .5f, 1, Mathf.PI * .5f);
            layout.enabled = false; layout.enabled = true;
            yield return null;
            Assert.That(Vector3.Distance(before, Camera.main.transform.position), Is.LessThan(.0001f), "Камера зависит от текущей позы героя");
        }
    }
}
#endif
