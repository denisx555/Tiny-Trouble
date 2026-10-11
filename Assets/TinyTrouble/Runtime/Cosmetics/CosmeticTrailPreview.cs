using System.Linq;
using UnityEngine;

namespace TinyTrouble
{
    /// <summary>Показывает свободное падение: четыре потока от рук и ног уходят назад и затухают.</summary>
    public sealed class CosmeticTrailPreview : MonoBehaviour
    {
        /// <summary>Время повторения движения, выбранное вместе с пользователем.</summary>
        const float LoopSeconds = 2.4f;
        /// <summary>Длина потока в координатах маленького образца.</summary>
        const float FlowLength = 1.65f;
        /// <summary>Ленты, их исходные точки и рабочие точки без выделения памяти каждый кадр.</summary>
        LineRenderer[] ribbons;
        /// <summary>Исходные пути лент.</summary>
        Vector3[][] paths;
        /// <summary>Массивы для обновлённых изгибов.</summary>
        Vector3[][] workingPaths;
        /// <summary>Движущиеся частицы, их начальные направления и масштабы.</summary>
        Transform[] particles;
        /// <summary>Исходные положения частиц задают их разброс и задержку появления.</summary>
        Vector3[] starts;
        /// <summary>Начальный размер каждой частицы.</summary>
        Vector3[] scales;
        /// <summary>Исходные повороты для вращения звёзд.</summary>
        Quaternion[] rotations;
        /// <summary>Временные настройки прозрачности, не меняющие общий материал ассета.</summary>
        MaterialPropertyBlock colors;
        /// <summary>Поверхности частиц, найденные один раз при подготовке просмотра.</summary>
        Renderer[][] surfaces;
        /// <summary>Исходные цвета поверхностей для личного затухания каждой частицы.</summary>
        Color[][] tints;
        /// <summary>Прошедшая часть полного круга движения.</summary>
        float phase;

        /// <summary>Подготавливает позу копии героя и прикрепляет потоки к конечностям.</summary>
        /// <param name="item">Выбранный стиль следа.</param>
        public void Configure(CosmeticItem item)
        {
            colors = new MaterialPropertyBlock();
            var miniature = transform.Find("FallingHero");
            if (miniature)
            {
                SetFallingPose(miniature);
                var surfaces = miniature.GetComponentsInChildren<Renderer>();
                foreach (Transform flow in transform)
                    if (flow.name.StartsWith("Flow-"))
                    {
                        var limb = surfaces.FirstOrDefault(r => r.name == flow.name.Substring(5));
                        if (limb) flow.position = limb.bounds.center;
                    }
            }
            ribbons = GetComponentsInChildren<LineRenderer>().Where(r => r.name.StartsWith("Ribbon")).ToArray();
            paths = ribbons.Select(r => { var points = new Vector3[r.positionCount]; r.GetPositions(points); return points; }).ToArray();
            workingPaths = paths.Select(p => new Vector3[p.Length]).ToArray();
            particles = transform.Cast<Transform>().Where(t => t.name.StartsWith("Flow-"))
                .SelectMany(t => t.Cast<Transform>()).Where(t => !t.name.StartsWith("Ribbon")).ToArray();
            starts = particles.Select(t => t.localPosition).ToArray();
            scales = particles.Select(t => t.localScale).ToArray();
            rotations = particles.Select(t => t.localRotation).ToArray();
            surfaces = particles.Select(t => t.GetComponentsInChildren<Renderer>()).ToArray();
            tints = surfaces.Select(group => group.Select(r => r.sharedMaterial.color).ToArray()).ToArray();
            ResetPreview();
        }
        /// <summary>Наклоняет только копию героя и разводит конечности без отрыва от тела.</summary>
        /// <param name="miniature">Временная копия героя в просмотре.</param>
        static void SetFallingPose(Transform miniature)
        {
            miniature.localRotation = Quaternion.Euler(-65, 0, 0);
            miniature.localScale = Vector3.one * .55f;
            var parts = miniature.GetComponentsInChildren<Transform>();
            foreach (var side in new[] { "Left", "Right" })
            {
                var sign = side == "Left" ? -1 : 1;
                var arm = parts.FirstOrDefault(t => t.name == side + "ArmPivot");
                if (arm) arm.localRotation = Quaternion.Euler(12, 0, sign * 65);
                var foot = parts.FirstOrDefault(t => t.name == side + "FootPivot");
                if (foot)
                {
                    // Группу ноги поворачиваем у бедра, чтобы штаны оставались внутри туловища.
                    var pivot = new GameObject(side + "FallingHip").transform;
                    pivot.SetParent(miniature, false); pivot.localPosition = new Vector3(sign * .25f, .8f, 0);
                    foot.SetParent(pivot, true); pivot.localRotation = Quaternion.Euler(-12, 0, sign * 16);
                }
            }
            var head = parts.FirstOrDefault(t => t.name == "HeadPivot");
            if (head) head.localRotation = Quaternion.Euler(25, 0, 0);
        }
        /// <summary>Изгибает ленты и перемещает частицы от героя назад с плавным исчезновением.</summary>
        /// <param name="unscaledDeltaTime">Секунды без замедления игры.</param>
        public void Advance(float unscaledDeltaTime)
        {
            if (float.IsNaN(unscaledDeltaTime) || float.IsInfinity(unscaledDeltaTime)) return;
            phase = Mathf.Repeat(phase + Mathf.Max(0, unscaledDeltaTime), LoopSeconds);
            var angle = phase / LoopSeconds * Mathf.PI * 2;
            if (ribbons != null)
                for (var i = 0; i < ribbons.Length; i++)
                {
                    for (var p = 0; p < paths[i].Length; p++)
                    {
                        var t = p / (float)(paths[i].Length - 1);
                        workingPaths[i][p] = paths[i][p] + new Vector3(Mathf.Sin(t * 8 - angle + i) * .09f * t, 0, Mathf.Cos(t * 7 - angle + i) * .07f * t);
                    }
                    ribbons[i].SetPositions(workingPaths[i]);
                }
            if (particles == null) return;
            for (var i = 0; i < particles.Length; i++)
            {
                var t = Mathf.Repeat(phase / LoopSeconds + starts[i].y / FlowLength, 1);
                particles[i].localPosition = new Vector3(starts[i].x * t + Mathf.Sin(angle + i) * .06f * t, t * FlowLength, starts[i].z * t);
                particles[i].localScale = scales[i] * Mathf.Lerp(1, .35f, t);
                particles[i].localRotation = rotations[i] * Quaternion.Euler(0, phase / LoopSeconds * 360, phase / LoopSeconds * 360);
                var opacity = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t / .1f)) * (1 - Mathf.SmoothStep(0, 1, Mathf.Clamp01((t - .35f) / .65f)));
                for (var surface = 0; surface < surfaces[i].Length; surface++)
                {
                    var tint = tints[i][surface]; tint.a *= opacity;
                    surfaces[i][surface].GetPropertyBlock(colors); colors.SetColor("_Color", tint); surfaces[i][surface].SetPropertyBlock(colors);
                }
            }
        }
        /// <summary>Возвращает начало повторяющегося движения.</summary>
        public void ResetPreview() { phase = 0; Advance(0); }
        /// <summary>Двигает образец даже при остановленном игровом времени.</summary>
        void Update() { Advance(Time.unscaledDeltaTime); }
    }
}
