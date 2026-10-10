using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TinyTrouble.Tests
{
    /// <summary>Проверяет реальные сообщения меню и приоритеты движений героя.</summary>
    public class CharacterLifeTests : CharacterTestRig
    {
        /// <summary>Сохраняет работающий Life; синхронные тесты сами передают ему время.</summary>
        protected override bool FreezeLife => false;
        /// <summary>Наличие пользовательских настроек до теста.</summary>
        bool hadColor, hadVolume;
        /// <summary>Исходный цвет одежды.</summary>
        int oldColor;
        /// <summary>Исходная громкость настройки и звука.</summary>
        float oldVolume, oldAudio;

        /// <summary>Запоминает настройки, чтобы тесты цвета не меняли выбор пользователя.</summary>
        [SetUp] public void RememberPreferences()
        {
            hadColor = PlayerPrefs.HasKey("TinyTrouble.HoodieColor"); oldColor = PlayerPrefs.GetInt("TinyTrouble.HoodieColor");
            hadVolume = PlayerPrefs.HasKey("TinyTrouble.Volume"); oldVolume = PlayerPrefs.GetFloat("TinyTrouble.Volume");
            oldAudio = AudioListener.volume;
        }

        /// <summary>Восстанавливает пользовательские настройки после проверки.</summary>
        [TearDown] public void RestorePreferences()
        {
            if (hadColor) PlayerPrefs.SetInt("TinyTrouble.HoodieColor", oldColor); else PlayerPrefs.DeleteKey("TinyTrouble.HoodieColor");
            if (hadVolume) PlayerPrefs.SetFloat("TinyTrouble.Volume", oldVolume); else PlayerPrefs.DeleteKey("TinyTrouble.Volume");
            PlayerPrefs.Save(); AudioListener.volume = oldAudio;
        }

        /// <summary>Получает текущий жест из настоящего компонента.</summary>
        /// <param name="life">Компонент выбора жеста.</param>
        /// <returns>Название жеста.</returns>
        static string Gesture(Component life) => life.GetType().GetProperty("CurrentGesture").GetValue(life).ToString();

        /// <summary>Продвигает время маленькими шагами через рабочий метод Life.</summary>
        /// <param name="life">Компонент героя.</param>
        /// <param name="seconds">Продолжительность проверки.</param>
        static void Advance(Component life, float seconds)
        {
            var method = life.GetType().GetMethod("Advance");
            for (var remaining = seconds; remaining > .0001f; remaining -= .01f)
                method.Invoke(life, new object[] { Mathf.Min(.01f, remaining), Vector2.one * .5f, true, Camera.main });
        }

        /// <summary>Создаёт событие левой кнопки; герой получает его через существующий Drag.</summary>
        /// <returns>Данные левой кнопки мыши.</returns>
        static PointerEventData Pointer() => new PointerEventData(EventSystem.current)
            { button = PointerEventData.InputButton.Left, pointerId = -1 };

        /// <summary>Приветствие выполняется при открытии, но не при переключении вкладок.</summary>
        [Test] public void GreetingOnceAndFreshIdlePause()
        {
            var life = Required("CharacterLife");
            Assert.That(Gesture(life), Is.EqualTo("Greeting"));
            var controller = UnityEngine.Object.FindObjectOfType<LobbyController>();
            for (var i = 0; i < 3; i++) controller.SelectTab(i);
            Advance(life, 2);
            Assert.That(Gesture(life), Is.EqualTo("None"));
            for (var i = 0; i < 3; i++) controller.SelectTab(i);
            Advance(life, 4);
            Assert.That(Gesture(life), Is.EqualTo("None"));
        }

        /// <summary>Случайные жесты используют все три варианта, паузы и не повторяются подряд.</summary>
        [Test] public void IdleGesturesHaveValidTimingAndNeverRepeat()
        {
            var life = Required("CharacterLife"); Advance(life, 2);
            var maximum = (float)life.GetType().GetField("MaximumIdleDelay", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public).GetRawConstantValue();
            var seen = new HashSet<string>(); var previousIdle = ""; var current = Gesture(life);
            var started = 0f; var ended = -.5f; var count = 0;
            for (var time = .01f; time < (maximum + 2) * 40 && count < 30; time += .01f)
            {
                Advance(life, .01f); var next = Gesture(life);
                if (next == current) continue;
                if (next != "None")
                {
                    Assert.That(new[] { "WeightShift", "LookAround", "Wave" }, Does.Contain(next));
                    Assert.That(next, Is.Not.EqualTo(previousIdle));
                    Assert.That(time - ended, Is.InRange(4.97f, maximum + .03f));
                    seen.Add(next); previousIdle = next; started = time;
                }
                else { Assert.That(time - started, Is.InRange(.98f, 2.02f)); ended = time; count++; }
                current = next;
            }
            Assert.That(count, Is.EqualTo(30));
            Assert.That(seen, Is.EquivalentTo(new[] { "WeightShift", "LookAround", "Wave" }));
        }

        /// <summary>Только фактическая смена цвета вызывает реакцию; запись цвета немедленная.</summary>
        [Test] public void ColorOnlyReactsToActualChange()
        {
            var life = Required("CharacterLife");
            Assert.That(Gesture(life), Is.EqualTo("Greeting"), "Восстановление цвета вызвало лишнюю реакцию");
            Advance(life, 2);
            var controller = UnityEngine.Object.FindObjectOfType<LobbyController>();
            controller.SetHoodieColor(LobbyPreferences.LoadHoodieColor());
            controller.SetVolume(.37f); controller.SelectTab(1); controller.SelectTab(2);
            Advance(life, .5f); Assert.That(Gesture(life), Is.EqualTo("None"));
            var chosen = (LobbyPreferences.LoadHoodieColor() + 1) % 4;
            controller.SetHoodieColor(chosen);
            Assert.That(LobbyPreferences.LoadHoodieColor(), Is.EqualTo(chosen));
            Advance(life, .01f); Assert.That(Gesture(life), Is.EqualTo("ColorReaction"));
        }

        /// <summary>Последний цвет, выбранный во время вращения, даёт одну реакцию после отпускания.</summary>
        [Test] public void RapidColorsDuringDragProduceOnlyLatestReaction()
        {
            var life = Required("CharacterLife"); var drag = hero.GetComponent<CharacterDrag>(); var pointer = Pointer();
            drag.OnPointerDown(pointer);
            var controller = UnityEngine.Object.FindObjectOfType<LobbyController>();
            var original = LobbyPreferences.LoadHoodieColor();
            for (var i = 1; i <= 3; i++) controller.SetHoodieColor((original + i) % 4);
            Advance(life, 1); Assert.That(Gesture(life), Is.EqualTo("None"));
            drag.OnPointerUp(pointer); Advance(life, .01f);
            Assert.That(Gesture(life), Is.EqualTo("ColorReaction"));
            Advance(life, 1.57f); Assert.That(Gesture(life), Is.EqualTo("ColorReaction"));
            Advance(life, .04f); Assert.That(Gesture(life), Is.EqualTo("None"));
            Advance(life, 4); Assert.That(Gesture(life), Is.EqualTo("None"), "Появилась очередь повторных кивков");
        }

        /// <summary>Новые цвета плавно заменяют текущую реакцию и не образуют очередь.</summary>
        [Test] public void RapidColorsSmoothlyReplaceCurrentReaction()
        {
            var life = Required("CharacterLife"); Advance(life, 2);
            var controller = UnityEngine.Object.FindObjectOfType<LobbyController>();
            var original = LobbyPreferences.LoadHoodieColor();
            controller.SetHoodieColor((original + 1) % 4); Advance(life, .5f);
            var head = Part("HeadPivot"); var before = head.localRotation;
            controller.SetHoodieColor((original + 2) % 4); Advance(life, .01f);
            Assert.That(Quaternion.Angle(before, head.localRotation), Is.LessThan(3f));
            controller.SetHoodieColor((original + 3) % 4); Advance(life, 2.1f);
            Assert.That(Gesture(life), Is.EqualTo("None"));
            Advance(life, 4); Assert.That(Gesture(life), Is.EqualTo("None"));
        }

        /// <summary>Все пути завершения мыши снимают блокировку и не повторяют приветствие.</summary>
        /// <param name="reason">Способ завершения перетаскивания.</param>
        [TestCase("outside")] [TestCase("focus")] [TestCase("pause")] [TestCase("disable")]
        public void ReleaseOutsideFocusLossAndDisableClearDrag(string reason)
        {
            var life = Required("CharacterLife"); var drag = hero.GetComponent<CharacterDrag>(); var pointer = Pointer();
            drag.OnPointerDown(pointer); Advance(life, .3f);
            if (reason == "outside") { drag.OnEndDrag(pointer); drag.OnPointerUp(pointer); }
            if (reason == "focus") drag.SendMessage("OnApplicationFocus", false);
            if (reason == "pause") drag.SendMessage("OnApplicationPause", true);
            if (reason == "disable") drag.enabled = false;
            Assert.That((bool)life.GetType().GetProperty("IsDragging").GetValue(life), Is.False);
            Advance(life, 4.9f); Assert.That(Gesture(life), Is.EqualTo("None"));
        }

        /// <summary>Вращение прерывает крупный жест, но не дыхание и моргание.</summary>
        [Test] public void BreathingAndBlinkContinueDuringDrag()
        {
            var life = Required("CharacterLife"); hero.GetComponent<CharacterDrag>().OnPointerDown(Pointer());
            var eye = Part("EyeWhite-1"); var full = eye.localScale.y;
            var minimum = full; var before = Part("UpperBodyPivot").localPosition;
            for (var i = 0; i < 650; i++) { Advance(life, .01f); minimum = Mathf.Min(minimum, eye.localScale.y); }
            Assert.That(minimum, Is.LessThan(full * .1f));
            Assert.That(Vector3.Distance(before, Part("UpperBodyPivot").localPosition), Is.GreaterThan(.001f));
            Assert.That(Gesture(life), Is.EqualTo("None"));
        }

        /// <summary>Выбор цвета до первого Start не должен быть заменён приветствием.</summary>
        [Test] public void ColorChosenBeforeLifeStartKeepsPriority()
        {
            Required("CharacterLife");
            var clone = UnityEngine.Object.Instantiate(hero);
            try
            {
                var life = clone.GetComponent("CharacterLife");
                life.GetType().GetMethod("NotifyColorChanged").Invoke(life, null);
                Advance(life, .01f); Assert.That(Gesture(life), Is.EqualTo("ColorReaction"));
            }
            finally { UnityEngine.Object.DestroyImmediate(clone); }
        }

        /// <summary>Отключение возвращает позу; повторное включение не запускает приветствие.</summary>
        [Test] public void DisableRestoresPoseWithoutRepeatingGreeting()
        {
            var life = Required("CharacterLife"); Advance(life, .6f);
            hero.SetActive(false);
            Assert.That(Part("HeadPivot").localRotation, Is.EqualTo(Quaternion.identity));
            Assert.That(Part("RightArmPivot").localRotation, Is.EqualTo(Quaternion.identity));
            Assert.That(Part("UpperBodyPivot").localPosition.y, Is.EqualTo(.95f).Within(.0001f));
            hero.SetActive(true); Advance(life, .1f);
            Assert.That(Gesture(life), Is.EqualTo("None"));
        }

        /// <summary>Обычный Update продолжает дыхание даже при остановленном времени игры.</summary>
        /// <returns>Ожидание реального времени.</returns>
        [UnityTest] public IEnumerator PausedGameStillAnimates()
        {
            Required("CharacterLife"); Time.timeScale = 0;
            var before = Part("UpperBodyPivot").localPosition;
            yield return new WaitForSecondsRealtime(.3f);
            Assert.That(Vector3.Distance(before, Part("UpperBodyPivot").localPosition), Is.GreaterThan(.001f));
        }

        /// <summary>Повторная загрузка создаёт новое приветствие вместо сохранённого состояния.</summary>
        /// <returns>Ожидание загрузки нового лобби.</returns>
        [UnityTest] public IEnumerator ReloadStartsNewGreeting()
        {
            var life = Required("CharacterLife"); Advance(life, 2);
            yield return SceneManager.LoadSceneAsync("Lobby"); yield return null;
            hero = GameObject.Find("LobbyCharacter");
            Assert.That(Gesture(Required("CharacterLife")), Is.EqualTo("Greeting"));
        }
    }
}
