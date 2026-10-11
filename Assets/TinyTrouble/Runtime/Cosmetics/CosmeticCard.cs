using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TinyTrouble
{
    /// <summary>Карточка показывает цену, владение и активную вещь отдельно от временного выделения.</summary>
    public sealed class CosmeticCard : MonoBehaviour, ISelectHandler
    {
        /// <summary>Изображение товара, имя и состояние покупки.</summary>
        [SerializeField] Image icon;
        /// <summary>Название карточки.</summary>
        [SerializeField] Text title;
        /// <summary>Цена, покупка или активный выбор.</summary>
        [SerializeField] Text state;
        /// <summary>Полоска временного выделения и отметка надетой вещи.</summary>
        [SerializeField] GameObject highlight, selectionFrame;
        /// <summary>Действие выбора без изменения профиля.</summary>
        Action<string> selected;
        /// <summary>Постоянный код показываемого товара.</summary>
        public string ItemId { get; private set; }
        /// <summary>Подключает данные и выбор карточки.</summary>
        /// <param name="item">Показываемая вещь.</param>
        /// <param name="service">Общий профиль.</param>
        /// <param name="selected">Действие при нажатии карточки.</param>
        public void Configure(CosmeticItem item, CosmeticService service, Action<string> selected)
        {
            ItemId = item.Id; this.selected = selected; icon.sprite = item.Icon; title.text = item.Name;
            var equipped = service.Snapshot.EquippedIds[(int)item.Slot] == item.Id;
            state.text = equipped ? "✓ ВЫБРАНО" : service.Owns(item.Id) ? item.Price == 0 ? "БЕСПЛАТНО" : "КУПЛЕНО" : item.Price + " Ш";
            state.color = equipped ? new Color(.3f, .9f, 1) : Color.white;
            selectionFrame.SetActive(equipped);
        }
        /// <summary>Нажатие или Enter открывает просмотр, не покупая вещь.</summary>
        public void Pick() { selected?.Invoke(ItemId); }
        /// <summary>Отмечает просматриваемую карточку независимо от надетой вещи.</summary>
        /// <param name="value">Просматривается ли эта карточка.</param>
        public void SetHighlighted(bool value) { highlight.SetActive(value); }
        /// <summary>Прокручивает список так, чтобы выбранная клавиатурой карточка была видна.</summary>
        /// <param name="eventData">Событие выбора элемента интерфейса.</param>
        public void OnSelect(BaseEventData eventData)
        {
            var scroll = GetComponentInParent<ScrollRect>(); if (!scroll) return;
            Canvas.ForceUpdateCanvases();
            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport, transform);
            var area = scroll.viewport.rect;
            if (bounds.min.y < area.yMin) scroll.content.anchoredPosition += Vector2.up * (area.yMin - bounds.min.y);
            else if (bounds.max.y > area.yMax) scroll.content.anchoredPosition -= Vector2.up * (bounds.max.y - area.yMax);
        }
    }
}
