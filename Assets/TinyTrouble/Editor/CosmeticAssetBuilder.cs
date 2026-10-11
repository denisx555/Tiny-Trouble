using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TinyTrouble.EditorTools
{
    /// <summary>Готовит сохраняемый каталог магазина и его собственные изображения.</summary>
    public static class CosmeticAssetBuilder
    {
        /// <summary>Папка собственных косметических ассетов игры.</summary>
        public const string Root = "Assets/TinyTrouble/Cosmetics";
        /// <summary>Создаёт собственные картинки, материалы и модели товаров.</summary>
        /// <param name="catalog">Каталог, в который записываются визуальные ссылки.</param>
        public static void BuildVisuals(CosmeticCatalog catalog)
        {
            foreach (var folder in new[] { "Materials", "Textures", "Models", "Icons", "Meshes" }) Directory.CreateDirectory(Root + "/" + folder);
            AssetDatabase.Refresh();
            foreach (var item in catalog.Items)
            {
                var materialPath = Root + "/Materials/" + item.Id + ".mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (!material) { material = new Material(Shader.Find("Standard")); AssetDatabase.CreateAsset(material, materialPath); }
                var solidEquipment = item.Category == CosmeticCategory.Weapon || item.Category == CosmeticCategory.Pickaxe || item.Category == CosmeticCategory.Parachute || item.Category == CosmeticCategory.Trail;
                material.shader = Shader.Find(item.Category == CosmeticCategory.Spray ? "Unlit/Transparent" : "Standard");
                material.color = item.BasicColorIndex >= 0 || solidEquipment ? CosmeticTextureBuilder.Tint(item) : Color.white;
                material.SetFloat("_Glossiness", .25f);
                if (item.BasicColorIndex < 0)
                {
                    var texture = CosmeticTextureBuilder.Draw(item);
                    var path = Root + "/Textures/" + item.Id + ".png";
                    File.WriteAllBytes(path, texture.EncodeToPNG());
                    Object.DestroyImmediate(texture);
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                    var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                    importer.wrapMode = item.Category == CosmeticCategory.Spray ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
                    importer.alphaIsTransparency = item.Category == CosmeticCategory.Spray;
                    importer.textureType = TextureImporterType.Default;
                    importer.SaveAndReimport();
                    material.mainTexture = solidEquipment ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                }
                else material.mainTexture = null;
                item.Material = material;
                EditorUtility.SetDirty(material);
                var model = CosmeticModelBuilder.Create(item, material);
                try
                {
                    if (item.Category != CosmeticCategory.Clothes && item.Category != CosmeticCategory.Dance)
                    {
                        SaveMeshes(item.Id, model);
                        item.PreviewPrefab = PrefabUtility.SaveAsPrefabAsset(model, Root + "/Models/" + item.Id + ".prefab");
                    }
                    if (item.Category == CosmeticCategory.Clothes)
                    {
                        var appearance = model.AddComponent<CharacterAppearance>();
                        appearance.Configure(model.GetComponentsInChildren<Renderer>().Where(r => r.name.StartsWith("Hoodie")).ToArray());
                        appearance.Apply(item);
                    }
                    item.Icon = RenderIcon(item, model);
                }
                finally { Object.DestroyImmediate(model); }
            }
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
        }
        /// <summary>Сохраняет собственные сетки деталей, чтобы они не исчезали после перезапуска Unity.</summary>
        /// <param name="id">Код предмета.</param>
        /// <param name="model">Новый образец с временными сетками.</param>
        static void SaveMeshes(string id, GameObject model)
        {
            var path = Root + "/Meshes/" + id + ".asset";
            var filters = model.GetComponentsInChildren<MeshFilter>().Where(f => f.sharedMesh && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(f.sharedMesh))).ToArray();
            if (filters.Length == 0) return;
            if (!AssetDatabase.LoadAssetAtPath<Mesh>(path)) AssetDatabase.CreateAsset(new Mesh { name = "MeshCollection" }, path);
            var saved = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>().ToDictionary(m => m.name);
            foreach (var filter in filters)
            {
                var mesh = filter.sharedMesh;
                if (saved.TryGetValue(mesh.name, out var previous))
                { // Обновляем сетку через её свойства: простое копирование оставляет старое изображение в видеопамяти.
                    previous.Clear(); previous.vertices = mesh.vertices; previous.uv = mesh.uv;
                    previous.normals = mesh.normals; previous.triangles = mesh.triangles; previous.bounds = mesh.bounds;
                    filter.sharedMesh = previous; EditorUtility.SetDirty(previous); Object.DestroyImmediate(mesh); }
                else { AssetDatabase.AddObjectToAsset(mesh, path); saved[mesh.name] = mesh; }
            }
        }
        /// <summary>Снимает маленькое изображение объёмного товара в отдельную текстуру.</summary>
        /// <param name="item">Товар с постоянным именем файла.</param>
        /// <param name="model">Временная модель для фотографии.</param>
        /// <returns>Сохранённая картинка карточки.</returns>
        static Sprite RenderIcon(CosmeticItem item, GameObject model)
        {
            var cameraObject = new GameObject("CosmeticIconCamera");
            var lightObject = new GameObject("CosmeticIconLight");
            var rt = RenderTexture.GetTemporary(256, 256, 24);
            var before = RenderTexture.active;
            var picture = new Texture2D(256, 256, TextureFormat.RGBA32, false);
            try
            {
                model.transform.position = new Vector3(1000, 1000, 1000);
                foreach (var child in model.GetComponentsInChildren<Transform>()) child.gameObject.layer = 31;
                var surfaces = model.GetComponentsInChildren<Renderer>();
                var bounds = surfaces[0].bounds;
                foreach (var surface in surfaces.Skip(1)) bounds.Encapsulate(surface.bounds);
                var camera = cameraObject.AddComponent<Camera>();
                camera.cullingMask = 1 << 31; camera.targetTexture = rt;
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.07f, .12f, .22f);
                camera.fieldOfView = 32; camera.nearClipPlane = .01f; camera.farClipPlane = 100;
                var view = item.Category == CosmeticCategory.Spray ? Vector3.back : new Vector3(.7f, .3f, -1).normalized;
                camera.transform.position = bounds.center + view * bounds.extents.magnitude / Mathf.Tan(16 * Mathf.Deg2Rad) * 1.15f;
                camera.transform.LookAt(bounds.center);
                var light = lightObject.AddComponent<Light>(); light.type = LightType.Point; light.intensity = 1.25f; light.range = 20; light.cullingMask = 1 << 31;
                light.transform.position = bounds.center + new Vector3(-2, 3, -4);
                camera.Render(); RenderTexture.active = rt;
                picture.ReadPixels(new Rect(0, 0, 256, 256), 0, 0); picture.Apply();
                var path = Root + "/Icons/" + item.Id + ".png";
                File.WriteAllBytes(path, picture.EncodeToPNG());
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
                importer.SaveAndReimport();
                return AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            finally
            { RenderTexture.active = before; RenderTexture.ReleaseTemporary(rt); Object.DestroyImmediate(picture); Object.DestroyImmediate(cameraObject); Object.DestroyImmediate(lightObject); }
        }
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
                new CosmeticItem { Id = "pistol-gray", Name = "Серый", Description = "Бесплатный стартовый скин пистолета.", Category = CosmeticCategory.Weapon, Slot = CosmeticSlot.Pistol },
                new CosmeticItem { Id = "rifle-gray", Name = "Серый", Description = "Бесплатный стартовый скин автомата.", Category = CosmeticCategory.Weapon, Slot = CosmeticSlot.Rifle },
                new CosmeticItem { Id = "shotgun-gray", Name = "Серый", Description = "Бесплатный стартовый скин дробовика.", Category = CosmeticCategory.Weapon, Slot = CosmeticSlot.Shotgun },
                new CosmeticItem { Id = "parachute-gray", Name = "Серый", Description = "Серый купол для первых приземлений.", Category = CosmeticCategory.Parachute, Slot = CosmeticSlot.Parachute },
                new CosmeticItem { Id = "trail-gray", Name = "Воздух", Description = "Мягкие серые полосы воздуха за падающим героем.", Category = CosmeticCategory.Trail, Slot = CosmeticSlot.Trail },
                new CosmeticItem { Id = "pickaxe-gray", Name = "Серая", Description = "Бесплатная стартовая кирка со стальным лезвием.", Category = CosmeticCategory.Pickaxe, Slot = CosmeticSlot.Pickaxe },
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
                new CosmeticItem { Id = "pickaxe-space", Name = "Космическая", Description = "Кирка со звёздами для крутости.", Category = CosmeticCategory.Pickaxe, Slot = CosmeticSlot.Pickaxe, Price = 400 },
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
