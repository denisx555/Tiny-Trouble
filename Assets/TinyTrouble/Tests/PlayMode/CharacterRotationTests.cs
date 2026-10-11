using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TinyTrouble.Tests
{
    /// <summary>
    /// Проверяет поворот настоящего героя через события мыши в сохранённом лобби.
    /// </summary>
    public class CharacterRotationTests : CosmeticLobbyTestScope
    {
        /// <summary>
        /// Настройки громкости до теста, чтобы проверка ползунка не меняла выбор игрока.
        /// </summary>
        bool hadVolume;
        /// <summary>
        /// Исходная сохранённая громкость и громкость звука Unity.
        /// </summary>
        float oldVolume, oldListener;

        /// <summary>
        /// Запоминает громкость и открывает сохранённую сцену лобби.
        /// </summary>
        /// <returns>Ожидание загрузки сцены и первого кадра.</returns>
        [UnitySetUp] public IEnumerator Setup()
        {
            hadVolume = PlayerPrefs.HasKey("TinyTrouble.Volume");
            oldVolume = PlayerPrefs.GetFloat("TinyTrouble.Volume");
            oldListener = AudioListener.volume;
            yield return SceneManager.LoadSceneAsync("Lobby");
            yield return null;
        }

        /// <summary>
        /// Возвращает громкость игрока после проверки.
        /// </summary>
        [TearDown] public void Cleanup()
        {
            if (hadVolume) PlayerPrefs.SetFloat("TinyTrouble.Volume", oldVolume);
            else PlayerPrefs.DeleteKey("TinyTrouble.Volume");
            PlayerPrefs.Save();
            AudioListener.volume = oldListener;
        }

        /// <summary>
        /// Создаёт положение мыши в центре тела героя и проверяет путь события до него.
        /// </summary>
        /// <returns>Данные левой кнопки мыши над героем.</returns>
        static PointerEventData HeroPointer()
        {
            var hero = GameObject.Find("LobbyCharacter");
            Physics.SyncTransforms();
            var data = new PointerEventData(EventSystem.current)
            {
                position = Camera.main.WorldToScreenPoint(hero.transform.TransformPoint(new Vector3(0, 1.3f, 0))),
                button = PointerEventData.InputButton.Left,
                pointerId = -1
            };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(data, hits);
            Assert.That(hits.Count, Is.GreaterThan(0), "Персонаж ещё не принимает нажатие мыши");
            Assert.That(hits[0].gameObject.transform.IsChildOf(hero.transform), Is.True, "Мышь не попала в героя");
            Assert.That(ExecuteEvents.GetEventHandler<IDragHandler>(hits[0].gameObject), Is.EqualTo(hero), "Нет обработчика вращения героя");
            return data;
        }

        /// <summary>
        /// Передаёт герою начало нажатия и подготовку перетаскивания.
        /// </summary>
        /// <param name="data">Положение и кнопка мыши.</param>
        static void Press(PointerEventData data)
        {
            var hero = GameObject.Find("LobbyCharacter");
            ExecuteEvents.Execute(hero, data, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(hero, data, ExecuteEvents.initializePotentialDrag);
        }

        /// <summary>
        /// Передаёт движение мыши герою через обычное событие Unity.
        /// </summary>
        /// <param name="data">Данные текущего нажатия.</param>
        /// <param name="delta">Смещение мыши в пикселях экрана.</param>
        static void Drag(PointerEventData data, Vector2 delta)
        {
            data.delta = delta;
            data.position += delta;
            ExecuteEvents.Execute(GameObject.Find("LobbyCharacter"), data, ExecuteEvents.dragHandler);
        }

        /// <summary>
        /// Проверяет поворот в обе стороны и полный круг без сброса от покачивания.
        /// </summary>
        /// <returns>Ожидание кадров после движений мыши.</returns>
        [UnityTest] public IEnumerator LeftDragTurnsBothWaysAndAllowsFullCircle()
        {
            var data = HeroPointer();
            var hero = GameObject.Find("LobbyCharacter").transform;
            var initial = hero.eulerAngles.y;
            Press(data);
            Assert.That(data.useDragThreshold, Is.False, "Вращение должно начинаться без порога движения");
            Drag(data, Vector2.right * Screen.width * .25f);
            yield return null;
            Assert.That(Mathf.DeltaAngle(initial, hero.eulerAngles.y), Is.EqualTo(-90f).Within(4f));
            yield return new WaitForSecondsRealtime(.1f);
            Assert.That(Mathf.DeltaAngle(initial, hero.eulerAngles.y), Is.EqualTo(-90f).Within(4f), "Покачивание сбросило поворот");
            Drag(data, Vector2.left * Screen.width * .25f);
            yield return null;
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(initial, hero.eulerAngles.y)), Is.LessThan(4f));
            Drag(data, Vector2.right * Screen.width);
            yield return null;
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(initial, hero.eulerAngles.y)), Is.LessThan(4f));
        }

        /// <summary>
        /// Проверяет, что отпускание мыши прекращает вращение и сохраняет выбранный угол.
        /// </summary>
        /// <returns>Ожидание кадров с продолжающимся покачиванием.</returns>
        [UnityTest] public IEnumerator ReleaseStopsDragAndKeepsChosenAngle()
        {
            var data = HeroPointer();
            Press(data);
            Drag(data, Vector2.right * Screen.width * .25f);
            yield return null;
            var hero = GameObject.Find("LobbyCharacter").transform;
            var chosen = hero.eulerAngles.y;
            ExecuteEvents.Execute(hero.gameObject, data, ExecuteEvents.pointerUpHandler);
            Drag(data, Vector2.right * Screen.width * .25f);
            yield return new WaitForSecondsRealtime(.1f);
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(chosen, hero.eulerAngles.y)), Is.LessThan(4f));
        }

        /// <summary>
        /// Проверяет игнорирование правой кнопки и вертикального движения мыши.
        /// </summary>
        /// <returns>Ожидание кадров после неподходящего ввода.</returns>
        [UnityTest] public IEnumerator RightButtonAndVerticalMovementDoNotTurnHero()
        {
            var data = HeroPointer();
            var hero = GameObject.Find("LobbyCharacter").transform;
            var initial = hero.eulerAngles.y;
            data.button = PointerEventData.InputButton.Right;
            data.pointerId = -2;
            Press(data);
            Drag(data, Vector2.right * Screen.width * .25f);
            yield return null;
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(initial, hero.eulerAngles.y)), Is.LessThan(4f));
            data.button = PointerEventData.InputButton.Left;
            data.pointerId = -1;
            Press(data);
            Drag(data, Vector2.up * Screen.height * .25f);
            yield return null;
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(initial, hero.eulerAngles.y)), Is.LessThan(4f));
        }

        /// <summary>
        /// Проверяет вращение на всех вкладках и возврат к исходной позе после загрузки.
        /// </summary>
        /// <returns>Ожидание вкладок, движений и повторной загрузки лобби.</returns>
        [UnityTest] public IEnumerator EveryTabAllowsRotationAndReloadResetsPose()
        {
            var initial = GameObject.Find("LobbyCharacter").transform.eulerAngles.y;
            for (var tab = 0; tab < 3; tab++)
            {
                Object.FindObjectOfType<LobbyController>().SelectTab(tab);
                yield return null;
                var data = HeroPointer();
                var hero = GameObject.Find("LobbyCharacter").transform;
                var before = hero.eulerAngles.y;
                Press(data);
                Drag(data, Vector2.right * Screen.width * .25f);
                ExecuteEvents.Execute(hero.gameObject, data, ExecuteEvents.pointerUpHandler);
                yield return null;
                Assert.That(Mathf.DeltaAngle(before, hero.eulerAngles.y), Is.EqualTo(-90f).Within(4f));
            }
            yield return SceneManager.LoadSceneAsync("Lobby");
            yield return null;
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(initial, GameObject.Find("LobbyCharacter").transform.eulerAngles.y)), Is.LessThan(4f));
        }

        /// <summary>
        /// Проверяет, что перетаскивание громкости над героем не передаёт ему нажатие.
        /// </summary>
        /// <returns>Ожидание переключения вкладки и изменения ползунка.</returns>
        [UnityTest] public IEnumerator SliderDragDoesNotRotateHero()
        {
            Object.FindObjectOfType<LobbyController>().SelectTab(3);
            yield return null;
            var slider = Object.FindObjectsOfType<Slider>().Single();
            var hero = GameObject.Find("LobbyCharacter").transform;
            var initial = hero.eulerAngles.y;
            var data = HeroPointer();
            data.position = RectTransformUtility.WorldToScreenPoint(null, slider.transform.position);
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(data, hits);
            Assert.That(ExecuteEvents.GetEventHandler<IDragHandler>(hits[0].gameObject), Is.EqualTo(slider.gameObject));
            ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, data, ExecuteEvents.pointerDownHandler);
            data.position = HeroPointer().position;
            data.delta = Vector2.left * Screen.width * .25f;
            ExecuteEvents.Execute(slider.gameObject, data, ExecuteEvents.dragHandler);
            ExecuteEvents.Execute(slider.gameObject, data, ExecuteEvents.pointerUpHandler);
            yield return null;
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(initial, hero.eulerAngles.y)), Is.LessThan(4f));
            Assert.That(AudioListener.volume, Is.EqualTo(slider.value).Within(.001f));
        }

        /// <summary>Нажатие на вытянутую кисть настоящим лучом начинает вращение спереди и сзади.</summary>
        /// <returns>Кадры после движения мыши, чтобы проверить поворот корня.</returns>
        [UnityTest] public IEnumerator WavingHandReceivesDragThroughRaycast()
        {
            var hero = GameObject.Find("LobbyCharacter");
            var idle = hero.GetComponent<CharacterIdle>();
            hero.GetComponent<CharacterLife>().enabled = false;
            var hand = hero.GetComponentsInChildren<Transform>().Single(t => t.name == "RightHand");
            foreach (var angle in new[] { 0f, 180f })
                foreach (var gesture in new[] { CharacterGesture.Wave, CharacterGesture.Greeting })
                {
                    idle.enabled = false;
                    hero.transform.rotation = Quaternion.Euler(0, angle, 0);
                    hero.GetComponent<CharacterGestures>().ApplyPose(gesture, .5f, 1, Mathf.PI * .5f);
                    Physics.SyncTransforms();
                    var data = new PointerEventData(EventSystem.current)
                    {
                        position = Camera.main.WorldToScreenPoint(hand.position),
                        button = PointerEventData.InputButton.Left, pointerId = -1
                    };
                    var hits = new List<RaycastResult>();
                    EventSystem.current.RaycastAll(data, hits);
                    Assert.That(hits.Count, Is.GreaterThan(0), "Машущая кисть не принимает нажатие");
                    var handler = ExecuteEvents.GetEventHandler<IDragHandler>(hits[0].gameObject);
                    Assert.That(handler, Is.EqualTo(hero), "Нажатие на кисть не дошло до вращения героя");
                    ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, data, ExecuteEvents.pointerDownHandler);
                    Assert.That(hero.GetComponent<CharacterLife>().IsDragging, Is.True);
                    idle.enabled = true;
                    var before = hero.transform.eulerAngles.y;
                    data.delta = Vector2.right * Screen.width * .25f;
                    ExecuteEvents.Execute(handler, data, ExecuteEvents.dragHandler);
                    yield return null;
                    Assert.That(Mathf.Abs(Mathf.DeltaAngle(before, hero.transform.eulerAngles.y)), Is.GreaterThan(30f));
                    ExecuteEvents.Execute(handler, data, ExecuteEvents.pointerUpHandler);
                }
        }

        /// <summary>
        /// Проверяет прекращение жеста при потере фокуса окна.
        /// </summary>
        /// <returns>Ожидание кадров до и после потери фокуса.</returns>
        [UnityTest] public IEnumerator LosingFocusCancelsActiveDrag()
        {
            var data = HeroPointer();
            var hero = GameObject.Find("LobbyCharacter");
            Press(data);
            Drag(data, Vector2.right * Screen.width * .25f);
            yield return null;
            var chosen = hero.transform.eulerAngles.y;
            hero.SendMessage("OnApplicationFocus", false);
            Drag(data, Vector2.right * Screen.width * .25f);
            yield return null;
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(chosen, hero.transform.eulerAngles.y)), Is.LessThan(4f));
        }
    }
}
