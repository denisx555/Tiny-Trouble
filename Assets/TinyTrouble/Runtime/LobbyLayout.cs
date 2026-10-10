using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TinyTrouble
{
    /// <summary>Размещает меню и героя в свободных областях горизонтального или вертикального окна.</summary>
    /// <remarks>Размеры здесь заданы в единицах Canvas — поверхности интерфейса, которую Unity масштабирует под экран.</remarks>
    [RequireComponent(typeof(CanvasScaler))]
    public sealed class LobbyLayout : MonoBehaviour
    {
        /// <summary>Подсказка вращения для узкого окна. Её можно сократить, изменив текст между кавычками.</summary>
        const string CompactRotationHint = "ЛКМ, чтобы повернуть";
        /// <summary>Камера лобби, которую отодвигаем при уменьшении свободного места.</summary>
        [SerializeField] Camera lobbyCamera;
        /// <summary>Герой, вокруг которого оставляем место для вращения.</summary>
        [SerializeField] Transform hero;
        /// <summary>Найденные элементы меню: поиск по имени выполняется один раз.</summary>
        readonly Dictionary<string, RectTransform> elements = new Dictionary<string, RectTransform>();
        /// <summary>Масштабирование поверхности интерфейса.</summary>
        CanvasScaler scaler;
        /// <summary>Размер окна при последней перестановке элементов.</summary>
        Vector2Int previousSize;

        /// <summary>Сохраняет ссылки при создании сцены, не заменяя настройки других компонентов.</summary>
        /// <param name="camera">Камера, которая показывает героя.</param>
        /// <param name="character">Корень персонажа.</param>
        public void Configure(Camera camera, Transform character)
        {
            lobbyCamera = camera;
            hero = character;
            previousSize = Vector2Int.zero;
        }

        /// <summary>При включении компонента готовит ссылки на существующее меню.</summary>
        void OnEnable()
        {
            scaler = GetComponent<CanvasScaler>();
            elements.Clear();
            foreach (var rect in GetComponentsInChildren<RectTransform>(true))
                if (rect != transform && !elements.ContainsKey(rect.name)) elements.Add(rect.name, rect);
            previousSize = Vector2Int.zero;
        }

        /// <summary>Перестраивает меню только после изменения ширины или высоты окна.</summary>
        void LateUpdate()
        {
            if (!lobbyCamera || !hero || !elements.ContainsKey("Header")) return;
            var size = new Vector2Int(Screen.width, Screen.height);
            if (size == previousSize || size.x <= 0 || size.y <= 0) return;
            previousSize = size;
            ApplyLayout(size.x, size.y);
        }

        /// <summary>Выбирает широкую или компактную раскладку и выделяет свободное место для героя.</summary>
        /// <param name="pixelWidth">Ширина окна игры в пикселях.</param>
        /// <param name="pixelHeight">Высота окна игры в пикселях.</param>
        void ApplyLayout(int pixelWidth, int pixelHeight)
        {
            var screenAspect = (float)pixelWidth / pixelHeight;
            var compact = screenAspect < 1.45f;
            var stacked = screenAspect <= 1.05f;
            // Возле квадратного формата уменьшаем долю экрана, занятую меню: герою остаётся больше места.
            var stackedWidth = Mathf.Lerp(720, 1080, Mathf.InverseLerp(.6f, 1.05f, screenAspect));
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.referenceResolution = stacked ? new Vector2(stackedWidth, 1280) : new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = stacked ? 0 : 1;
            // При панели снизу считаем высоту из ширины; при панели справа считаем ширину из высоты.
            var width = stacked ? stackedWidth : 1080f * screenAspect;
            var height = stacked ? stackedWidth / screenAspect : 1080f;
            var headerHeight = compact ? 146f : 122f;
            Place("Header", 0, height - headerHeight, width, headerHeight);
            LayoutHeader(width, compact);
            Show("Intro", !compact); Show("Footer", !compact); Show("KeyboardHint", !compact);
            var panelWidth = stacked ? width - 48 : 425;
            var panelHeight = stacked ? Mathf.Clamp(height * .32f, 240, 300) : 350;
            var panelX = stacked ? 24 : width - panelWidth - 48;
            var panelY = stacked ? 24 : 96;
            foreach (var name in new[] { "PlayPanel", "CharacterPanel", "SettingsPanel" })
            {
                Place(name, panelX, panelY, panelWidth, panelHeight);
                var accent = elements[name].Find("Accent") as RectTransform;
                Place(accent, 0, panelHeight - 4, panelWidth, 4);
            }
            LayoutPanels(panelWidth, panelHeight, stacked);
            var tagY = height - headerHeight - 78;
            var heroCenterX = !stacked && compact ? (48 + width - 500) / 2 : width / 2;
            Place("PlayerTag", heroCenterX - 123, tagY, 246, 62);
            var hintY = stacked ? panelY + panelHeight + 8 : 24;
            TextBox("RotationHint", stacked ? 24 : heroCenterX - 225, hintY, stacked ? width - 48 : 450, 28, 16, TextAnchor.MiddleCenter);
            elements["RotationHint"].GetComponent<Text>().text = compact ? CompactRotationHint : "Зажми и потяни, чтобы повернуть";
            Place("Intro", 48, 450, 420, 320);
            // Описание тоже должно помещаться внутри своей области, без наложения строк.
            TextBox("IntroCopy", 0, 0, 410, 60, 18);
            Place("Footer", 48, 24, 380, 28);
            Place("KeyboardHint", width - 428, 24, 380, 28);
            var heroArea = stacked
                ? Rect.MinMaxRect(48, hintY + 48, width - 48, tagY - 18)
                : Rect.MinMaxRect(compact ? 48 : 500, 88, width - 500, tagY - 18);
            FitCamera(heroArea, width, height);
        }

        /// <summary>Отделяет название от вкладок на узком экране.</summary>
        /// <param name="width">Ширина поверхности интерфейса.</param>
        /// <param name="compact">Нужно ли расположить вкладки отдельной строкой.</param>
        void LayoutHeader(float width, bool compact)
        {
            TextBox("Brand", compact ? 24 : 48, compact ? 100 : 64, 350, compact ? 34 : 44, compact ? 28 : 31);
            TextBox("BrandCaption", compact ? 24 : 48, compact ? 76 : 35, 350, 22, 12);
            Show("LobbyTag", !compact);
            Place("LobbyTag", width - 198, 42, 150, 38);
            var tabWidth = compact ? (width - 64) / 3 : 204;
            var start = compact ? 24 : width / 2 - 322;
            var index = 0;
            foreach (var name in new[] { "TabPlay", "TabCharacter", "TabSettings" })
            {
                Place(name, start + index * (tabWidth + (compact ? 8 : 16)), compact ? 16 : 34, tabWidth, compact ? 48 : 54);
                elements[name].GetComponentInChildren<Text>().fontSize = compact ? 18 : 20;
                index++;
            }
        }

        /// <summary>Расставляет содержимое всех вкладок с одинаковыми отступами от края панели.</summary>
        /// <param name="width">Ширина панели.</param>
        /// <param name="height">Высота панели.</param>
        /// <param name="compact">Использовать ли компактный вариант.</param>
        void LayoutPanels(float width, float height, bool compact)
        {
            var contentWidth = width - 48;
            Show("PlayEyebrow", !compact);
            TextBox("PlayEyebrow", 24, height - 44, contentWidth, 24, 13);
            TextBox("PlayTitle", 24, height - (compact ? 62 : 98), contentWidth, 44, compact ? 28 : 32);
            TextBox("PlayStatus", 24, height - (compact ? 99 : 138), contentWidth, 30, 17);
            Place("PlayButton", 24, compact ? 38 : 42, contentWidth, compact ? 74 : 94);
            elements["PlayButton"].GetComponentInChildren<Text>().fontSize = compact ? 32 : 36;
            TextBox("PlayHint", 24, 8, contentWidth, 22, 11, TextAnchor.MiddleCenter);
            foreach (var name in new[] { "CharacterTitle", "SettingsTitle" })
                TextBox(name, 24, height - 72, contentWidth, 48, compact ? 28 : 32);
            TextBox("CharacterCopy", 24, height - 115, contentWidth, 32, 18);
            for (var i = 0; i < 4; i++)
            {
                Place("Color" + i, width / 2 - 167 + i * 88, compact ? 54 : 122, 70, compact ? 56 : 74);
                // Полоска выбора находится под кнопкой, но выше поясняющего текста.
                Place(elements["Color" + i].Find("SelectionFrame") as RectTransform, 0, -10, 70, 4);
            }
            TextBox("StyleHint", 24, 10, contentWidth, 30, 14, TextAnchor.MiddleCenter);
            TextBox("VolumeLabel", 24, height - 119, width - 150, 30, 15);
            TextBox("VolumePercent", width - 109, height - 119, 85, 30, 20, TextAnchor.MiddleRight);
            Place("VolumeSlider", 24, compact ? 65 : 106, contentWidth, compact ? 40 : 50);
            foreach (var name in new[] { "Track", "FillArea" }) Place(name, 0, compact ? 15 : 20, contentWidth, 10);
            Place("HandleArea", 10, 0, contentWidth - 20, compact ? 40 : 50);
            TextBox("VolumeHint", 24, 8, contentWidth, 48, 14, TextAnchor.MiddleCenter);
        }

        /// <summary>Подбирает расстояние по постоянной области всех поз, поэтому жесты не качают камеру.</summary>
        /// <param name="area">Свободная область между текстом, шапкой и панелью.</param>
        /// <param name="width">Ширина интерфейса.</param>
        /// <param name="height">Высота интерфейса.</param>
        void FitCamera(Rect area, float width, float height)
        {
            var bounds = new Bounds(hero.position, Vector3.zero);
            var gestures = hero.GetComponent<CharacterGestures>();
            if (gestures && gestures.PoseBounds.size.sqrMagnitude > .01f)
            {
                // Поворот покрывается радиусом ниже; текущая поза и угол героя не меняют область.
                var local = gestures.PoseBounds;
                var scale = hero.lossyScale;
                scale = new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
                bounds = new Bounds(hero.position + Vector3.Scale(local.center, scale), Vector3.Scale(local.size, scale));
            }
            else foreach (var renderer in hero.GetComponentsInChildren<Renderer>()) bounds.Encapsulate(renderer.bounds);
            // Запас по ширине учитывает вращение, по высоте — лёгкое покачивание персонажа.
            var reachX = Mathf.Max(Mathf.Abs(bounds.min.x - hero.position.x), Mathf.Abs(bounds.max.x - hero.position.x));
            var reachZ = Mathf.Max(Mathf.Abs(bounds.min.z - hero.position.z), Mathf.Abs(bounds.max.z - hero.position.z));
            var radius = Mathf.Sqrt(reachX * reachX + reachZ * reachZ) + .08f;
            var halfHeight = bounds.extents.y + .12f;
            var tangent = Mathf.Tan(lobbyCamera.fieldOfView * Mathf.Deg2Rad * .5f);
            var aspect = width / height;
            var distance = Mathf.Max(halfHeight / (area.height / height * tangent),
                radius / (area.width / width * tangent * aspect)) + radius + .25f;
            var rotation = Quaternion.Euler(6, 0, 0);
            var center = new Vector3(hero.position.x, bounds.center.y, hero.position.z);
            var offsetX = (area.center.x / width * 2 - 1) * distance * tangent * aspect;
            var offsetY = (area.center.y / height * 2 - 1) * distance * tangent;
            lobbyCamera.transform.SetPositionAndRotation(center - rotation * new Vector3(offsetX, offsetY, distance), rotation);
        }

        /// <summary>Размещает именованный элемент относительно левого нижнего края родителя.</summary>
        /// <param name="name">Имя элемента в сцене.</param>
        /// <param name="x">Отступ слева.</param>
        /// <param name="y">Отступ снизу.</param>
        /// <param name="width">Ширина элемента.</param>
        /// <param name="height">Высота элемента.</param>
        void Place(string name, float x, float y, float width, float height) => Place(elements[name], x, y, width, height);

        /// <summary>Задаёт положение и размеры без зависимости от старых точек привязки.</summary>
        /// <param name="rect">Прямоугольник элемента.</param>
        /// <param name="x">Отступ слева.</param>
        /// <param name="y">Отступ снизу.</param>
        /// <param name="width">Ширина элемента.</param>
        /// <param name="height">Высота элемента.</param>
        static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        }

        /// <summary>Размещает текст и подбирает читаемый размер букв.</summary>
        /// <param name="name">Имя надписи.</param>
        /// <param name="x">Отступ слева.</param>
        /// <param name="y">Отступ снизу.</param>
        /// <param name="width">Ширина текста.</param>
        /// <param name="height">Высота текста.</param>
        /// <param name="fontSize">Размер букв в единицах интерфейса.</param>
        /// <param name="alignment">Выравнивание текста внутри его области.</param>
        void TextBox(string name, float x, float y, float width, float height, int fontSize, TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            Place(name, x, y, width, height);
            var text = elements[name].GetComponent<Text>();
            text.fontSize = fontSize;
            text.alignment = alignment;
        }

        /// <summary>Скрывает необязательные пояснения, оставляя доступными все вкладки и настройки.</summary>
        /// <param name="name">Имя элемента.</param>
        /// <param name="visible">Показывать ли его.</param>
        void Show(string name, bool visible) => elements[name].gameObject.SetActive(visible);
    }
}
