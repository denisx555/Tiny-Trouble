using UnityEngine;

namespace TinyTrouble
{
    /// <summary>
    /// Слегка покачивает персонажа на платформе и сохраняет выбранный мышью поворот.
    /// </summary>
    public sealed class CharacterIdle : MonoBehaviour
    {
        /// <summary>
        /// Сила наклона в градусах. SerializeField позволяет менять значение в панели Inspector в Unity.
        /// </summary>
        [SerializeField] float tiltDegrees = 1.3f;
        /// <summary>
        /// Скорость покачивания: большее число ускоряет движение.
        /// </summary>
        [SerializeField] float speed = 1.7f;
        /// <summary>
        /// Начальное положение героя относительно родительского объекта.
        /// </summary>
        Vector3 basePosition;
        /// <summary>
        /// Начальный поворот героя. Quaternion — способ хранения поворота в Unity.
        /// </summary>
        Quaternion baseRotation;
        /// <summary>
        /// Поворот вокруг вертикальной оси, который игрок выбрал в текущем лобби.
        /// </summary>
        float turnDegrees;
        /// <summary>
        /// Unity вызывает этот метод при создании компонента: запоминаем исходную позу.
        /// </summary>
        void Awake() { basePosition = transform.localPosition; baseRotation = transform.localRotation; }
        /// <summary>
        /// Добавляет поворот игрока к покачиванию, не меняя исходную позу сцены.
        /// </summary>
        /// <param name="degrees">Поворот в градусах: положительное и отрицательное значения вращают в разные стороны.</param>
        public void Turn(float degrees)
        {
            // Repeat оставляет угол в пределах одного круга даже после многих движений мыши.
            turnDegrees = Mathf.Repeat(turnDegrees + degrees, 360f);
        }
        /// <summary>
        /// Unity вызывает этот метод каждый кадр: плавно меняем наклон и высоту героя.
        /// </summary>
        void Update()
        {
            // phase — время движения с учётом скорости; unscaledTime не зависит от замедления игры.
            var phase = Time.unscaledTime * speed;
            // Sin плавно меняется от -1 до 1: наклон идёт туда и обратно.
            // Euler создаёт поворот из углов в градусах.
            transform.localRotation = baseRotation * Quaternion.Euler(0, turnDegrees + Mathf.Sin(phase * .6f) * 2f, Mathf.Sin(phase) * tiltDegrees);
            // Прибавляем небольшое движение вверх и вниз к исходной позиции.
            transform.localPosition = basePosition + Vector3.up * (Mathf.Sin(phase * 2f) * .012f);
        }
    }
}
