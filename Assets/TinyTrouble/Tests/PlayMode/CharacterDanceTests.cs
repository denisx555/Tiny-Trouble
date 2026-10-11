using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TinyTrouble.Tests
{
    /// <summary>Проверяет настоящие позы танцев, приоритет действий и постоянную область движений.</summary>
    public class CharacterDanceTests : CharacterTestRig
    {
        /// <summary>Подключает показ танца к сохранённому управлению движениями.</summary>
        /// <returns>Компонент просмотра танца.</returns>
        CharacterDance Dance()
        {
            var dance = hero.GetComponent<CharacterDance>();
            if (!dance) dance = hero.AddComponent<CharacterDance>();
            dance.Configure(hero.GetComponent<CharacterLife>()); return dance;
        }
        /// <summary>Продвигает движение маленькими шагами, не ожидая настоящих секунд.</summary>
        /// <param name="seconds">Количество секунд.</param>
        /// <param name="focus">Получает ли игра ввод.</param>
        void Advance(float seconds, bool focus = true)
        {
            for (var left = seconds; left > .0001f; left -= .01f)
                hero.GetComponent<CharacterLife>().Advance(Mathf.Min(.01f, left), Vector2.one * .5f, focus, Camera.main);
        }
        /// <summary>Каждый танец меняет позу во времени, а кисть остаётся связана с рукавом.</summary>
        [Test] public void ThreeDancesHaveDistinctMovingPoses()
        {
            var dance = Dance(); var poses = new Quaternion[3]; var ids = new[] { "robot", "spring", "victory" };
            var sleeve = Part("HoodieRightSleeve"); var hand = Part("RightHand");
            var relative = sleeve.InverseTransformPoint(hand.position);
            for (var i = 0; i < ids.Length; i++)
            {
                Assert.That(dance.Preview(ids[i]), Is.True); Advance(.75f);
                poses[i] = Part("RightArmPivot").localRotation;
                Assert.That(Vector3.Distance(sleeve.InverseTransformPoint(hand.position), relative), Is.LessThan(.0001f));
                Advance(.4f); Assert.That(Quaternion.Angle(poses[i], Part("RightArmPivot").localRotation), Is.GreaterThan(1));
                dance.Stop(); Advance(.21f);
            }
            Assert.That(Quaternion.Angle(poses[0], poses[1]), Is.GreaterThan(5));
            Assert.That(Quaternion.Angle(poses[1], poses[2]), Is.GreaterThan(5));
        }
        /// <summary>При танце герой продолжает дышать и моргать обоими глазами.</summary>
        [Test] public void DanceRetainsBreathingBlink()
        {
            Assert.That(Dance().Preview("robot"), Is.True);
            var full = Part("EyeWhite1").localScale.y; var minimum = full;
            var before = Part("UpperBodyPivot").localPosition.y; var largest = 0f;
            for (var i = 0; i < 650; i++)
            { Advance(.01f); minimum = Mathf.Min(minimum, Part("EyeWhite1").localScale.y); largest = Mathf.Max(largest, Mathf.Abs(Part("UpperBodyPivot").localPosition.y - before)); Assert.That(Part("EyeWhite1").localScale.y, Is.EqualTo(Part("EyeWhite-1").localScale.y).Within(.0001f)); }
            Assert.That(minimum, Is.LessThan(full * .1f)); Assert.That(largest, Is.GreaterThan(.015f));
            Assert.That(hero.GetComponent<CharacterLife>().CurrentGesture, Is.EqualTo(CharacterGesture.DanceRobot));
        }
        /// <summary>Вращение, цвет, уход фокуса и остановка прекращают цикл без самоповтора.</summary>
        [Test] public void DragColorFocusLossAndStopInterruptDance()
        {
            var dance = Dance(); var life = hero.GetComponent<CharacterLife>();
            Assert.That(dance.Preview("robot"), Is.True); Advance(.6f); dance.Stop(); Advance(.21f);
            Assert.That(life.CurrentGesture, Is.EqualTo(CharacterGesture.None));
            Assert.That(dance.Preview("spring"), Is.True); Advance(.6f); life.SetDragging(true); Advance(.21f);
            Assert.That(life.CurrentGesture, Is.EqualTo(CharacterGesture.None)); Assert.That(dance.Preview("victory"), Is.False);
            life.SetDragging(false); Assert.That(dance.Preview("victory"), Is.True); Advance(.6f); life.NotifyColorChanged(); Advance(.21f);
            Assert.That(life.CurrentGesture, Is.EqualTo(CharacterGesture.ColorReaction)); Assert.That(dance.Preview("robot"), Is.False);
            Advance(1.7f); Assert.That(dance.Preview("robot"), Is.True); Advance(.6f); Advance(.21f, false);
            Assert.That(life.CurrentGesture, Is.EqualTo(CharacterGesture.None)); Advance(4);
            Assert.That(life.CurrentGesture, Is.EqualTo(CharacterGesture.None)); Advance(6);
            Assert.That(life.CurrentGesture, Is.Not.EqualTo(CharacterGesture.DanceRobot));
        }
        /// <summary>Границы сохранённого героя охватывают движения без изменения каждый кадр.</summary>
        [Test] public void DanceBoundsEnclosePoseSamples()
        {
            var gestures = hero.GetComponent<CharacterGestures>(); var bounds = gestures.PoseBounds;
            foreach (var id in new[] { "robot", "spring", "victory" })
            {
                Assert.That(Dance().Preview(id), Is.True);
                for (var i = 0; i < 40; i++)
                {
                    Advance(.075f);
                    foreach (var surface in hero.GetComponentsInChildren<Renderer>())
                        for (var corner = 0; corner < 8; corner++)
                        {
                            var area = surface.bounds;
                            var point = hero.transform.InverseTransformPoint(area.center + Vector3.Scale(area.extents, new Vector3((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1)));
                            Assert.That(bounds.Contains(point), Is.True, id + " " + surface.name);
                        }
                }
                Dance().Stop(); Advance(.21f);
            }
            Assert.That(gestures.PoseBounds, Is.EqualTo(bounds));
        }
    }
}
