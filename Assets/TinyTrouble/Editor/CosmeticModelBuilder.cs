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
            if (item.Category == CosmeticCategory.Pet) Pet(root, item, material, cream, ink);
            else CosmeticEquipmentBuilder.Build(root, item, material);
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

        /// <summary>Соединяет точки круглой антенной питомца.</summary>
        /// <param name="root">Корень питомца.</param>
        /// <param name="from">Начало антенны.</param>
        /// <param name="to">Конец антенны.</param>
        /// <param name="width">Толщина.</param>
        /// <param name="material">Цвет.</param>
        static void Rod(GameObject root, Vector3 from, Vector3 to, float width, Material material)
        {
            var rod = Part(root, "АнтеннаСтойка", PrimitiveType.Cylinder, (from + to) * .5f, new Vector3(width, (to - from).magnitude * .5f, width), material);
            rod.localRotation = Quaternion.FromToRotation(Vector3.up, to - from);
        }

    }
}
