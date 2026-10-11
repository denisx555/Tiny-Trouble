using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TinyTrouble
{
    /// <summary>Хранит товары отдельно от покупок игрока.</summary>
    [CreateAssetMenu(menuName = "Tiny Trouble/Каталог косметики")]
    public sealed class CosmeticCatalog : ScriptableObject
    {
        /// <summary>Список сохраняется внутри ассета каталога.</summary>
        [SerializeField] CosmeticItem[] items = Array.Empty<CosmeticItem>();
        /// <summary>Товары в порядке карточек.</summary>
        public IReadOnlyList<CosmeticItem> Items => items;
        /// <summary>Задаёт список при подготовке каталога в редакторе.</summary>
        /// <param name="values">Определения товаров.</param>
        public void Configure(CosmeticItem[] values) { items = values ?? Array.Empty<CosmeticItem>(); }
        /// <summary>Ищет вещь по постоянному коду.</summary>
        /// <param name="id">Код вещи.</param>
        /// <param name="item">Найденная вещь.</param>
        /// <returns>Есть ли такая вещь.</returns>
        public bool TryGet(string id, out CosmeticItem item)
        { item = items.FirstOrDefault(value => value != null && value.Id == id); return item != null; }
        /// <summary>Находит ошибки определений и визуальных ссылок.</summary>
        /// <param name="requireVisuals">Нужно ли проверять модели и картинки.</param>
        /// <returns>Список ошибок.</returns>
        public string[] Validate(bool requireVisuals)
        {
            var errors = new List<string>();
            var ids = new HashSet<string>();
            foreach (var item in items)
            {
                if (item == null) { errors.Add("Пустое определение товара"); continue; }
                if (string.IsNullOrWhiteSpace(item.Id) || !ids.Add(item.Id)) errors.Add("Повторный или пустой код: " + item.Id);
                if (item.Price < 0) errors.Add("Отрицательная цена: " + item.Id);
                var validSlot = item.Category == CosmeticCategory.Weapon
                    ? item.Slot >= CosmeticSlot.Pistol && item.Slot <= CosmeticSlot.Shotgun
                    : (int)item.Slot == (item.Category == CosmeticCategory.Clothes ? 0 : (int)item.Category + 2);
                if (!Enum.IsDefined(typeof(CosmeticCategory), item.Category) || !validSlot) errors.Add("Неверное место выбора: " + item.Id);
                if (requireVisuals)
                {
                    if (!item.Icon) errors.Add("Нет картинки: " + item.Id);
                    if (item.Category == CosmeticCategory.Clothes ? !item.Material : item.Category != CosmeticCategory.Dance && !item.PreviewPrefab)
                        errors.Add("Нет материала или модели: " + item.Id);
                }
            }
            return errors.ToArray();
        }
    }
}
