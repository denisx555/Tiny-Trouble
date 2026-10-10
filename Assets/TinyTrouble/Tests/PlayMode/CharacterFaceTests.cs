using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TinyTrouble.Tests
{
    /// <summary>Проверяет глаза и голову настоящей модели при управляемом времени.</summary>
    public class CharacterFaceTests : CharacterTestRig
    {
        /// <summary>Оба глаза закрываются вместе на короткое время и восстанавливают размер.</summary>
        [Test] public void BothEyesBlinkAndReturnToRest()
        {
            var face = Required("CharacterFace");
            var left = Part("EyeWhite-1"); var right = Part("EyeWhite1");
            var leftScale = left.localScale; var rightScale = right.localScale;
            var started = -1f; var finished = -1f; var smallest = 1f;
            for (var i = 1; i <= 650; i++)
            {
                Pose(face, "Animate", "None", .01f, 0f, 0f, false, Vector2.one * .5f, true, Camera.main);
                var closure = left.localScale.y / leftScale.y;
                Assert.That(closure, Is.EqualTo(right.localScale.y / rightScale.y).Within(.001f));
                smallest = Mathf.Min(smallest, closure);
                if (started < 0 && closure < .999f) started = i * .01f;
                if (started >= 0 && closure >= .999f) { finished = i * .01f; break; }
            }
            Assert.That(started, Is.InRange(3f, 6.02f));
            Assert.That(finished - started, Is.EqualTo(.16f).Within(.02f));
            Assert.That(smallest, Is.LessThan(.1f));
            Assert.That(left.localScale, Is.EqualTo(leftScale));
        }

        /// <summary>Указатель двигает глаза только при допустимом повороте героя.</summary>
        /// <param name="angle">Поворот лица относительно камеры.</param>
        /// <param name="tracks">Должны ли глаза следить за мышью.</param>
        [TestCase(0f, true)] [TestCase(60f, true)] [TestCase(61f, false)] [TestCase(180f, false)]
        public void GazeRespectsFacingAngle(float angle, bool tracks)
        {
            var face = Required("CharacterFace");
            var pupil = Part("Pupil-1"); var before = pupil.localPosition;
            var camera = Camera.main; var oldPosition = camera.transform.position;
            hero.transform.rotation = Quaternion.identity;
            try
            {
                camera.transform.position = hero.transform.position + Quaternion.Euler(0, angle, 0) * Vector3.back * 5;
                for (var i = 0; i < 20; i++) Pose(face, "Animate", "None", .05f, 0f, 0f, true, Vector2.one, true, camera);
                var offset = pupil.localPosition - before;
                Assert.That(Mathf.Abs(offset.x), Is.LessThanOrEqualTo(.0351f));
                Assert.That(Mathf.Abs(offset.y), Is.LessThanOrEqualTo(.0251f));
                Assert.That(Mathf.Abs(offset.x) > .01f, Is.EqualTo(tracks));
                if (tracks) Assert.That(Mathf.DeltaAngle(0, Part("HeadPivot").localEulerAngles.x), Is.GreaterThan(1f), "Мышь сверху должна поднимать взгляд головы");
                Assert.That(Quaternion.Angle(Quaternion.identity, Part("HeadPivot").localRotation), Is.LessThan(15f));
            }
            finally { camera.transform.position = oldPosition; }
        }

        /// <summary>Уход мыши, вращение и потеря фокуса возвращают глаза в центр.</summary>
        /// <param name="reason">Причина прекращения слежения.</param>
        [TestCase("focus")] [TestCase("outside")] [TestCase("drag")] [TestCase("camera")]
        public void GazeReturnsWhenUnavailable(string reason)
        {
            var face = Required("CharacterFace");
            var before = Part("Pupil-1").localPosition;
            for (var i = 0; i < 20; i++) Pose(face, "Animate", "None", .05f, 0f, 0f, true, Vector2.one, true, Camera.main);
            Assert.That(Vector3.Distance(before, Part("Pupil-1").localPosition), Is.GreaterThan(.01f));
            for (var i = 0; i < 20; i++) Pose(face, "Animate", "None", .05f, 0f, 0f,
                reason != "drag", reason == "outside" ? Vector2.one * 2 : Vector2.one,
                reason != "focus", reason == "camera" ? null : Camera.main);
            Assert.That(Vector3.Distance(before, Part("Pupil-1").localPosition), Is.LessThan(.001f));
        }

        /// <summary>Реакция на одежду опускает голову, кивает и полностью возвращается.</summary>
        [Test] public void ColorReactionMovesHeadThroughThreeStages()
        {
            var face = Required("CharacterFace"); var head = Part("HeadPivot"); var rest = head.localRotation;
            var hairOffset = head.InverseTransformPoint(Part("HairCap").position);
            Pose(face, "Animate", "ColorReaction", .01f, .5f / 1.6f, 1f, true, Vector2.one, true, Camera.main);
            var down = Mathf.DeltaAngle(0, head.localEulerAngles.x);
            Assert.That(down, Is.LessThan(-10f));
            Pose(face, "Animate", "ColorReaction", .01f, .6f / 1.6f, 1f, true, Vector2.one, true, Camera.main);
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(down, head.localEulerAngles.x)), Is.GreaterThan(1f));
            Pose(face, "Animate", "ColorReaction", .01f, 1f, 1f, true, Vector2.one, true, Camera.main);
            Assert.That(Quaternion.Angle(rest, head.localRotation), Is.LessThan(.1f));
            Assert.That(Vector3.Distance(hairOffset, head.InverseTransformPoint(Part("HairCap").position)), Is.LessThan(.0001f));
        }

        /// <summary>Оглядывание меняет голову, а не выбранный поворот корня.</summary>
        [Test] public void LookAroundDoesNotOverwriteRootYaw()
        {
            var face = Required("CharacterFace"); var rootRotation = hero.transform.rotation;
            Pose(face, "Animate", "LookAround", .01f, .25f, 1f, true, Vector2.one, true, Camera.main);
            Assert.That(Quaternion.Angle(Quaternion.identity, Part("HeadPivot").localRotation), Is.GreaterThan(10f));
            Assert.That(hero.transform.rotation, Is.EqualTo(rootRotation));
        }

        /// <summary>Неполная пара глаз пропускается без повторных предупреждений; голова работает.</summary>
        [Test] public void MissingEyeLogsOnceAndHeadStillMoves()
        {
            var face = Required("CharacterFace");
            LogAssert.Expect(LogType.Warning, new Regex("^CharacterFace:"));
            face.GetType().GetMethod("Configure").Invoke(face, new object[] { Part("HeadPivot"),
                new[] { Part("EyeWhite-1"), (Transform)null }, new[] { Part("Pupil-1"), Part("Pupil1") },
                new[] { Part("EyeShine-1"), Part("EyeShine1") } });
            for (var i = 0; i < 10; i++) Pose(face, "Animate", "LookAround", .01f, .25f, 1f, false, Vector2.zero, true, Camera.main);
            Assert.That(Quaternion.Angle(Quaternion.identity, Part("HeadPivot").localRotation), Is.GreaterThan(10f));
            LogAssert.NoUnexpectedReceived();
        }
    }
}
