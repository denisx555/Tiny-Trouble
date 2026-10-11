using UnityEditor;
using UnityEngine;

namespace TinyTrouble.EditorTools
{
    /// <summary>Собирает небольшие мультяшные модели товаров из простых объёмных фигур.</summary>
    public static class CosmeticModelBuilder
    {
        /// <summary>Создаёт временную модель для сохранения шаблона и фотографии карточки.</summary>
        /// <param name="item">Товар, определяющий форму модели.</param>
        /// <param name="material">Сохранённый материал с узором.</param>
        /// <returns>Корневой объект модели, который затем удаляет вызывающий код.</returns>
        public static GameObject Create(CosmeticItem item, Material material)
        {
            if (item.Category == CosmeticCategory.Clothes || item.Category == CosmeticCategory.Dance)
            {
                var hero = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TinyTrouble/Prefabs/LobbyCharacter.prefab"));
                hero.name = item.Id;
                if (item.Category == CosmeticCategory.Dance)
                {
                    var arm = hero.transform.Find("UpperBodyPivot/RightArmPivot");
                    if (arm) arm.localRotation = Quaternion.Euler(0, 0, item.Id == "robot" ? 90 : item.Id == "spring" ? 35 : 150);
                    hero.transform.rotation = Quaternion.Euler(0, item.Id == "robot" ? -20 : item.Id == "spring" ? 20 : 0, 0);
                }
                return hero;
            }
            var root = new GameObject(item.Id);
            var cream = AssetDatabase.LoadAssetAtPath<Material>("Assets/TinyTrouble/Materials/Cream.mat");
            var ink = AssetDatabase.LoadAssetAtPath<Material>("Assets/TinyTrouble/Materials/Sneakers.mat");
            if (item.Category == CosmeticCategory.Weapon)
            {
                var length = item.Slot == CosmeticSlot.Pistol ? .65f : item.Slot == CosmeticSlot.Rifle ? 1.5f : 1.25f;
                Part(root, "Корпус", PrimitiveType.Cube, Vector3.zero, new Vector3(length, .25f, .24f), material);
                Part(root, "Ствол", PrimitiveType.Cylinder, new Vector3(length * .6f, .02f, 0), new Vector3(.13f, .2f, .13f), ink).localRotation = Quaternion.Euler(0, 0, 90);
                Part(root, "Рукоять", PrimitiveType.Cube, new Vector3(-length * .3f, -.3f, 0), new Vector3(.2f, .45f, .21f), material).localRotation = Quaternion.Euler(0, 0, -15);
                if (item.Slot != CosmeticSlot.Pistol) Part(root, "Приклад", PrimitiveType.Cube, new Vector3(-length * .65f, -.08f, 0), new Vector3(.4f, .33f, .25f), material);
                if (item.Slot == CosmeticSlot.Rifle) Part(root, "Магазин", PrimitiveType.Cube, new Vector3(.1f, -.26f, 0), new Vector3(.2f, .32f, .2f), ink);
                if (item.Slot == CosmeticSlot.Shotgun) Part(root, "Цевьё", PrimitiveType.Cube, new Vector3(.35f, -.14f, 0), new Vector3(.4f, .18f, .3f), cream);
            }
            else if (item.Category == CosmeticCategory.Pickaxe)
            {
                Part(root, "Рукоять", PrimitiveType.Cylinder, new Vector3(0, .7f, 0), new Vector3(.1f, .7f, .1f), ink);
                Part(root, "Лезвие", PrimitiveType.Cube, new Vector3(0, 1.25f, 0), new Vector3(1.1f, .2f, .2f), material).localRotation = Quaternion.Euler(0, 0, -12);
                Part(root, "Наконечник", PrimitiveType.Cube, new Vector3(.55f, 1.05f, 0), new Vector3(.18f, .4f, .2f), material).localRotation = Quaternion.Euler(0, 0, 25);
            }
            else if (item.Category == CosmeticCategory.Parachute)
            {
                Part(root, "Купол", PrimitiveType.Sphere, new Vector3(0, 1.55f, 0), new Vector3(2, .8f, 1.4f), material);
                for (var side = -1; side <= 1; side += 2)
                    for (var depth = -1; depth <= 1; depth += 2)
                        Rod(root, new Vector3(side * .85f, 1.4f, depth * .48f), new Vector3(side * .22f, .1f, depth * .15f), .025f, cream);
                Part(root, "Подвес", PrimitiveType.Cube, new Vector3(0, .08f, 0), new Vector3(.5f, .12f, .4f), ink);
            }
            else if (item.Category == CosmeticCategory.Pet) Pet(root, item, material, cream, ink);
            else if (item.Category == CosmeticCategory.Spray)
                Part(root, "Рисунок", PrimitiveType.Quad, Vector3.zero, new Vector3(1.2f, 1.2f, 1), material);
            else if (item.Category == CosmeticCategory.Trail)
            {
                for (var i = 0; i < 12; i++)
                {
                    var angle = i * 2.4f;
                    Part(root, "Частица" + i, item.Id == "trail-stars" ? PrimitiveType.Cube : PrimitiveType.Sphere,
                        new Vector3(Mathf.Sin(angle) * .2f, i * .1f, Mathf.Cos(angle) * .2f), Vector3.one * (.07f + i * .008f), material).localRotation = Quaternion.Euler(30, 20, 45);
                }
            }
            return root;
        }

        /// <summary>Создаёт котёнка, щенка или маленького робота с различимыми ушами и мордочкой.</summary>
        /// <param name="root">Корень питомца.</param>
        /// <param name="item">Выбранный питомец.</param>
        /// <param name="coat">Цвет шерсти или корпуса.</param>
        /// <param name="cream">Светлый материал глаз.</param>
        /// <param name="ink">Тёмный материал зрачков и лап.</param>
        static void Pet(GameObject root, CosmeticItem item, Material coat, Material cream, Material ink)
        {
            var robot = item.Id == "pet-robot";
            var shape = robot ? PrimitiveType.Cube : PrimitiveType.Sphere;
            Part(root, "Тело", shape, new Vector3(0, .25f, 0), new Vector3(.45f, .4f, .4f), coat);
            Part(root, "Голова", shape, new Vector3(0, .58f, -.03f), new Vector3(.53f, .45f, .45f), coat);
            for (var side = -1; side <= 1; side += 2)
            {
                Part(root, "Глаз" + side, PrimitiveType.Sphere, new Vector3(side * .12f, .62f, -.24f), Vector3.one * .13f, cream);
                Part(root, "Зрачок" + side, PrimitiveType.Sphere, new Vector3(side * .12f, .62f, -.3f), Vector3.one * .065f, ink);
                Part(root, "Лапа" + side, shape, new Vector3(side * .15f, .07f, -.05f), new Vector3(.18f, .14f, .25f), ink);
                if (!robot)
                {
                    var puppy = item.Id == "pet-puppy";
                    var ear = Part(root, "Ухо" + side, puppy ? PrimitiveType.Sphere : PrimitiveType.Cube,
                        new Vector3(side * .25f, puppy ? .56f : .84f, 0), puppy ? new Vector3(.16f, .38f, .18f) : new Vector3(.2f, .2f, .16f), coat);
                    if (!puppy) ear.localRotation = Quaternion.Euler(0, 0, 45);
                }
            }
            if (robot)
            {
                Rod(root, new Vector3(0, .76f, 0), new Vector3(0, .95f, 0), .035f, ink);
                Part(root, "Антенна", PrimitiveType.Sphere, new Vector3(0, .96f, 0), Vector3.one * .11f, cream);
            }
            else Part(root, "Нос", PrimitiveType.Sphere, new Vector3(0, .51f, -.28f), Vector3.one * .09f, ink);
        }

        /// <summary>Добавляет деталь без столкновений: эти модели служат только для просмотра.</summary>
        /// <param name="root">Родитель детали.</param>
        /// <param name="name">Понятное имя детали.</param>
        /// <param name="shape">Форма простой фигуры.</param>
        /// <param name="position">Положение внутри модели.</param>
        /// <param name="scale">Размер детали.</param>
        /// <param name="material">Сохранённый материал.</param>
        /// <returns>Точка положения и поворота детали.</returns>
        static Transform Part(GameObject root, string name, PrimitiveType shape, Vector3 position, Vector3 scale, Material material)
        {
            var part = GameObject.CreatePrimitive(shape); part.name = name; part.transform.SetParent(root.transform, false);
            part.transform.localPosition = position; part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(part.GetComponent<Collider>()); return part.transform;
        }

        /// <summary>Соединяет две точки тонким цилиндром, например стропой парашюта.</summary>
        /// <param name="root">Родитель цилиндра.</param>
        /// <param name="from">Первая точка.</param>
        /// <param name="to">Вторая точка.</param>
        /// <param name="width">Толщина.</param>
        /// <param name="material">Цвет цилиндра.</param>
        static void Rod(GameObject root, Vector3 from, Vector3 to, float width, Material material)
        {
            var rod = Part(root, "Стропа", PrimitiveType.Cylinder, (from + to) * .5f, new Vector3(width, (to - from).magnitude * .5f, width), material);
            rod.localRotation = Quaternion.FromToRotation(Vector3.up, to - from);
        }
    }
}
