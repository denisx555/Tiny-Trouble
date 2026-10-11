using UnityEngine;

namespace TinyTrouble
{
    /// <summary>Двигает тело и конечности героя, сохраняя исходные положения связанных деталей.</summary>
    public sealed class CharacterGestures : MonoBehaviour
    {
        /// <summary>Высота подъёма верхней части тела на вдохе; большее число делает дыхание заметнее.</summary>
        const float BreathingLift = .024f;
        /// <summary>Изменение ширины и глубины тела на вдохе: 0,03 означает три процента.</summary>
        const float BreathingExpansion = .03f;
        /// <summary>Группа тела, головы и рук: дыхание перемещает её целиком.</summary>
        [SerializeField] Transform upperBody;
        /// <summary>Точки поворота рук у плеч.</summary>
        [SerializeField] Transform leftArm, rightArm;
        /// <summary>Точки поворота ступней у носков.</summary>
        [SerializeField] Transform leftFoot, rightFoot;
        /// <summary>Постоянная область всех поз относительно корня героя; её рассчитывает редактор.</summary>
        [SerializeField] Bounds poseBounds;
        /// <summary>Пять групп, которыми владеет только этот компонент.</summary>
        Transform[] joints;
        /// <summary>Исходные положения и размеры групп, чтобы движения не накапливались.</summary>
        Vector3[] positions, scales;
        /// <summary>Исходные повороты групп.</summary>
        Quaternion[] rotations;
        /// <summary>Не даёт повторять предупреждение о неполной модели каждый кадр.</summary>
        bool warned;

        /// <summary>Возвращает сохранённую область всех движений в координатах корня.</summary>
        public Bounds PoseBounds => poseBounds;

        /// <summary>Сохраняет область, рассчитанную инструментом редактора.</summary>
        /// <param name="bounds">Границы допустимых поз.</param>
        public void SetPoseBounds(Bounds bounds) { poseBounds = bounds; }

        /// <summary>Подключает группы частей модели и запоминает их исходную позу.</summary>
        /// <param name="upperBody">Тело вместе с головой и руками.</param>
        /// <param name="leftArm">Левое плечо.</param>
        /// <param name="rightArm">Правое плечо.</param>
        /// <param name="leftFoot">Левый носок.</param>
        /// <param name="rightFoot">Правый носок.</param>
        public void Configure(Transform upperBody, Transform leftArm, Transform rightArm, Transform leftFoot, Transform rightFoot)
        {
            this.upperBody = upperBody; this.leftArm = leftArm; this.rightArm = rightArm;
            this.leftFoot = leftFoot; this.rightFoot = rightFoot;
            CapturePose();
        }

        /// <summary>Запоминает положения один раз, пропуская отсутствующие группы.</summary>
        void CapturePose()
        {
            joints = new[] { upperBody, leftArm, rightArm, leftFoot, rightFoot };
            positions = new Vector3[5]; scales = new Vector3[5]; rotations = new Quaternion[5];
            for (var i = 0; i < joints.Length; i++)
            {
                if (!joints[i])
                {
                    if (!warned) { Debug.LogWarning("CharacterGestures: часть модели отсутствует; её движение пропущено.", this); warned = true; }
                    continue;
                }
                positions[i] = joints[i].localPosition; rotations[i] = joints[i].localRotation; scales[i] = joints[i].localScale;
            }
        }

        /// <summary>Возвращает только принадлежащие этому компоненту группы в исходную позу.</summary>
        public void ResetPose()
        {
            if (joints == null) CapturePose();
            for (var i = 0; i < joints.Length; i++)
                if (joints[i]) { joints[i].localPosition = positions[i]; joints[i].localRotation = rotations[i]; joints[i].localScale = scales[i]; }
        }

        /// <summary>Применяет дыхание и выбранное движение, не изменяя поворот корня.</summary>
        /// <param name="gesture">Выбранный жест.</param>
        /// <param name="progress">Пройденная часть жеста от 0 до 1.</param>
        /// <param name="weight">Сила жеста от 0 до 1, включая плавное прерывание.</param>
        /// <param name="breathingPhase">Место в цикле дыхания в радианах; полный круг равен двум Pi.</param>
        public void ApplyPose(CharacterGesture gesture, float progress, float weight, float breathingPhase)
        {
            ResetPose();
            progress = Mathf.Clamp01(progress);
            var strength = Mathf.Clamp01(weight) * Mathf.SmoothStep(0, 1, progress / .2f)
                * Mathf.SmoothStep(0, 1, (1 - progress) / .2f);
            var breath = Mathf.Sin(breathingPhase);
            if (upperBody)
            {
                upperBody.localPosition = positions[0] + Vector3.up * (breath * BreathingLift);
                upperBody.localScale = Vector3.Scale(scales[0], new Vector3(1 + breath * BreathingExpansion, 1, 1 + breath * BreathingExpansion));
            }
            if (gesture == CharacterGesture.WeightShift)
            {
                var shift = Mathf.Sin(progress * Mathf.PI * 2) * strength;
                if (upperBody) upperBody.localRotation = rotations[0] * Quaternion.Euler(0, 0, shift * 3);
                if (leftFoot) leftFoot.localRotation = rotations[3] * Quaternion.Euler(-Mathf.Max(0, shift) * 6, 0, 0);
                if (rightFoot) rightFoot.localRotation = rotations[4] * Quaternion.Euler(-Mathf.Max(0, -shift) * 6, 0, 0);
            }
            if ((gesture == CharacterGesture.Wave || gesture == CharacterGesture.Greeting) && rightArm)
                rightArm.localRotation = rotations[2] * Quaternion.Euler(0, 0,
                    (55 + Mathf.Sin(progress * Mathf.PI * 6) * 12) * strength);
            if (gesture >= CharacterGesture.DanceRobot)
            {
                var beat = Mathf.Sin(progress * Mathf.PI * 8);
                var left = 0f; var right = 0f; var tilt = 0f; var lift = 0f;
                if (gesture == CharacterGesture.DanceRobot)
                { var step = Mathf.Round(beat * 4) / 4; left = -45 - step * 25; right = 45 - step * 25; tilt = step * 5; }
                if (gesture == CharacterGesture.DanceSpring)
                { left = -20 - beat * 15; right = 20 + beat * 15; lift = beat * .05f; }
                if (gesture == CharacterGesture.DanceVictory)
                { left = -110 - beat * 12; right = 110 + beat * 12; tilt = beat * 6; }
                if (upperBody)
                { upperBody.localPosition += Vector3.up * lift * strength; upperBody.localRotation = rotations[0] * Quaternion.Euler(0, 0, tilt * strength); }
                if (leftArm) leftArm.localRotation = rotations[1] * Quaternion.Euler(0, 0, left * strength);
                if (rightArm) rightArm.localRotation = rotations[2] * Quaternion.Euler(0, 0, right * strength);
                if (leftFoot) leftFoot.localRotation = rotations[3] * Quaternion.Euler(-Mathf.Max(0, beat) * 8 * strength, 0, 0);
                if (rightFoot) rightFoot.localRotation = rotations[4] * Quaternion.Euler(-Mathf.Max(0, -beat) * 8 * strength, 0, 0);
            }
        }

        /// <summary>Не оставляет согнутые части после отключения компонента.</summary>
        void OnDisable() { if (joints != null) ResetPose(); }
    }
}
