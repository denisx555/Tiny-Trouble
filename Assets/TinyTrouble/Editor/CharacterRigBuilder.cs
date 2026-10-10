using System.Linq;
using UnityEngine;

namespace TinyTrouble
{
    /// <summary>Подготавливает группы существующей модели, не пересоздавая сцену или детали.</summary>
    public static class CharacterRigBuilder
    {
        /// <summary>Высота точки поворота плеча; меньшее число опускает её ниже.</summary>
        const float ShoulderHeight = 1.50f;

        /// <summary>Создаёт недостающие группы, сохраняет внешний вид и подключает движения тела.</summary>
        /// <param name="hero">Существующая модель или открытый шаблон героя.</param>
        public static void Configure(GameObject hero)
        {
            var root = hero.transform;
            var upper = Pivot(root, root, "UpperBodyPivot", new Vector3(0, .95f, 0));
            var head = Pivot(root, upper, "HeadPivot", new Vector3(0, 1.72f, 0));
            var leftArm = Pivot(root, upper, "LeftArmPivot", new Vector3(-.36f, ShoulderHeight, 0));
            var rightArm = Pivot(root, upper, "RightArmPivot", new Vector3(.36f, ShoulderHeight, 0));
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
                // Отдельные области нажатия двигаются вместе с рукавами и кистями при взмахе.
                if (parent == leftArm || parent == rightArm)
                {
                    var collider = part.GetComponent<BoxCollider>();
                    if (!collider) collider = part.gameObject.AddComponent<BoxCollider>();
                    var meshBounds = part.GetComponent<MeshFilter>().sharedMesh.bounds;
                    collider.center = meshBounds.center; collider.size = meshBounds.size;
                }
            }
            var gestures = hero.GetComponent<CharacterGestures>();
            if (!gestures) gestures = hero.AddComponent<CharacterGestures>();
            gestures.Configure(upper, leftArm, rightArm, leftFoot, rightFoot);
            var face = hero.GetComponent<CharacterFace>();
            if (!face) face = hero.AddComponent<CharacterFace>();
            var parts = hero.GetComponentsInChildren<Transform>(true);
            face.Configure(head,
                new[] { parts.FirstOrDefault(t => t.name == "EyeWhite-1"), parts.FirstOrDefault(t => t.name == "EyeWhite1") },
                new[] { parts.FirstOrDefault(t => t.name == "Pupil-1"), parts.FirstOrDefault(t => t.name == "Pupil1") },
                new[] { parts.FirstOrDefault(t => t.name == "EyeShine-1"), parts.FirstOrDefault(t => t.name == "EyeShine1") });
            var life = hero.GetComponent<CharacterLife>();
            if (!life) life = hero.AddComponent<CharacterLife>();
            life.Configure(gestures, face);
            gestures.SetPoseBounds(CalculatePoseBounds(hero));
        }

        /// <summary>На временной копии измеряет все допустимые позы, не двигая сохранённого героя.</summary>
        /// <param name="hero">Подготовленная модель с группами и компонентами.</param>
        /// <returns>Постоянные границы в координатах корня, с запасом 0,12 с каждой стороны.</returns>
        public static Bounds CalculatePoseBounds(GameObject hero)
        {
            var randomState = Random.state;
            var copy = Object.Instantiate(hero);
            var cameraObject = new GameObject("PoseBoundsCamera");
            try
            {
                copy.hideFlags = HideFlags.HideAndDontSave;
                copy.SetActive(false);
                copy.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                copy.transform.localScale = Vector3.one;
                cameraObject.hideFlags = HideFlags.HideAndDontSave;
                var camera = cameraObject.AddComponent<Camera>(); camera.enabled = false;
                camera.transform.position = new Vector3(0, 1, -5);
                var gestures = copy.GetComponent<CharacterGestures>(); var face = copy.GetComponent<CharacterFace>();
                var renderers = copy.GetComponentsInChildren<Renderer>(true);
                var bounds = new Bounds(Vector3.zero, Vector3.zero);
                foreach (CharacterGesture gesture in System.Enum.GetValues(typeof(CharacterGesture)))
                    for (var step = 0; step <= 40; step++)
                        foreach (var weight in new[] { 0f, .5f, 1f })
                            foreach (var breath in new[] { -Mathf.PI * .5f, 0f, Mathf.PI * .5f })
                                foreach (var pointer in new[] { Vector2.zero, Vector2.one, new Vector2(0, 1), new Vector2(1, 0) })
                                {
                                    if (gestures) gestures.ApplyPose(gesture, step / 40f, weight, breath);
                                    if (face) face.Animate(1, gesture, step / 40f, weight, true, pointer, true, camera);
                                    foreach (var renderer in renderers)
                                    {
                                        var area = renderer.bounds;
                                        for (var corner = 0; corner < 8; corner++)
                                        {
                                            var point = area.center + Vector3.Scale(area.extents, new Vector3(
                                                (corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1));
                                            bounds.Encapsulate(copy.transform.InverseTransformPoint(point));
                                        }
                                    }
                                }
                bounds.Expand(.24f);
                return bounds;
            }
            finally { Object.DestroyImmediate(copy); Object.DestroyImmediate(cameraObject); Random.state = randomState; }
        }

        /// <summary>Находит существующую группу или создаёт её в заданной точке относительно корня.</summary>
        /// <param name="root">Корень модели.</param>
        /// <param name="parent">Родитель группы.</param>
        /// <param name="name">Сохраняемое имя группы.</param>
        /// <param name="position">Точка поворота в координатах корня.</param>
        /// <returns>Группа частей без изменения их внешнего вида.</returns>
        /// <remarks>При переносе существующей точки сохраняет положение деталей, чтобы исходная модель не сдвигалась.</remarks>
        static Transform Pivot(Transform root, Transform parent, string name, Vector3 position)
        {
            var existing = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);
            if (existing)
            {
                // Переносим точку и компенсируем этот перенос у детей — частей руки или тела.
                var offset = root.TransformPoint(position) - existing.position;
                existing.position += offset;
                foreach (Transform child in existing) child.position -= offset;
                return existing;
            }
            var pivot = new GameObject(name).transform;
            pivot.SetParent(root, false); pivot.localPosition = position;
            pivot.SetParent(parent, true);
            return pivot;
        }
    }
}
