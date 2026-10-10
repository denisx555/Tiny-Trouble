using UnityEngine;
using UnityEngine.EventSystems;

namespace TinyTrouble
{
    /// <summary>
    /// Поворачивает героя левой кнопкой мыши и приближает его колёсиком над моделью.
    /// </summary>
    /// <remarks>
    /// EventSystem передаёт сюда нажатие на область героя через PhysicsRaycaster камеры.
    /// Нажатия на интерфейс обрабатываются его кнопками и ползунком.
    /// </remarks>
    [RequireComponent(typeof(CharacterIdle))]
    public sealed class CharacterDrag : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,
        IInitializePotentialDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
    {
        /// <summary>Скорость приближения за шаг колёсика: большее число приближает быстрее.</summary>
        const float ZoomStep = .16f;
        /// <summary>
        /// Угол поворота при движении мыши на ширину окна; одинаков для разных разрешений.
        /// </summary>
        [SerializeField, Min(1f)] float degreesPerScreenWidth = 360f;
        /// <summary>
        /// Компонент, который объединяет ручной поворот и покачивание героя.
        /// </summary>
        CharacterIdle idle;
        /// <summary>Раскладка лобби, которая одновременно управляет положением камеры.</summary>
        LobbyLayout layout;
        /// <summary>
        /// Началось ли текущее нажатие левой кнопки именно на герое.
        /// </summary>
        bool isDragging;

        /// <summary>
        /// Находит покачивание того же героя при создании компонента.
        /// </summary>
        void Awake() { idle = GetComponent<CharacterIdle>(); }

        /// <summary>Передаёт колёсико камере только при попадании указателя в героя.</summary>
        /// <param name="eventData">Прокрутка мыши: положительное значение приближает, отрицательное отдаляет.</param>
        public void OnScroll(PointerEventData eventData)
        {
            if (eventData.pointerId != -1 || eventData.scrollDelta.y == 0) return;
            if (!layout) layout = FindObjectOfType<LobbyLayout>();
            if (layout) layout.ChangeZoom(eventData.scrollDelta.y * ZoomStep);
        }

        /// <summary>
        /// Начинает жест только для левой кнопки мыши.
        /// </summary>
        /// <param name="eventData">Данные кнопки и указателя от Unity.</param>
        public void OnPointerDown(PointerEventData eventData)
        {
            // В старой системе ввода Unity номер -1 обозначает левую кнопку мыши.
            if (eventData.button == PointerEventData.InputButton.Left && eventData.pointerId == -1)
                SetDragging(true);
        }

        /// <summary>
        /// Позволяет вращать героя с первого движения, без минимального расстояния жеста.
        /// </summary>
        /// <param name="eventData">Настройки начинающегося перетаскивания.</param>
        public void OnInitializePotentialDrag(PointerEventData eventData) { eventData.useDragThreshold = false; }

        /// <summary>
        /// Превращает горизонтальное смещение мыши в поворот вокруг вертикальной оси.
        /// </summary>
        /// <param name="eventData">Смещение мыши в пикселях с прошлого кадра.</param>
        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragging || eventData.button != PointerEventData.InputButton.Left || eventData.pointerId != -1) return;
            // Делим на ширину окна, чтобы скорость не зависела от разрешения; вертикальное движение игнорируем.
            idle.Turn(-eventData.delta.x / Mathf.Max(1, Screen.width) * degreesPerScreenWidth);
        }

        /// <summary>
        /// Прекращает жест после отпускания левой кнопки, сохраняя выбранный угол.
        /// </summary>
        /// <param name="eventData">Данные отпущенной кнопки мыши.</param>
        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && eventData.pointerId == -1)
                SetDragging(false);
        }

        /// <summary>
        /// Завершает перетаскивание, в том числе при отпускании мыши за пределами героя.
        /// </summary>
        /// <param name="eventData">Данные завершённого жеста.</param>
        public void OnEndDrag(PointerEventData eventData) { OnPointerUp(eventData); }

        /// <summary>
        /// Прекращает жест, если окно потеряло фокус и отпускание мыши могло быть пропущено.
        /// </summary>
        /// <param name="hasFocus">Получает ли окно игры ввод от пользователя.</param>
        void OnApplicationFocus(bool hasFocus) { if (!hasFocus) SetDragging(false); }

        /// <summary>
        /// Прекращает жест при приостановке приложения.
        /// </summary>
        /// <param name="paused">Приостановлено ли приложение.</param>
        void OnApplicationPause(bool paused) { if (paused) SetDragging(false); }

        /// <summary>
        /// Убирает незавершённое нажатие при отключении компонента или закрытии сцены.
        /// </summary>
        void OnDisable() { SetDragging(false); }

        /// <summary>Передаёт Life только настоящее изменение нажатия, без повторного сброса таймера.</summary>
        /// <param name="value">Новое состояние левой кнопки на герое.</param>
        void SetDragging(bool value)
        {
            if (isDragging == value) return;
            isDragging = value;
            var life = GetComponent<CharacterLife>();
            if (life) life.SetDragging(value);
        }
    }
}
