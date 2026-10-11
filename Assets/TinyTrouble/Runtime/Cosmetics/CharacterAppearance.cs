using UnityEngine;
using System.Linq;

namespace TinyTrouble
{
    /// <summary>Единственный владелец личных материалов одежды героя.</summary>
    public sealed class CharacterAppearance : MonoBehaviour
    {
        /// <summary>Только поверхности одежды, которым принадлежат наши копии материалов.</summary>
        Renderer[] renderers;
        /// <summary>Исходные общие материалы и созданные нами личные копии.</summary>
        Material[] originals, owned;
        /// <summary>Последняя применённая одежда для возврата после примерки.</summary>
        CosmeticItem applied;
        /// <summary>Код закреплённой одежды, без учёта временного просмотра.</summary>
        public string AppliedId { get; private set; }
        /// <summary>Подключает только детали одежды и создаёт личные материалы.</summary>
        /// <param name="hoodieRenderers">Рендереры толстовки.</param>
        public void Configure(Renderer[] hoodieRenderers)
        {
            if (renderers != null && renderers.SequenceEqual(hoodieRenderers)) return;
            Release();
            renderers = hoodieRenderers;
            originals = renderers.Select(r => r.sharedMaterial).ToArray();
            owned = originals.Select(m => new Material(m)).ToArray();
            for (var i = 0; i < renderers.Length; i++) renderers[i].sharedMaterial = owned[i];
        }
        /// <summary>Закрепляет внешний вид одежды.</summary>
        /// <param name="item">Определение доступной одежды.</param>
        public void Apply(CosmeticItem item)
        { if (item == null || item.Category != CosmeticCategory.Clothes || !item.Material) return; applied = item; AppliedId = item.Id; Paint(item); }
        /// <summary>Временно показывает одежду без изменения закреплённого выбора.</summary>
        /// <param name="item">Просматриваемая одежда.</param>
        public void Preview(CosmeticItem item)
        { if (item != null && item.Category == CosmeticCategory.Clothes && item.Material) Paint(item); }
        /// <summary>Возвращает закреплённый внешний вид.</summary>
        public void Restore() { if (applied != null) Paint(applied); }
        /// <summary>Изменяет только личные материалы, сохраняя общие ассеты.</summary>
        /// <param name="item">Одежда с нужным цветом и рисунком.</param>
        void Paint(CosmeticItem item)
        { if (owned != null) foreach (var material in owned) material.CopyPropertiesFromMaterial(item.Material); }
        /// <summary>Возвращает исходные ссылки и освобождает созданные копии.</summary>
        void Release()
        {
            if (owned == null) return;
            for (var i = 0; i < owned.Length; i++)
            {
                if (renderers[i]) renderers[i].sharedMaterial = originals[i];
                if (owned[i]) { if (Application.isPlaying) Destroy(owned[i]); else DestroyImmediate(owned[i]); }
            }
            owned = null; renderers = null; applied = null; AppliedId = null;
        }
        /// <summary>Очищает личные ресурсы при закрытии лобби или удалении компонента.</summary>
        void OnDestroy() { Release(); }
    }
}
