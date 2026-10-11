using UnityEngine;
using UnityEngine.UI;

namespace TinyTrouble
{
    /// <summary>Расставляет категории и список, оставляя название и действие на месте при прокрутке.</summary>
    public sealed class CosmeticPanelLayout : MonoBehaviour
    {
        /// <summary>Две строки категорий, видимая область списка и его содержимое.</summary>
        [SerializeField] RectTransform categoryRoot, viewport, content;
        /// <summary>Закреплённое описание и кнопка действия.</summary>
        [SerializeField] RectTransform description, action;
        /// <summary>Последний размер и показ отдельной кнопки танца.</summary>
        Vector2 previous;
        /// <summary>Была ли показана отдельная кнопка танца.</summary>
        bool previousDance;
        /// <summary>Подключает готовые части панели.</summary>
        /// <param name="categoryRoot">Категории.</param>
        /// <param name="viewport">Видимая область списка.</param>
        /// <param name="content">Все карточки списка.</param>
        /// <param name="description">Описание выбранной вещи.</param>
        /// <param name="action">Покупка или применение.</param>
        public void Configure(RectTransform categoryRoot, RectTransform viewport, RectTransform content, RectTransform description, RectTransform action)
        { this.categoryRoot = categoryRoot; this.viewport = viewport; this.content = content; this.description = description; this.action = action; previous = Vector2.zero; }
        /// <summary>Переставляет элементы по фактическому размеру панели без изменения профиля.</summary>
        /// <param name="size">Ширина и высота панели.</param>
        public void Apply(Vector2 size)
        {
            if (!categoryRoot) return;
            previous = size; var width = size.x - 48; var height = size.y;
            var heading = transform.Find("CharacterTitle") ?? transform.Find("ShopTitle");
            Place((RectTransform)heading, 24, height - 54, width, 38);
            Place(categoryRoot, 24, height - 150, width, 88);
            var categories = categoryRoot.GetComponent<GridLayoutGroup>(); categories.cellSize = new Vector2((width - 24) / 4, 40); categories.spacing = new Vector2(8, 8);
            foreach (var text in categoryRoot.GetComponentsInChildren<Text>()) text.fontSize = Mathf.Clamp(Mathf.FloorToInt(categories.cellSize.x / 7), 14, 18);
            var listHeight = Mathf.Max(90, height - 352); Place((RectTransform)viewport.parent, 24, 190, width, listHeight);
            var grid = content.GetComponent<GridLayoutGroup>(); grid.constraintCount = width >= 660 ? 3 : 2;
            grid.cellSize = new Vector2((width - 16 - 12 * (grid.constraintCount - 1)) / grid.constraintCount, height < 560 ? 140 : 170);
            Place((RectTransform)transform.Find("DetailTitle"), 24, 146, width, 32);
            Place(description, 24, 102, width, 38);
            var dance = (RectTransform)transform.Find("DanceButton"); previousDance = dance.gameObject.activeSelf;
            var actionWidth = previousDance ? width * .64f - 6 : width;
            Place(action, 24, 48, actionWidth, 48); Place(dance, 24 + width * .64f + 6, 48, width * .36f - 6, 48);
            action.GetComponentInChildren<Text>().fontSize = height < 560 ? 17 : 20; dance.GetComponentInChildren<Text>().fontSize = 17;
            Place((RectTransform)transform.Find("OperationStatus"), 24, 8, width, 32);
            Place((RectTransform)transform.Find("EmptyMessage"), 40, 190 + (listHeight - 100) * .5f, size.x - 80, 100);
            var accent = transform.Find("Accent"); if (accent) Place((RectTransform)accent, 0, height - 4, size.x, 4);
            GetComponent<CosmeticPanel>().RebuildNavigation();
        }
        /// <summary>Перестраивает панель только при изменении размера или кнопки танца.</summary>
        void LateUpdate()
        {
            var size = ((RectTransform)transform).rect.size; var dance = transform.Find("DanceButton");
            if (size != previous || (dance && dance.gameObject.activeSelf != previousDance)) Apply(size);
        }
        /// <summary>Размещает прямоугольник от левого нижнего края панели.</summary>
        /// <param name="rect">Элемент.</param>
        /// <param name="x">Отступ слева.</param>
        /// <param name="y">Отступ снизу.</param>
        /// <param name="width">Ширина.</param>
        /// <param name="height">Высота.</param>
        static void Place(RectTransform rect, float x, float y, float width, float height)
        { rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero; rect.anchoredPosition = new Vector2(x, y); rect.sizeDelta = new Vector2(width, height); }
    }
}
