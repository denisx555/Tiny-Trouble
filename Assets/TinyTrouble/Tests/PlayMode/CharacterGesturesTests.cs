using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TinyTrouble.Tests
{
    /// <summary>Проверяет дыхание и согласованное движение частей тела.</summary>
    public class CharacterGesturesTests : CharacterTestRig
    {
        /// <summary>Дыхание изменяет верх тела, но не поднимает ступни.</summary>
        [Test] public void BreathingKeepsFeetOnPlatform()
        {
            var gestures = Required("CharacterGestures");
            var upper = Part("UpperBodyPivot");
            var before = upper.localPosition;
            var foot = Part("RightSole").position;
            Pose(gestures, "ApplyPose", "None", 0f, 0f, Mathf.PI * .5f);
            Assert.That(upper.localPosition.y - before.y, Is.InRange(.02f, .03f), "Подъём должен быть заметным с камеры лобби");
            Assert.That(Vector3.Distance(Part("RightSole").position, foot), Is.LessThan(.0001f));
        }

        /// <summary>Вдох и выдох заметно меняют силуэт туловища, а сброс возвращает исходный вид.</summary>
        [Test] public void BreathingChangesTorsoSilhouetteAndReturnsToRest()
        {
            var gestures = Required("CharacterGestures");
            var body = Part("HoodieBody").GetComponent<Renderer>();
            var rest = body.bounds.size;
            Pose(gestures, "ApplyPose", "None", 0f, 0f, -Mathf.PI * .5f);
            var exhale = body.bounds.size;
            Pose(gestures, "ApplyPose", "None", 0f, 0f, Mathf.PI * .5f);
            var inhale = body.bounds.size;
            Assert.That((inhale.x - exhale.x) / rest.x, Is.GreaterThan(.04f), "Ширина туловища меняется слишком слабо");
            Assert.That((inhale.z - exhale.z) / rest.z, Is.GreaterThan(.04f), "Дыхание сбоку тоже должно быть заметным");
            gestures.GetType().GetMethod("ResetPose").Invoke(gestures, null);
            Assert.That(Vector3.Distance(body.bounds.size, rest), Is.LessThan(.0001f));
        }

        /// <summary>Кисть остаётся связанной с рукавом при взмахе и после сброса.</summary>
        [Test] public void WaveKeepsHandAndSleeveTogether()
        {
            var gestures = Required("CharacterGestures");
            var arm = Part("RightArmPivot");
            var sleeve = Part("HoodieRightSleeve");
            var hand = Part("RightHand");
            var relative = arm.InverseTransformPoint(hand.position);
            var sleeveRelative = sleeve.localPosition;
            var rest = arm.localRotation;
            Pose(gestures, "ApplyPose", "Wave", .5f, 1f, 0f);
            Assert.That(Quaternion.Angle(rest, arm.localRotation), Is.GreaterThan(30f));
            Assert.That(Vector3.Distance(relative, arm.InverseTransformPoint(hand.position)), Is.LessThan(.0001f));
            Assert.That(sleeve.localPosition, Is.EqualTo(sleeveRelative));
            gestures.GetType().GetMethod("ResetPose").Invoke(gestures, null);
            Assert.That(Quaternion.Angle(rest, arm.localRotation), Is.LessThan(.01f));
        }

        /// <summary>Рукав остаётся немного внутри туловища во всех фазах взмаха, без щели у плеча.</summary>
        /// <param name="gesture">Взмах при приветствии или случайном жесте.</param>
        [TestCase("Wave")] [TestCase("Greeting")]
        public void WavingSleeveStaysConnectedToTorso(string gesture)
        {
            var gestures = Required("CharacterGestures");
            var sleeve = Part("HoodieRightSleeve");
            var body = Part("HoodieBody");
            var vertices = sleeve.GetComponent<MeshFilter>().sharedMesh.vertices;
            for (var step = 0; step <= 100; step++)
                foreach (var weight in new[] { .5f, 1f })
                    foreach (var breath in new[] { -Mathf.PI * .5f, Mathf.PI * .5f })
                    {
                        Pose(gestures, "ApplyPose", gesture, step / 100f, weight, breath);
                        var nearest = float.MaxValue;
                        foreach (var vertex in vertices)
                        {
                            // Туловище — растянутая сфера: в его местных координатах радиус равен 0,5.
                            var point = body.InverseTransformPoint(sleeve.TransformPoint(vertex));
                            nearest = Mathf.Min(nearest, point.magnitude);
                        }
                        Assert.That(nearest, Is.LessThan(.48f),
                            gesture + " фаза " + step / 100f + ": рукав не входит в туловище с небольшим запасом");
                    }
        }

        /// <summary>Переминание приподнимает пятки по очереди, сохраняя точку носка.</summary>
        [Test] public void WeightShiftAlternatesFeetWithoutMovingToePivots()
        {
            var gestures = Required("CharacterGestures");
            var left = Part("LeftFootPivot"); var right = Part("RightFootPivot");
            var position = left.position; var rest = left.localRotation;
            Pose(gestures, "ApplyPose", "WeightShift", .25f, 1f, 0f);
            Assert.That(Quaternion.Angle(rest, left.localRotation), Is.GreaterThan(1f));
            Assert.That(Quaternion.Angle(rest, right.localRotation), Is.LessThan(.01f));
            Assert.That(Vector3.Distance(left.position, position), Is.LessThan(.0001f));
            Pose(gestures, "ApplyPose", "WeightShift", .75f, 1f, 0f);
            Assert.That(Quaternion.Angle(rest, right.localRotation), Is.GreaterThan(1f));
        }

        /// <summary>Отсутствующая рука не останавливает дыхание и не засоряет консоль.</summary>
        [Test] public void MissingArmDoesNotStopBreathing()
        {
            var gestures = Required("CharacterGestures");
            LogAssert.Expect(LogType.Warning, new Regex("^CharacterGestures:"));
            gestures.GetType().GetMethod("Configure").Invoke(gestures, new object[] {
                Part("UpperBodyPivot"), Part("LeftArmPivot"), null, Part("LeftFootPivot"), Part("RightFootPivot") });
            var before = Part("UpperBodyPivot").localPosition;
            for (var i = 0; i < 10; i++) Pose(gestures, "ApplyPose", "Wave", .5f, 1f, Mathf.PI * .5f);
            Assert.That(Part("UpperBodyPivot").localPosition.y, Is.GreaterThan(before.y));
            LogAssert.NoUnexpectedReceived();
        }
    }
}
