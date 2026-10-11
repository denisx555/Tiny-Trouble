using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace TinyTrouble.Tests
{
    /// <summary>Проверяет сохранённые группы героя и безопасную повторную подготовку модели.</summary>
    public class CharacterRigTests
    {
        /// <summary>Путь к существующему шаблону героя.</summary>
        const string PrefabPath = "Assets/TinyTrouble/Prefabs/LobbyCharacter.prefab";

        /// <summary>Находит инструмент подготовки; отсутствие инструмента объясняет красный тест.</summary>
        /// <returns>Метод настройки героя.</returns>
        static System.Reflection.MethodInfo ConfigureMethod()
        {
            var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("TinyTrouble.CharacterRigBuilder"))
                .FirstOrDefault(t => t != null);
            Assert.That(type, Is.Not.Null, "Подготовка групп героя ещё не реализована");
            return type.GetMethod("Configure");
        }

        /// <summary>Сохранённый герой должен иметь группы и ссылки, иначе части нельзя двигать вместе.</summary>
        [Test] public void SavedRigHasRequiredGroupsAndReferences()
        {
            var hero = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var gestures = hero.GetComponent("CharacterGestures");
            Assert.That(gestures, Is.Not.Null, "Герой ещё не получает позы тела");
            var so = new SerializedObject(gestures);
            foreach (var field in new[] { "upperBody", "leftArm", "rightArm", "leftFoot", "rightFoot" })
                Assert.That(so.FindProperty(field).objectReferenceValue, Is.Not.Null, field);
            foreach (var name in new[] { "UpperBodyPivot", "HeadPivot", "LeftArmPivot", "RightArmPivot", "LeftFootPivot", "RightFootPivot" })
                Assert.That(hero.GetComponentsInChildren<Transform>(true).Count(t => t.name == name), Is.EqualTo(1), name);
            var face = hero.GetComponent("CharacterFace");
            Assert.That(face, Is.Not.Null);
            var faceData = new SerializedObject(face);
            Assert.That(faceData.FindProperty("headPivot").objectReferenceValue, Is.Not.Null);
            foreach (var field in new[] { "eyeWhites", "pupils", "eyeShines" })
            {
                var refs = faceData.FindProperty(field);
                Assert.That(refs.arraySize, Is.EqualTo(2));
                for (var i = 0; i < 2; i++) Assert.That(refs.GetArrayElementAtIndex(i).objectReferenceValue, Is.Not.Null, field);
            }
            var life = hero.GetComponent("CharacterLife");
            Assert.That(life, Is.Not.Null);
            var lifeData = new SerializedObject(life);
            foreach (var field in new[] { "gestures", "face" })
                Assert.That(lifeData.FindProperty(field).objectReferenceValue, Is.Not.Null, field);
            var dance = hero.GetComponent<CharacterDance>();
            Assert.That(dance, Is.Not.Null);
            Assert.That(new SerializedObject(dance).FindProperty("life").objectReferenceValue, Is.EqualTo(life));
        }

        /// <summary>Две настройки подряд не должны сдвигать детали или создавать дубликаты групп.</summary>
        [Test] public void ConfigureTwicePreservesRestAppearance()
        {
            var configure = ConfigureMethod();
            var hero = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
            try
            {
                var parts = hero.GetComponentsInChildren<Renderer>();
                var positions = parts.Select(p => p.transform.position).ToArray();
                var rotations = parts.Select(p => p.transform.rotation).ToArray();
                var scales = parts.Select(p => p.transform.lossyScale).ToArray();
                configure.Invoke(null, new object[] { hero });
                configure.Invoke(null, new object[] { hero });
                for (var i = 0; i < parts.Length; i++)
                {
                    Assert.That(Vector3.Distance(parts[i].transform.position, positions[i]), Is.LessThan(.0001f), parts[i].name);
                    Assert.That(Quaternion.Angle(parts[i].transform.rotation, rotations[i]), Is.LessThan(.01f), parts[i].name);
                    Assert.That(Vector3.Distance(parts[i].transform.lossyScale, scales[i]), Is.LessThan(.0001f), parts[i].name);
                }
                Assert.That(hero.GetComponentsInChildren<Transform>().Where(t => t.name.EndsWith("Pivot"))
                    .GroupBy(t => t.name).Any(g => g.Count() > 1), Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(hero); }
        }

        /// <summary>Сохранённая область охватывает крайние и промежуточные позы, включая дыхание.</summary>
        [Test] public void PoseBoundsContainsMotionExtremes()
        {
            var hero = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
            var randomState = UnityEngine.Random.state;
            try
            {
                ConfigureMethod().Invoke(null, new object[] { hero });
                var gestures = hero.GetComponent<CharacterGestures>(); var face = hero.GetComponent<CharacterFace>();
                var saved = gestures.PoseBounds;
                Assert.That(saved.size.sqrMagnitude, Is.GreaterThan(1), "Область всех поз ещё не рассчитана");
                foreach (CharacterGesture gesture in Enum.GetValues(typeof(CharacterGesture)))
                    for (var phase = 0; phase <= 50; phase++)
                        foreach (var weight in new[] { 0f, .5f, 1f })
                            foreach (var breath in new[] { -Mathf.PI * .5f, Mathf.PI * .5f })
                            {
                                var progress = Mathf.Min(1, phase / 50f + .003f);
                                gestures.ApplyPose(gesture, progress, weight, breath);
                                face.Animate(1, gesture, progress, weight, false, Vector2.zero, false, null);
                                foreach (var renderer in hero.GetComponentsInChildren<Renderer>())
                                    for (var corner = 0; corner < 8; corner++)
                                    {
                                        var b = renderer.bounds;
                                        var point = hero.transform.InverseTransformPoint(b.center + Vector3.Scale(b.extents,
                                            new Vector3((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1)));
                                        Assert.That(saved.Contains(point), Is.True, gesture + " " + renderer.name + " " + progress);
                                    }
                            }
                Assert.That(gestures.PoseBounds, Is.EqualTo(saved));
            }
            finally { UnityEngine.Object.DestroyImmediate(hero); UnityEngine.Random.state = randomState; }
        }
    }
}
