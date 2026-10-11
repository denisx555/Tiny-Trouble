using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TinyTrouble.EditorTools
{
    /// <summary>Добавляет магазин и инвентарь к существующему интерфейсу лобби.</summary>
    public static class CosmeticUIBuilder
    {
        /// <summary>Заголовок личных вещей героя; текст можно изменить внутри кавычек.</summary>
        public const string InventoryTitle = "ИНВЕНТАРЬ";
        /// <summary>Добавляет магазин к существующей сцене без пересоздания героя и ручных объектов.</summary>
        /// <param name="scene">Существующая сцена лобби.</param>
        /// <param name="catalog">Сохранённый каталог вещей.</param>
        public static void Upgrade(Scene scene, CosmeticCatalog catalog)
        {
            var canvas = scene.GetRootGameObjects().Single(r => r.name == "LobbyUI");
            var hero = scene.GetRootGameObjects().Single(r => r.name == "LobbyCharacter");
            Attach(canvas, hero, catalog); EditorSceneManager.MarkSceneDirty(scene);
        }
        /// <summary>Подключает общие части магазина к готовой поверхности интерфейса.</summary>
        /// <param name="canvas">Готовый корень интерфейса.</param>
        /// <param name="hero">Готовый герой, который не пересоздаётся.</param>
        /// <param name="catalog">Каталог вещей.</param>
        public static void Attach(GameObject canvas, GameObject hero, CosmeticCatalog catalog)
        {
            var controller = canvas.GetComponent<LobbyController>();
            var appearance = hero.GetComponent<CharacterAppearance>(); if (!appearance) appearance = hero.AddComponent<CharacterAppearance>();
            var preview = canvas.GetComponent<CosmeticPreview>(); if (!preview) preview = canvas.AddComponent<CosmeticPreview>();
            var inventory = Panel(canvas.transform.Find("CharacterPanel").gameObject, InventoryTitle, true);
            var shop = Panel(Rect("ShopPanel", canvas.transform).gameObject, "МАГАЗИН", false);
            var header = canvas.transform.Find("Header"); var tabs = new Button[4]; var panels = new GameObject[4];
            var tabNames = new[] { "TabPlay", "TabCharacter", "TabShop", "TabSettings" }; var tabLabels = new[] { "ИГРАТЬ", "ПЕРСОНАЖ", "МАГАЗИН", "НАСТРОЙКИ" };
            var panelNames = new[] { "PlayPanel", "CharacterPanel", "ShopPanel", "SettingsPanel" };
            for (var i = 0; i < 4; i++)
            {
                tabs[i] = Button(tabNames[i], header, tabLabels[i]); Clear(tabs[i]); UnityEventTools.AddIntPersistentListener(tabs[i].onClick, controller.SelectTab, i);
                panels[i] = canvas.transform.Find(panelNames[i]).gameObject;
            }
            for (var i = 0; i < 4; i++) tabs[i].navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = tabs[(i + 3) % 4], selectOnRight = tabs[(i + 1) % 4] };
            var balance = Label("BalanceBadge", header, "1000  ШАЛКОИНЫ", 16); balance.alignment = TextAnchor.MiddleRight;
            Place(balance.rectTransform, 1530, 42, 330, 36);
            var coin = Rect("CurrencyCoin", header); var coinImage = Background(coin, new Color(1f, .78f, .16f));
            coinImage.sprite = CurrencySprite(); Place(coin, 1490, 44, 32, 32);
            var symbol = Label("CurrencySymbol", coin, "Ш", 21); symbol.color = new Color(.22f, .12f, .01f); symbol.alignment = TextAnchor.MiddleCenter;
            Stretch(symbol.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var stageObject = GameObject.Find("CosmeticPreviewRoot"); if (!stageObject) stageObject = new GameObject("CosmeticPreviewRoot"); stageObject.transform.position = new Vector3(1000, 1000, 1000);
            var pet = GameObject.Find("PetAnchor"); if (!pet) pet = new GameObject("PetAnchor"); pet.transform.position = hero.transform.position + new Vector3(1, 0, -.05f);
            var cameraObject = Rect("CosmeticPreviewCamera", canvas.transform).gameObject;
            var camera = cameraObject.GetComponent<Camera>(); if (!camera) camera = cameraObject.AddComponent<Camera>(); camera.enabled = false; camera.cullingMask = 1 << CosmeticPreview.PreviewLayer;
            var image = Rect("CosmeticObjectImage", canvas.transform).gameObject.GetComponent<RawImage>(); if (!image) image = canvas.transform.Find("CosmeticObjectImage").gameObject.AddComponent<RawImage>();
            image.enabled = false; image.raycastTarget = false; Place(image.rectTransform, 180, 230, 650, 630); image.transform.SetAsFirstSibling();
            var path = CosmeticAssetBuilder.Root + "/Preview.renderTexture"; var texture = AssetDatabase.LoadAssetAtPath<RenderTexture>(path);
            if (!texture) { texture = new RenderTexture(512, 512, 24); AssetDatabase.CreateAsset(texture, path); }
            camera.targetTexture = texture; image.texture = texture;
            var lightObject = stageObject.transform.Find("CosmeticPreviewLight");
            if (!lightObject) { lightObject = new GameObject("CosmeticPreviewLight", typeof(Light)).transform; lightObject.SetParent(stageObject.transform, false); }
            var light = lightObject.GetComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.2f; light.cullingMask = 1 << CosmeticPreview.PreviewLayer; light.transform.rotation = Quaternion.Euler(30, -25, 0);
            var main = Camera.main; if (main) main.cullingMask &= ~(1 << CosmeticPreview.PreviewLayer);
            var tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]); tags.FindProperty("layers").GetArrayElementAtIndex(CosmeticPreview.PreviewLayer).stringValue = "CosmeticPreview"; tags.ApplyModifiedPropertiesWithoutUndo();
            var previewData = new SerializedObject(preview);
            Set(previewData, "catalog", catalog); Set(previewData, "appearance", appearance); Set(previewData, "dance", hero.GetComponent<CharacterDance>()); Set(previewData, "hero", hero.transform);
            Set(previewData, "previewCamera", camera); Set(previewData, "target", image); Set(previewData, "previewRoot", stageObject.transform); Set(previewData, "petAnchor", pet.transform); previewData.ApplyModifiedPropertiesWithoutUndo();
            var data = new SerializedObject(controller); Array(data, "tabs", tabs); Array(data, "panels", panels); Set(data, "catalog", catalog); Set(data, "appearance", appearance); Set(data, "preview", preview);
            Set(data, "shopPanel", shop); Set(data, "inventoryPanel", inventory); Set(data, "balanceText", balance);
            Array(data, "colorButtons", inventory.GetComponentsInChildren<Button>(true).Where(b => b.name.StartsWith("Color")).OrderBy(b => b.name).ToArray()); data.ApplyModifiedPropertiesWithoutUndo();
            foreach (var panel in panels) panel.SetActive(panel.name == "PlayPanel"); AssetDatabase.SaveAssets();
        }
        /// <summary>Сохраняет собственный круглый значок монеты с прозрачными углами.</summary>
        /// <returns>Изображение золотой монеты для шапки лобби.</returns>
        static Sprite CurrencySprite()
        {
            var path = CosmeticAssetBuilder.Root + "/Icons/currency.png";
            if (!System.IO.File.Exists(path))
            {
                var texture = new Texture2D(64, 64);
                for (var y = 0; y < 64; y++) for (var x = 0; x < 64; x++)
                {
                    var distance = Vector2.Distance(new Vector2(x, y), new Vector2(31.5f, 31.5f));
                    texture.SetPixel(x, y, distance > 31 ? Color.clear : distance > 27 ? new Color(.8f, .57f, .08f) : Color.white);
                }
                texture.Apply(); System.IO.File.WriteAllBytes(path, texture.EncodeToPNG()); Object.DestroyImmediate(texture); AssetDatabase.ImportAsset(path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path); importer.textureType = TextureImporterType.Sprite; importer.alphaIsTransparency = true; importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        /// <summary>Готовит панель с категориями, прокручиваемым списком и закреплённым действием.</summary>
        /// <param name="root">Корень готовой панели.</param>
        /// <param name="title">Заголовок режима.</param>
        /// <param name="inventory">Нужны ли четыре бесплатные карточки.</param>
        /// <returns>Подключённый компонент панели.</returns>
        static CosmeticPanel Panel(GameObject root, string title, bool inventory)
        {
            var panel = root.GetComponent<CosmeticPanel>(); if (!panel) panel = root.AddComponent<CosmeticPanel>();
            var rt = (RectTransform)root.transform; Place(rt, 1040, 120, 820, 810); Background(rt, new Color(.027f, .063f, .14f, .97f));
            foreach (var old in new[] { "CharacterCopy", "StyleHint" }) { var found = rt.Find(old); if (found) Object.DestroyImmediate(found.gameObject); }
            var heading = Label(inventory ? "CharacterTitle" : "ShopTitle", rt, title, 30); Place(heading.rectTransform, 24, 750, 772, 42);
            var categoryRoot = Rect("Categories", rt); Place(categoryRoot, 24, 628, 772, 100);
            var grid = categoryRoot.GetComponent<GridLayoutGroup>(); if (!grid) grid = categoryRoot.gameObject.AddComponent<GridLayoutGroup>(); grid.cellSize = new Vector2(181, 44); grid.spacing = new Vector2(12, 8); grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 4;
            var categories = new Button[8]; var names = new[] { "Одежда", "Оружие", "Танцы", "Спреи", "Парашюты", "Питомцы", "Следы", "Кирки" };
            for (var i = 0; i < categories.Length; i++) { categories[i] = Button("Category" + i, categoryRoot, names[i]); Clear(categories[i]); UnityEventTools.AddIntPersistentListener(categories[i].onClick, panel.SelectCategoryIndex, i); categories[i].GetComponentInChildren<Text>().fontSize = 16; }
            var scrollRoot = Rect("CatalogScroll", rt); Place(scrollRoot, 24, 202, 772, 404); Background(scrollRoot, new Color(.05f, .09f, .18f)).raycastTarget = true;
            var scroll = scrollRoot.GetComponent<ScrollRect>(); if (!scroll) scroll = scrollRoot.gameObject.AddComponent<ScrollRect>();
            var viewport = Rect("Viewport", scrollRoot); Stretch(viewport, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero); if (!viewport.GetComponent<RectMask2D>()) viewport.gameObject.AddComponent<RectMask2D>();
            var content = Rect("Cards", viewport); content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one; content.pivot = new Vector2(.5f, 1); content.anchoredPosition = Vector2.zero; content.sizeDelta = Vector2.zero;
            var cardsGrid = content.GetComponent<GridLayoutGroup>(); if (!cardsGrid) cardsGrid = content.gameObject.AddComponent<GridLayoutGroup>(); cardsGrid.cellSize = new Vector2(180, 176); cardsGrid.spacing = new Vector2(12, 12); cardsGrid.padding = new RectOffset(8, 8, 8, 8); cardsGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; cardsGrid.constraintCount = 4;
            var fitter = content.GetComponent<ContentSizeFitter>(); if (!fitter) fitter = content.gameObject.AddComponent<ContentSizeFitter>(); fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport; scroll.content = content; scroll.horizontal = false; scroll.vertical = true; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 30;
            var prefab = CardPrefab();
            if (inventory)
                for (var i = 0; i < 4; i++)
                {
                    var old = rt.Find("Color" + i); if (old) { old.SetParent(content, false); PrepareCard(old.gameObject); }
                    else if (!content.Find("Color" + i)) { var card = (GameObject)PrefabUtility.InstantiatePrefab(prefab.gameObject, content); card.name = "Color" + i; }
                }
            var empty = Label("EmptyMessage", rt, "Здесь пока нет вещей.\nЗагляни в магазин и выбери свой стиль!", 20); Place(empty.rectTransform, 40, 320, 740, 110); empty.alignment = TextAnchor.MiddleCenter;
            var detail = Label("DetailTitle", rt, "Выбери вещь", 23); Place(detail.rectTransform, 24, 154, 772, 36);
            var description = Label("Description", rt, "", 16); Place(description.rectTransform, 24, 108, 772, 42);
            var action = Button("ActionButton", rt, "Выбери вещь"); Place((RectTransform)action.transform, 24, 52, 530, 50); action.image.color = new Color(1, .85f, .29f); action.GetComponentInChildren<Text>().color = new Color(.04f, .09f, .18f); Clear(action); UnityEventTools.AddPersistentListener(action.onClick, panel.PerformAction);
            var dance = Button("DanceButton", rt, "Танцевать"); Place((RectTransform)dance.transform, 570, 52, 226, 50); Clear(dance); UnityEventTools.AddPersistentListener(dance.onClick, panel.PlayDance);
            var status = Label("OperationStatus", rt, "", 14); Place(status.rectTransform, 24, 12, 772, 34); status.color = new Color(.4f, .86f, .96f);
            var data = new SerializedObject(panel); Set(data, "cardsRoot", content); Set(data, "cardPrefab", prefab); Array(data, "categoryButtons", categories); Set(data, "actionButton", action); Set(data, "danceButton", dance);
            Set(data, "detailTitle", detail); Set(data, "description", description); Set(data, "operationStatus", status); Set(data, "emptyMessage", empty); data.ApplyModifiedPropertiesWithoutUndo();
            var layout = root.GetComponent<CosmeticPanelLayout>(); if (!layout) layout = root.AddComponent<CosmeticPanelLayout>(); layout.Configure(categoryRoot, viewport, content, description.rectTransform, (RectTransform)action.transform);
            return panel;
        }
        /// <summary>Сохраняет общий шаблон карточки с русским шрифтом и обработчиком кнопки.</summary>
        /// <returns>Сохранённая карточка для создания при запуске.</returns>
        static CosmeticCard CardPrefab()
        {
            const string path = "Assets/TinyTrouble/Cosmetics/Card.prefab";
            var saved = AssetDatabase.LoadAssetAtPath<GameObject>(path); if (saved) return saved.GetComponent<CosmeticCard>();
            var root = new GameObject("CosmeticCard", typeof(RectTransform));
            try { PrepareCard(root); return PrefabUtility.SaveAsPrefabAsset(root, path).GetComponent<CosmeticCard>(); }
            finally { Object.DestroyImmediate(root); }
        }
        /// <summary>Подключает картинку, подписи и две разные отметки к карточке.</summary>
        /// <param name="root">Новая или прежняя бесплатная карточка.</param>
        static void PrepareCard(GameObject root)
        {
            var rt = (RectTransform)root.transform; rt.sizeDelta = new Vector2(180, 176); Background(rt, new Color(.12f, .19f, .32f));
            var button = root.GetComponent<Button>(); if (!button) button = root.AddComponent<Button>(); button.targetGraphic = root.GetComponent<Image>(); root.GetComponent<Image>().raycastTarget = true; button.navigation = new Navigation { mode = Navigation.Mode.Automatic };
            var card = root.GetComponent<CosmeticCard>(); if (!card) card = root.AddComponent<CosmeticCard>(); Clear(button); UnityEventTools.AddPersistentListener(button.onClick, card.Pick);
            var icon = Rect("ItemIcon", rt); Stretch(icon, new Vector2(0, .32f), Vector2.one, new Vector2(7, 2), new Vector2(-7, -7)); var picture = Background(icon, Color.white); picture.preserveAspect = true;
            var name = Label("CardName", rt, "Вещь", 16); Stretch(name.rectTransform, new Vector2(0, .15f), new Vector2(1, .32f), new Vector2(8, 0), new Vector2(-8, 0));
            var state = Label("CardState", rt, "", 12); Stretch(state.rectTransform, Vector2.zero, new Vector2(1, .15f), new Vector2(8, 0), new Vector2(-8, 0));
            var highlight = Rect("Highlight", rt); Stretch(highlight, new Vector2(0, 1), Vector2.one, new Vector2(0, -4), Vector2.zero); Background(highlight, new Color(1, .85f, .29f)); highlight.gameObject.SetActive(false);
            var selected = Rect("SelectionFrame", rt); Stretch(selected, Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(0, 3)); Background(selected, Color.cyan); selected.gameObject.SetActive(false);
            var data = new SerializedObject(card); Set(data, "icon", picture); Set(data, "title", name); Set(data, "state", state); Set(data, "highlight", highlight.gameObject); Set(data, "selectionFrame", selected.gameObject); data.ApplyModifiedPropertiesWithoutUndo();
        }
        /// <summary>Находит именованный прямоугольник или создаёт его один раз.</summary>
        /// <param name="name">Имя нашего элемента.</param>
        /// <param name="parent">Родитель элемента.</param>
        /// <returns>Найденный или новый прямоугольник.</returns>
        static RectTransform Rect(string name, Transform parent)
        { var existing = parent.Find(name); if (existing) return (RectTransform)existing; var value = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); value.SetParent(parent, false); return value; }
        /// <summary>Создаёт фон, оставляя декоративные области прозрачными для ввода.</summary>
        /// <param name="rect">Область фона.</param>
        /// <param name="color">Цвет.</param>
        /// <returns>Компонент изображения.</returns>
        static Image Background(RectTransform rect, Color color)
        { var image = rect.GetComponent<Image>(); if (!image) image = rect.gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = false; return image; }
        /// <summary>Создаёт читаемую надпись с сохранённым русским шрифтом.</summary>
        /// <param name="name">Имя надписи.</param>
        /// <param name="parent">Родитель надписи.</param>
        /// <param name="value">Текст.</param>
        /// <param name="size">Размер букв.</param>
        /// <returns>Компонент текста.</returns>
        static Text Label(string name, Transform parent, string value, int size)
        { var rect = Rect(name, parent); var text = rect.GetComponent<Text>(); if (!text) text = rect.gameObject.AddComponent<Text>(); text.text = value; text.font = AssetDatabase.LoadAssetAtPath<Font>("Assets/TinyTrouble/UI/Fonts/DejaVuSans.ttf"); text.fontSize = size; text.color = Color.white; text.raycastTarget = false; text.verticalOverflow = VerticalWrapMode.Truncate; return text; }
        /// <summary>Создаёт кнопку и растягивает её подпись по всей области.</summary>
        /// <param name="name">Имя кнопки.</param>
        /// <param name="parent">Родитель кнопки.</param>
        /// <param name="label">Текст кнопки.</param>
        /// <returns>Кнопка.</returns>
        static Button Button(string name, Transform parent, string label)
        { var rect = Rect(name, parent); var image = Background(rect, new Color(.13f, .2f, .36f)); image.raycastTarget = true; var button = rect.GetComponent<Button>(); if (!button) button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image; var text = rect.GetComponentsInChildren<Text>(true).FirstOrDefault(t => t.transform.parent == rect); if (!text) text = Label("Label", rect, label, 20);
            foreach (var extra in rect.GetComponentsInChildren<Text>(true).Where(t => t.transform.parent == rect && t != text)) Object.DestroyImmediate(extra.gameObject);
            text.text = label; text.alignment = TextAnchor.MiddleCenter; Stretch(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(5, 2), new Vector2(-5, -2)); return button; }
        /// <summary>Очищает старые сохранённые обработчики перед повторным точечным обновлением.</summary>
        /// <param name="button">Наша кнопка, которой назначается один обработчик.</param>
        static void Clear(Button button) { while (button.onClick.GetPersistentEventCount() > 0) UnityEventTools.RemovePersistentListener(button.onClick, 0); }
        /// <summary>Записывает ссылку в сохраняемое поле компонента.</summary>
        /// <param name="data">Сохраняемые поля компонента.</param>
        /// <param name="name">Имя поля.</param>
        /// <param name="value">Связанный объект.</param>
        static void Set(SerializedObject data, string name, Object value) { data.FindProperty(name).objectReferenceValue = value; }
        /// <summary>Записывает массив ссылок в компонент.</summary>
        /// <param name="data">Сохраняемые поля.</param>
        /// <param name="name">Имя массива.</param>
        /// <param name="values">Ссылки элементов.</param>
        static void Array(SerializedObject data, string name, Object[] values)
        { var array = data.FindProperty(name); array.arraySize = values.Length; for (var i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i]; }
        /// <summary>Размещает элемент в координатах от левого нижнего края родителя.</summary>
        /// <param name="rect">Прямоугольник элемента.</param>
        /// <param name="x">Отступ слева.</param>
        /// <param name="y">Отступ снизу.</param>
        /// <param name="width">Ширина.</param>
        /// <param name="height">Высота.</param>
        static void Place(RectTransform rect, float x, float y, float width, float height)
        { rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero; rect.anchoredPosition = new Vector2(x, y); rect.sizeDelta = new Vector2(width, height); }
        /// <summary>Растягивает элемент между указанными точками родителя.</summary>
        /// <param name="rect">Прямоугольник элемента.</param>
        /// <param name="min">Нижняя левая точка привязки.</param>
        /// <param name="max">Верхняя правая точка привязки.</param>
        /// <param name="from">Отступ снизу и слева.</param>
        /// <param name="to">Отступ сверху и справа.</param>
        static void Stretch(RectTransform rect, Vector2 min, Vector2 max, Vector2 from, Vector2 to)
        { rect.anchorMin = min; rect.anchorMax = max; rect.pivot = Vector2.one * .5f; rect.offsetMin = from; rect.offsetMax = to; }
    }
}
