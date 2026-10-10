using UnityEngine;

namespace TinyTrouble
{
    /// <summary>Выбирает жесты героя и совмещает их с действиями игрока.</summary>
    public sealed class CharacterLife : MonoBehaviour
    {
        /// <summary>Самое короткое ожидание нового случайного жеста в секундах.</summary>
        const float MinimumIdleDelay = 5f;
        /// <summary>Самое долгое ожидание жеста. Измени это число: максимум должен быть больше 5.</summary>
        const float MaximumIdleDelay = 10f;
        /// <summary>Компоненты, которые изменяют тело и лицо; здесь находятся только таймеры.</summary>
        [SerializeField] CharacterGestures gestures;
        /// <summary>Управление глазами и головой.</summary>
        [SerializeField] CharacterFace face;
        /// <summary>Три случайных жеста, между которыми выбираем без повторения.</summary>
        static readonly CharacterGesture[] IdleGestures = { CharacterGesture.WeightShift, CharacterGesture.LookAround, CharacterGesture.Wave };
        /// <summary>Камера лобби для определения положения указателя.</summary>
        Camera lobbyCamera;
        /// <summary>Время жеста, его длительность, ожидание нового жеста и цикл дыхания.</summary>
        float elapsed, duration, idleWait, breathingPhase;
        /// <summary>Время плавного завершения прерванного жеста и зафиксированная его поза.</summary>
        float fadeElapsed, fadeProgress;
        /// <summary>Начиналось ли поведение, ожидается ли реакция на цвет, заканчивается ли прерванный жест.</summary>
        bool initialized, pendingColor, fading;
        /// <summary>Не повторяет предупреждение об отсутствии компонента каждый кадр.</summary>
        bool warned;
        /// <summary>Номер последнего случайного жеста, чтобы не выбирать его снова.</summary>
        int lastIdle = -1;

        /// <summary>Единственный крупный жест, который сейчас выполняется.</summary>
        public CharacterGesture CurrentGesture { get; private set; }
        /// <summary>Зажата ли левая кнопка на герое; вращение имеет высший приоритет.</summary>
        public bool IsDragging { get; private set; }

        /// <summary>Сохраняет ссылки на части поведения в шаблоне героя.</summary>
        /// <param name="gestures">Движения тела и конечностей.</param>
        /// <param name="face">Моргание и движения головы.</param>
        public void Configure(CharacterGestures gestures, CharacterFace face)
        { this.gestures = gestures; this.face = face; }

        /// <summary>Начинает поведение один раз после восстановления настроек меню.</summary>
        void Start() { Initialize(); }

        /// <summary>При первом запуске приветствует, если более важное действие ещё не началось.</summary>
        void Initialize()
        {
            if (initialized) return;
            initialized = true; idleWait = Random.Range(MinimumIdleDelay, MaximumIdleDelay);
            if ((!gestures || !face) && !warned)
            { Debug.LogWarning("CharacterLife: компонент движений отсутствует; оставшиеся движения продолжатся.", this); warned = true; }
            if (!IsDragging && !pendingColor) Begin(CharacterGesture.Greeting, 1.5f);
        }

        /// <summary>Получает изменение нажатия от CharacterDrag, включая потерю фокуса.</summary>
        /// <param name="dragging">true при нажатии, false после завершения вращения.</param>
        public void SetDragging(bool dragging)
        {
            if (IsDragging == dragging) return;
            IsDragging = dragging;
            if (dragging) Interrupt();
            else idleWait = Random.Range(MinimumIdleDelay, MaximumIdleDelay);
        }

        /// <summary>Оставляет только одну актуальную реакцию на новый цвет одежды.</summary>
        public void NotifyColorChanged()
        {
            pendingColor = true;
            Interrupt();
        }

        /// <summary>Замораживает позу крупного жеста и начинает плавный возврат за 0,2 секунды.</summary>
        void Interrupt()
        {
            if (CurrentGesture == CharacterGesture.None || fading) return;
            fading = true; fadeElapsed = 0; fadeProgress = Mathf.Clamp01(elapsed / duration);
        }

        /// <summary>Начинает выбранный жест без очередей отдельных анимаций.</summary>
        /// <param name="gesture">Новый жест.</param>
        /// <param name="seconds">Его продолжительность в секундах.</param>
        void Begin(CharacterGesture gesture, float seconds)
        { CurrentGesture = gesture; duration = seconds; elapsed = 0; fading = false; }

        /// <summary>Заканчивает жест и назначает новую паузу после его завершения.</summary>
        void Finish()
        {
            CurrentGesture = CharacterGesture.None; elapsed = 0; fading = false;
            idleWait = Random.Range(MinimumIdleDelay, MaximumIdleDelay);
        }

        /// <summary>Считает таймеры и передаёт одну согласованную позу телу и лицу.</summary>
        /// <param name="deltaTime">Прошедшее время без замедления игры.</param>
        /// <param name="pointerViewport">Мышь в координатах изображения от 0 до 1.</param>
        /// <param name="hasFocus">Получает ли игра ввод пользователя.</param>
        /// <param name="camera">Камера, передаваемая лицу для слежения.</param>
        public void Advance(float deltaTime, Vector2 pointerViewport, bool hasFocus, Camera camera)
        {
            Initialize(); deltaTime = Mathf.Max(0, deltaTime);
            breathingPhase = Mathf.Repeat(breathingPhase + deltaTime * Mathf.PI * 2 / 3, Mathf.PI * 2);
            var finishedThisStep = false;
            if (fading)
            {
                fadeElapsed += deltaTime;
                if (fadeElapsed >= .2f) { Finish(); finishedThisStep = true; }
            }
            else if (CurrentGesture != CharacterGesture.None)
            {
                elapsed += deltaTime;
                if (elapsed >= duration) { Finish(); finishedThisStep = true; }
            }
            if (CurrentGesture == CharacterGesture.None && !IsDragging)
            {
                if (pendingColor) { pendingColor = false; Begin(CharacterGesture.ColorReaction, 1.6f); }
                else if (!finishedThisStep)
                {
                    idleWait -= deltaTime;
                    if (idleWait <= 0)
                    {
                        lastIdle = lastIdle < 0 ? Random.Range(0, 3) : (lastIdle + 1 + Random.Range(0, 2)) % 3;
                        Begin(IdleGestures[lastIdle], Random.Range(1f, 2f));
                    }
                }
            }
            var progress = fading ? fadeProgress : CurrentGesture == CharacterGesture.None ? 0 : Mathf.Clamp01(elapsed / duration);
            var weight = fading ? 1 - Mathf.SmoothStep(0, 1, fadeElapsed / .2f) : 1;
            if (gestures) gestures.ApplyPose(CurrentGesture, progress, weight, breathingPhase);
            if (face) face.Animate(deltaTime, CurrentGesture, progress, weight, !IsDragging, pointerViewport, hasFocus, camera);
        }

        /// <summary>Каждый кадр использует реальное время и положение мыши в изображении игры.</summary>
        void Update()
        {
            if (!lobbyCamera) lobbyCamera = Camera.main;
            var pointer = new Vector2(Input.mousePosition.x / Mathf.Max(1, Screen.width), Input.mousePosition.y / Mathf.Max(1, Screen.height));
            Advance(Time.unscaledDeltaTime, pointer, Application.isFocused, lobbyCamera);
        }

        /// <summary>Очищает состояния и позы; повторное включение не повторяет приветствие.</summary>
        void OnDisable()
        {
            IsDragging = false; pendingColor = false; fading = false; CurrentGesture = CharacterGesture.None;
            elapsed = 0; breathingPhase = 0; idleWait = Random.Range(MinimumIdleDelay, MaximumIdleDelay);
            if (gestures) gestures.ResetPose();
            if (face) face.ResetPose();
        }
    }
}
