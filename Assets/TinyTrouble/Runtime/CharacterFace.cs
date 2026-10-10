using UnityEngine;

namespace TinyTrouble
{
    /// <summary>Управляет глазами и головой; остальные компоненты не меняют эти части.</summary>
    public sealed class CharacterFace : MonoBehaviour
    {
        /// <summary>Точка поворота головы у шеи.</summary>
        [SerializeField] Transform headPivot;
        /// <summary>Белки, зрачки и блики двух глаз в порядке левого и правого.</summary>
        [SerializeField] Transform[] eyeWhites, pupils, eyeShines;
        /// <summary>Детали глаз, расположенные тройками: белок, зрачок, блик.</summary>
        Transform[] eyes;
        /// <summary>Исходные положения и размеры деталей глаз.</summary>
        Vector3[] eyePositions, eyeScales;
        /// <summary>Показывает, у каких глаз есть все три детали.</summary>
        bool[] completeEyes;
        /// <summary>Исходный поворот головы.</summary>
        Quaternion headRotation;
        /// <summary>Плавное направление взгляда: ноль означает центр.</summary>
        Vector2 gaze;
        /// <summary>Оставшееся ожидание моргания и прошедшее время закрытия глаз.</summary>
        float blinkWait, blinkElapsed = -1;
        /// <summary>Не повторяет предупреждение о недостающих частях.</summary>
        bool warned;

        /// <summary>Сохраняет ссылки на голову и пары глаз.</summary>
        /// <param name="headPivot">Группа головы с волосами, ушами и лицом.</param>
        /// <param name="eyeWhites">Два белка глаз.</param>
        /// <param name="pupils">Два зрачка.</param>
        /// <param name="eyeShines">Два светлых блика.</param>
        public void Configure(Transform headPivot, Transform[] eyeWhites, Transform[] pupils, Transform[] eyeShines)
        {
            this.headPivot = headPivot; this.eyeWhites = eyeWhites; this.pupils = pupils; this.eyeShines = eyeShines;
            CapturePose(); ResetPose();
        }

        /// <summary>Запоминает исходные детали и пропускает неполные пары.</summary>
        void CapturePose()
        {
            eyes = new Transform[6]; eyePositions = new Vector3[6]; eyeScales = new Vector3[6]; completeEyes = new bool[2];
            if (headPivot) headRotation = headPivot.localRotation;
            for (var i = 0; i < 2; i++)
            {
                eyes[i * 3] = Element(eyeWhites, i); eyes[i * 3 + 1] = Element(pupils, i); eyes[i * 3 + 2] = Element(eyeShines, i);
                completeEyes[i] = eyes[i * 3] && eyes[i * 3 + 1] && eyes[i * 3 + 2];
            }
            if ((!headPivot || !completeEyes[0] || !completeEyes[1]) && !warned)
            { Debug.LogWarning("CharacterFace: часть лица отсутствует; её движение пропущено.", this); warned = true; }
            for (var i = 0; i < eyes.Length; i++)
                if (eyes[i]) { eyePositions[i] = eyes[i].localPosition; eyeScales[i] = eyes[i].localScale; }
            blinkWait = Random.Range(3f, 6f);
        }

        /// <summary>Безопасно получает деталь из необязательного массива.</summary>
        /// <param name="parts">Сохранённые ссылки.</param>
        /// <param name="index">Номер детали.</param>
        /// <returns>Деталь или null, если её нет.</returns>
        static Transform Element(Transform[] parts, int index) => parts != null && index < parts.Length ? parts[index] : null;

        /// <summary>Возвращает лицо в исходную позу и начинает новое ожидание моргания.</summary>
        public void ResetPose()
        {
            if (eyes == null) CapturePose();
            if (headPivot) headPivot.localRotation = headRotation;
            for (var i = 0; i < eyes.Length; i++)
                if (eyes[i]) { eyes[i].localPosition = eyePositions[i]; eyes[i].localScale = eyeScales[i]; }
            gaze = Vector2.zero; blinkElapsed = -1; blinkWait = Random.Range(3f, 6f);
        }

        /// <summary>Совмещает моргание, взгляд и движение головы текущего жеста.</summary>
        /// <param name="deltaTime">Прошедшее время без замедления игры.</param>
        /// <param name="gesture">Текущий крупный жест.</param>
        /// <param name="progress">Пройденная часть жеста от 0 до 1.</param>
        /// <param name="weight">Сила жеста, уменьшающаяся при прерывании.</param>
        /// <param name="allowGaze">Можно ли следить за мышью; при вращении нельзя.</param>
        /// <param name="pointerViewport">Положение мыши относительно изображения: от 0 до 1.</param>
        /// <param name="hasFocus">Получает ли игра ввод пользователя.</param>
        /// <param name="camera">Камера лобби; отсутствие камеры отключает только слежение.</param>
        public void Animate(float deltaTime, CharacterGesture gesture, float progress, float weight,
            bool allowGaze, Vector2 pointerViewport, bool hasFocus, Camera camera)
        {
            if (eyes == null) CapturePose();
            deltaTime = Mathf.Max(0, deltaTime); progress = Mathf.Clamp01(progress); weight = Mathf.Clamp01(weight);
            var target = Vector2.zero;
            if (allowGaze && hasFocus && camera && pointerViewport.x >= 0 && pointerViewport.x <= 1
                && pointerViewport.y >= 0 && pointerViewport.y <= 1)
            {
                // Угол вокруг вертикали не зависит от наклона камеры или уже повернувшейся головы.
                var towardCamera = Vector3.ProjectOnPlane(camera.transform.position - transform.position, Vector3.up);
                var forward = Vector3.ProjectOnPlane(transform.TransformDirection(Vector3.back), Vector3.up);
                if (Vector3.Angle(forward, towardCamera) <= 60.001f) target = pointerViewport * 2 - Vector2.one;
            }
            gaze = Vector2.Lerp(gaze, target, 1 - Mathf.Exp(-12 * deltaTime));
            var ownsHead = gesture == CharacterGesture.LookAround || gesture == CharacterGesture.ColorReaction;
            var gazeWeight = ownsHead ? 1 - weight : 1;
            if (headPivot)
            {
                var pitch = gaze.y * 8 * gazeWeight;
                var yaw = -gaze.x * 12 * gazeWeight;
                if (gesture == CharacterGesture.LookAround)
                {
                    var envelope = Mathf.SmoothStep(0, 1, progress / .2f) * Mathf.SmoothStep(0, 1, (1 - progress) / .2f);
                    yaw += Mathf.Sin(progress * Mathf.PI * 2) * 20 * weight * envelope;
                }
                if (gesture == CharacterGesture.ColorReaction)
                {
                    var seconds = progress * 1.6f;
                    if (seconds < .5f) pitch += -15 * Mathf.SmoothStep(0, 1, seconds / .5f) * weight;
                    else if (seconds < 1.3f) pitch += (-15 + Mathf.Sin((seconds - .5f) / .8f * Mathf.PI * 4) * 6) * weight;
                    else pitch += -15 * (1 - Mathf.SmoothStep(0, 1, (seconds - 1.3f) / .3f)) * weight;
                }
                headPivot.localRotation = headRotation * Quaternion.Euler(pitch, yaw, 0);
            }
            var closure = Blink(deltaTime);
            for (var eye = 0; eye < 2; eye++)
            {
                if (!completeEyes[eye]) continue;
                var centerY = eyePositions[eye * 3].y;
                for (var detail = 0; detail < 3; detail++)
                {
                    var index = eye * 3 + detail;
                    var position = eyePositions[index];
                    if (detail != 0) position += new Vector3(gaze.x * .035f, gaze.y * .025f, 0) * gazeWeight;
                    position.y = centerY + (position.y - centerY) * closure;
                    eyes[index].localPosition = position;
                    eyes[index].localScale = Vector3.Scale(eyeScales[index], new Vector3(1, closure, 1));
                }
            }
        }

        /// <summary>Возвращает долю открытого глаза, продолжая таймер независимо от жестов.</summary>
        /// <param name="deltaTime">Прошедшее время.</param>
        /// <returns>1 для открытого глаза, около 0 для закрытого.</returns>
        float Blink(float deltaTime)
        {
            if (blinkElapsed < 0)
            {
                blinkWait -= deltaTime;
                if (blinkWait > 0) return 1;
                blinkElapsed = -blinkWait;
            }
            else blinkElapsed += deltaTime;
            if (blinkElapsed >= .16f) { blinkElapsed = -1; blinkWait = Random.Range(3f, 6f); return 1; }
            return 1 - Mathf.Sin(blinkElapsed / .16f * Mathf.PI) * .97f;
        }

        /// <summary>Не оставляет закрытые глаза после отключения компонента.</summary>
        void OnDisable() { if (eyes != null) ResetPose(); }
    }
}
