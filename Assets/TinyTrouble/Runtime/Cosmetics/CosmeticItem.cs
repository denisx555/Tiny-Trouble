using System;
using UnityEngine;

namespace TinyTrouble
{
    /// <summary>Раздел магазина; оружие объединяет три отдельных места выбора.</summary>
    public enum CosmeticCategory { Clothes, Weapon, Dance, Spray, Parachute, Pet, Trail, Pickaxe }
    /// <summary>Место одной выбранной вещи; номера записываются в сохранение.</summary>
    public enum CosmeticSlot { Clothes = 0, Pistol = 1, Rifle = 2, Shotgun = 3, Dance = 4, Spray = 5, Parachute = 6, Pet = 7, Trail = 8, Pickaxe = 9 }
    /// <summary>Описание одной вещи, её цены и изображения в каталоге Unity.</summary>
    [Serializable] public sealed class CosmeticItem
    {
        /// <summary>Постоянный код вещи, который не зависит от её места в списке.</summary>
        public string Id;
        /// <summary>Название, показанное игроку.</summary>
        public string Name;
        /// <summary>Короткое пояснение внешнего вида.</summary>
        public string Description;
        /// <summary>Раздел, в котором находится карточка.</summary>
        public CosmeticCategory Category;
        /// <summary>Место, занятое после выбора этой вещи.</summary>
        public CosmeticSlot Slot;
        /// <summary>Цена в Шалкоинах; ноль обозначает бесплатную стартовую вещь.</summary>
        public int Price;
        /// <summary>Номер бесплатного цвета одежды; у других вещей равен −1.</summary>
        public int BasicColorIndex = -1;
        /// <summary>Картинка для маленькой карточки.</summary>
        public Sprite Icon;
        /// <summary>Материал одежды с цветом и рисунком.</summary>
        public Material Material;
        /// <summary>Сохранённая модель для крупного просмотра.</summary>
        public GameObject PreviewPrefab;
    }
}
