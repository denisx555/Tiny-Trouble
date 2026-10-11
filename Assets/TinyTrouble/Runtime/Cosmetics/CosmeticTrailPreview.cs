using UnityEngine;

namespace TinyTrouble
{
    /// <summary>Двигает отдельный образец следа падения, не затрагивая героя лобби.</summary>
    public sealed class CosmeticTrailPreview : MonoBehaviour
    {
        /// <summary>Полный круг движения в секундах, выбранный в макете вместе с пользователем.</summary>
        const float LoopSeconds = 2.4f;
        /// <summary>Частицы или ленты образца и их начальные положения.</summary>
        Transform[] parts;
        /// <summary>Начало движения каждой частицы.</summary>
        Vector3[] starts;
        /// <summary>Прошедшая часть полного круга движения.</summary>
        float phase;
        /// <summary>Подключает части выбранного следа.</summary>
        /// <param name="item">Товар следа.</param>
        public void Configure(CosmeticItem item)
        {
            parts = new Transform[transform.childCount]; starts = new Vector3[parts.Length];
            for (var i = 0; i < parts.Length; i++) { parts[i] = transform.GetChild(i); starts[i] = parts[i].localPosition; }
            ResetPreview();
        }
        /// <summary>Продвигает частицы на прошедшее время; полный круг возвращает исходную позу.</summary>
        /// <param name="unscaledDeltaTime">Секунды без замедления игры.</param>
        public void Advance(float unscaledDeltaTime)
        {
            phase = Mathf.Repeat(phase + Mathf.Max(0, unscaledDeltaTime), LoopSeconds);
            if (parts == null) return;
            var angle = phase / LoopSeconds * Mathf.PI * 2;
            for (var i = 0; i < parts.Length; i++)
                parts[i].localPosition = starts[i] + new Vector3(Mathf.Sin(angle + i * .4f) - Mathf.Sin(i * .4f), 1 - Mathf.Cos(angle), 0) * .2f;
        }
        /// <summary>Возвращает начало образца движения.</summary>
        public void ResetPreview()
        { phase = 0; if (parts != null) for (var i = 0; i < parts.Length; i++) parts[i].localPosition = starts[i]; }
        /// <summary>Двигает образец даже при остановленном игровом времени.</summary>
        void Update() { Advance(Time.unscaledDeltaTime); }
    }
}
