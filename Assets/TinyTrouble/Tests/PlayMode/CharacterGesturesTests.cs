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
            Assert.That(upper.localPosition.y - before.y, Is.EqualTo(.012f).Within(.0001f));
            Assert.That(Vector3.Distance(Part("RightSole").position, foot), Is.LessThan(.0001f));
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
