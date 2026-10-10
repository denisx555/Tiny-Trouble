using System.Linq;
using UnityEngine;

namespace TinyTrouble
{
    /// <summary>Подготавливает группы существующей модели, не пересоздавая сцену или детали.</summary>
    public static class CharacterRigBuilder
    {
        /// <summary>Создаёт недостающие группы, сохраняет внешний вид и подключает движения тела.</summary>
        /// <param name="hero">Существующая модель или открытый шаблон героя.</param>
        public static void Configure(GameObject hero)
        {
            var root = hero.transform;
            var upper = Pivot(root, root, "UpperBodyPivot", new Vector3(0, .95f, 0));
            var head = Pivot(root, upper, "HeadPivot", new Vector3(0, 1.72f, 0));
            var leftArm = Pivot(root, upper, "LeftArmPivot", new Vector3(-.44f, 1.55f, 0));
            var rightArm = Pivot(root, upper, "RightArmPivot", new Vector3(.44f, 1.55f, 0));
            var leftFoot = Pivot(root, root, "LeftFootPivot", new Vector3(-.25f, .07f, -.35f));
            var rightFoot = Pivot(root, root, "RightFootPivot", new Vector3(.25f, .07f, -.35f));
            // Сохранение мировой позы при смене родителя оставляет модель внешне прежней.
            foreach (var part in hero.GetComponentsInChildren<Renderer>(true))
            {
                var name = part.name;
                Transform parent;
                if (name == "LeftHand" || name == "HoodieLeftSleeve") parent = leftArm;
                else if (name == "RightHand" || name == "HoodieRightSleeve") parent = rightArm;
                else if (name.StartsWith("Left") && name != "LeftEar") parent = leftFoot;
                else if (name.StartsWith("Right") && name != "RightEar") parent = rightFoot;
                else if (name.StartsWith("Hoodie") || name.StartsWith("Drawstring") || name == "Neck") parent = upper;
                else parent = head;
                part.transform.SetParent(parent, true);
            }
            var gestures = hero.GetComponent<CharacterGestures>();
            if (!gestures) gestures = hero.AddComponent<CharacterGestures>();
            gestures.Configure(upper, leftArm, rightArm, leftFoot, rightFoot);
        }

        /// <summary>Находит существующую группу или создаёт её в заданной точке относительно корня.</summary>
        /// <param name="root">Корень модели.</param>
        /// <param name="parent">Родитель группы.</param>
        /// <param name="name">Сохраняемое имя группы.</param>
        /// <param name="position">Точка поворота в координатах корня.</param>
        /// <returns>Группа частей без изменения их внешнего вида.</returns>
        static Transform Pivot(Transform root, Transform parent, string name, Vector3 position)
        {
            var existing = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);
            if (existing) return existing;
            var pivot = new GameObject(name).transform;
            pivot.SetParent(root, false); pivot.localPosition = position;
            pivot.SetParent(parent, true);
            return pivot;
        }
    }
}
