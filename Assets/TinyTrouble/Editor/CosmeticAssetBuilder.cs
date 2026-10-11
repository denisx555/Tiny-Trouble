using System.IO;
using UnityEditor;
using UnityEngine;

namespace TinyTrouble.EditorTools
{
    /// <summary>Готовит сохраняемый каталог магазина и его собственные изображения.</summary>
    public static class CosmeticAssetBuilder
    {
        /// <summary>Папка собственных косметических ассетов игры.</summary>
        public const string Root = "Assets/TinyTrouble/Cosmetics";
        /// <summary>Создаёт или обновляет определения товаров без изменения их кодов.</summary>
        /// <returns>Сохранённый каталог косметики.</returns>
        public static CosmeticCatalog BuildCatalog()
        {
            Directory.CreateDirectory(Root);
            AssetDatabase.Refresh();
            var catalog = AssetDatabase.LoadAssetAtPath<CosmeticCatalog>(Root + "/Catalog.asset");
            if (!catalog)
            {
                catalog = ScriptableObject.CreateInstance<CosmeticCatalog>();
                AssetDatabase.CreateAsset(catalog, Root + "/Catalog.asset");
            }
            var definitions = new CosmeticItem[]
            {
                new CosmeticItem { Id = "stripes", Name = "Полосатый", Description = "Яркие полосы на любимой толстовке.", Category = CosmeticCategory.Clothes, Slot = CosmeticSlot.Clothes, Price = 400 },
                new CosmeticItem { Id = "space", Name = "Космос", Description = "Толстовка со звёздами для новых приключений.", Category = CosmeticCategory.Clothes, Slot = CosmeticSlot.Clothes, Price = 400 },
                new CosmeticItem { Id = "neon", Name = "Неон", Description = "Яркие линии на тёмной толстовке.", Category = CosmeticCategory.Clothes, Slot = CosmeticSlot.Clothes, Price = 400 },
                new CosmeticItem { Id = "pistol-pop", Name = "Конфетти", Description = "Пёстрый скин мультяшного пистолета.", Category = CosmeticCategory.Weapon, Slot = CosmeticSlot.Pistol, Price = 350 },
                new CosmeticItem { Id = "pistol-ice", Name = "Ледяной", Description = "Голубой скин мультяшного пистолета.", Category = CosmeticCategory.Weapon, Slot = CosmeticSlot.Pistol, Price = 350 },
                new CosmeticItem { Id = "rifle-space", Name = "Галактика", Description = "Космический скин мультяшного автомата.", Category = CosmeticCategory.Weapon, Slot = CosmeticSlot.Rifle, Price = 350 },
                new CosmeticItem { Id = "rifle-lime", Name = "Лайм", Description = "Зелёный скин мультяшного автомата.", Category = CosmeticCategory.Weapon, Slot = CosmeticSlot.Rifle, Price = 350 },
                new CosmeticItem { Id = "shotgun-fire", Name = "Вспышка", Description = "Оранжевый скин мультяшного дробовика.", Category = CosmeticCategory.Weapon, Slot = CosmeticSlot.Shotgun, Price = 350 },
                new CosmeticItem { Id = "shotgun-wave", Name = "Волна", Description = "Синий скин мультяшного дробовика.", Category = CosmeticCategory.Weapon, Slot = CosmeticSlot.Shotgun, Price = 350 },
                new CosmeticItem { Id = "robot", Name = "Робот", Description = "Чёткие движения рук и забавные повороты.", Category = CosmeticCategory.Dance, Slot = CosmeticSlot.Dance, Price = 500 },
                new CosmeticItem { Id = "spring", Name = "Пружинка", Description = "Пружинистые движения и лёгкие подпрыгивания.", Category = CosmeticCategory.Dance, Slot = CosmeticSlot.Dance, Price = 500 },
                new CosmeticItem { Id = "victory", Name = "Победный", Description = "Праздничный танец маленького победителя.", Category = CosmeticCategory.Dance, Slot = CosmeticSlot.Dance, Price = 500 },
                new CosmeticItem { Id = "spray-smile", Name = "Улыбка", Description = "Весёлая улыбка для стен и пола в матче.", Category = CosmeticCategory.Spray, Slot = CosmeticSlot.Spray, Price = 300 },
                new CosmeticItem { Id = "spray-mischief", Name = "Шалун", Description = "Хитрый смайлик маленького шалуна.", Category = CosmeticCategory.Spray, Slot = CosmeticSlot.Spray, Price = 300 },
                new CosmeticItem { Id = "spray-boom", Name = "Бум!", Description = "Оставь яркий след в матче.", Category = CosmeticCategory.Spray, Slot = CosmeticSlot.Spray, Price = 300 },
                new CosmeticItem { Id = "parachute-clouds", Name = "Облака", Description = "Голубой парашют с белыми облаками.", Category = CosmeticCategory.Parachute, Slot = CosmeticSlot.Parachute, Price = 400 },
                new CosmeticItem { Id = "parachute-space", Name = "Космос", Description = "Космический купол с яркими звёздами.", Category = CosmeticCategory.Parachute, Slot = CosmeticSlot.Parachute, Price = 400 },
                new CosmeticItem { Id = "parachute-neon", Name = "Неон", Description = "Яркие полосы для заметного приземления.", Category = CosmeticCategory.Parachute, Slot = CosmeticSlot.Parachute, Price = 400 },
                new CosmeticItem { Id = "pet-kitten", Name = "Котёнок", Description = "Любопытный маленький спутник героя.", Category = CosmeticCategory.Pet, Slot = CosmeticSlot.Pet, Price = 600 },
                new CosmeticItem { Id = "pet-puppy", Name = "Щенок", Description = "Супер друг для красоты.", Category = CosmeticCategory.Pet, Slot = CosmeticSlot.Pet, Price = 600 },
                new CosmeticItem { Id = "pet-robot", Name = "Роботик", Description = "Маленький робот, который любит компанию.", Category = CosmeticCategory.Pet, Slot = CosmeticSlot.Pet, Price = 600 },
                new CosmeticItem { Id = "trail-sparks", Name = "Искры", Description = "Тёплые искры за героем во время падения.", Category = CosmeticCategory.Trail, Slot = CosmeticSlot.Trail, Price = 400 },
                new CosmeticItem { Id = "trail-stars", Name = "Звёзды", Description = "Сияющие звёзды на пути к приземлению.", Category = CosmeticCategory.Trail, Slot = CosmeticSlot.Trail, Price = 400 },
                new CosmeticItem { Id = "trail-rainbow", Name = "Радуга", Description = "Разноцветный след за летящим героем.", Category = CosmeticCategory.Trail, Slot = CosmeticSlot.Trail, Price = 400 },
                new CosmeticItem { Id = "pickaxe-ice", Name = "Ледяная", Description = "Кирка с голубым ледяным узором.", Category = CosmeticCategory.Pickaxe, Slot = CosmeticSlot.Pickaxe, Price = 400 },
                new CosmeticItem { Id = "pickaxe-neon", Name = "Неоновая", Description = "Яркие полосы на мультяшной кирке.", Category = CosmeticCategory.Pickaxe, Slot = CosmeticSlot.Pickaxe, Price = 400 },
                new CosmeticItem { Id = "pickaxe-space", Name = "Космическая", Description = "Кирка со звёздами для новых приключений.", Category = CosmeticCategory.Pickaxe, Slot = CosmeticSlot.Pickaxe, Price = 400 },
                new CosmeticItem { Id = "base-yellow", Name = "Жёлтый", Description = "Твой бесплатный цвет толстовки.", Category = CosmeticCategory.Clothes, Slot = CosmeticSlot.Clothes, Price = 0, BasicColorIndex = 0 },
                new CosmeticItem { Id = "base-blue", Name = "Голубой", Description = "Твой бесплатный цвет толстовки.", Category = CosmeticCategory.Clothes, Slot = CosmeticSlot.Clothes, Price = 0, BasicColorIndex = 1 },
                new CosmeticItem { Id = "base-red", Name = "Красный", Description = "Твой бесплатный цвет толстовки.", Category = CosmeticCategory.Clothes, Slot = CosmeticSlot.Clothes, Price = 0, BasicColorIndex = 2 },
                new CosmeticItem { Id = "base-purple", Name = "Фиолетовый", Description = "Твой бесплатный цвет толстовки.", Category = CosmeticCategory.Clothes, Slot = CosmeticSlot.Clothes, Price = 0, BasicColorIndex = 3 }
            };
            // Повторное обновление определений сохраняет уже созданные визуальные ссылки.
            foreach (var item in definitions)
                if (catalog.TryGet(item.Id, out var previous))
                { item.Icon = previous.Icon; item.Material = previous.Material; item.PreviewPrefab = previous.PreviewPrefab; }
            catalog.Configure(definitions);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            return catalog;
        }
    }
}
