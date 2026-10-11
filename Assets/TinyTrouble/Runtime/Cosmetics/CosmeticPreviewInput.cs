using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TinyTrouble
{
    /// <summary>Передаёт вращение и колесо мыши только предмету под указателем.</summary>
    [RequireComponent(typeof(RawImage))]
    public sealed class CosmeticPreviewInput : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,
        IInitializePotentialDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
    {
        /// <summary>Просмотр, которому принадлежит изображение.</summary>
        CosmeticPreview preview;
        /// <summary>Размер области нужен для одинаковой скорости на разных экранах.</summary>
        RectTransform area;
        /// <summary>Нажата ли левая кнопка внутри области предмета.</summary>
        bool dragging;
        /// <summary>Подключает владельца и начинает новый просмотр без старого нажатия.</summary>
        /// <param name="owner">Компонент просмотра предмета.</param>
        public void Configure(CosmeticPreview owner) { preview = owner; area = (RectTransform)transform; Cancel(); }
        /// <summary>Принимает только левую кнопку мыши над видимым предметом.</summary>
        /// <param name="data">Нажатие, переданное интерфейсом Unity.</param>
        public void OnPointerDown(PointerEventData data)
        { if (data.pointerId == -1 && data.button == PointerEventData.InputButton.Left && GetComponent<RawImage>().enabled) dragging = true; }
        /// <summary>Начинает вращение с первого движения указателя.</summary>
        /// <param name="data">Настройки перетаскивания.</param>
        public void OnInitializePotentialDrag(PointerEventData data) { data.useDragThreshold = false; }
        /// <summary>Вращает камеру вокруг предмета, оставляя модель героя на месте.</summary>
        /// <param name="data">Смещение указателя в пикселях.</param>
        /// <remarks>Минус перед вертикальным смещением позволяет поворачивать предмет вслед за мышью.</remarks>
        public void OnDrag(PointerEventData data)
        {
            if (!dragging || !preview || data.pointerId != -1 || data.button != PointerEventData.InputButton.Left) return;
            var scale = GetComponent<RawImage>().canvas ? GetComponent<RawImage>().canvas.scaleFactor : 1;
            preview.Turn(new Vector2(data.delta.x / Mathf.Max(1, area.rect.width * scale), -data.delta.y / Mathf.Max(1, area.rect.height * scale)));
        }
        /// <summary>Заканчивает вращение при отпускании кнопки, включая отпускание вне картинки.</summary>
        /// <param name="data">Отпущенная кнопка.</param>
        public void OnPointerUp(PointerEventData data)
        { if (data.pointerId == -1 && data.button == PointerEventData.InputButton.Left) Cancel(); }
        /// <summary>Заканчивает перетаскивание после выхода указателя за границы.</summary>
        /// <param name="data">Событие окончания.</param>
        public void OnEndDrag(PointerEventData data) { OnPointerUp(data); }
        /// <summary>Приближает видимый предмет колёсиком над его картинкой.</summary>
        /// <param name="data">Направление и величина прокрутки.</param>
        public void OnScroll(PointerEventData data)
        { if (preview && data.pointerId == -1 && GetComponent<RawImage>().enabled) preview.Zoom(data.scrollDelta.y * .12f); }
        /// <summary>Сбрасывает нажатие при выборе другого предмета или закрытии просмотра.</summary>
        public void Cancel() { dragging = false; }
        /// <summary>Не оставляет зажатую кнопку после переключения в другое окно.</summary>
        /// <param name="focus">Активно ли окно игры.</param>
        void OnApplicationFocus(bool focus) { if (!focus) Cancel(); }
        /// <summary>Сбрасывает нажатие при паузе приложения.</summary>
        /// <param name="paused">Приостановлено ли приложение.</param>
        void OnApplicationPause(bool paused) { if (paused) Cancel(); }
        /// <summary>Завершает жест при отключении области просмотра.</summary>
        void OnDisable() { Cancel(); }
    }
}
