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
    /// <summary>Проверяет приближение настоящего героя через события колёсика в сохранённом лобби.</summary>
    public class CharacterZoomTests : CharacterTestRig
    {
        /// <summary>Находит первое попадание мыши и передаёт ему обычное событие колёсика Unity.</summary>
        /// <param name="position">Место указателя в пикселях.</param>
        /// <param name="steps">Направление и величина прокрутки.</param>
        /// <returns>Первый объект под указателем.</returns>
        static GameObject Scroll(Vector2 position, float steps)
        {
            Physics.SyncTransforms();
            var data = new PointerEventData(EventSystem.current) { position = position, scrollDelta = Vector2.up * steps, pointerId = -1 };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(data, hits);
            Assert.That(hits.Count, Is.GreaterThan(0), "Указатель не попал в объект");
            ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, data, ExecuteEvents.scrollHandler);
            return hits[0].gameObject;
        }

        /// <summary>Получает указатель над центром головы, который остаётся виден крупным планом.</summary>
        /// <returns>Место головы на экране.</returns>
        Vector2 HeadPointer() => Camera.main.WorldToScreenPoint(Part("Head").position);

        /// <summary>Считает видимую высоту головы по границам её объёмной формы.</summary>
        /// <returns>Высота головы в пикселях.</returns>
        float HeadHeight()
        {
            var bounds = Part("Head").GetComponent<Renderer>().bounds;
            var points = Enumerable.Range(0, 8).Select(i => Camera.main.WorldToScreenPoint(bounds.center + Vector3.Scale(bounds.extents,
                new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1)))).ToArray();
            return points.Max(p => p.y) - points.Min(p => p.y);
        }

        /// <summary>Прокрутка вверх плавно приближает, обратный шаг возвращает прежний вид.</summary>
        /// <returns>Ожидание движения камеры.</returns>
        [UnityTest] public IEnumerator WheelMovesCameraSmoothlyInBothDirections()
        {
            var camera = Camera.main;
            var before = camera.transform.position;
            Assert.That(Scroll(HeadPointer(), 1).transform.IsChildOf(hero.transform), Is.True);
            Assert.That(Vector3.Distance(camera.transform.position, before), Is.LessThan(.0001f), "Камера перескочила без плавного перехода");
            yield return new WaitForSecondsRealtime(.5f);
            Assert.That(camera.transform.position.z, Is.GreaterThan(before.z + .05f), "Колёсико вверх не приблизило героя");
            Scroll(HeadPointer(), -1);
            yield return new WaitForSecondsRealtime(.7f);
            Assert.That(Vector3.Distance(camera.transform.position, before), Is.LessThan(.025f), "Обратный шаг не вернул исходный вид");
        }

        /// <summary>Крупный план делает лицо заметно больше, сохраняя его на экране и камеру снаружи модели.</summary>
        /// <returns>Ожидание предельного приближения.</returns>
        [UnityTest] public IEnumerator CloseViewShowsFaceAndKeepsCameraOutsideModel()
        {
            var restHeight = HeadHeight();
            Scroll(HeadPointer(), 1000);
            yield return new WaitForSecondsRealtime(.8f);
            Assert.That(HeadHeight(), Is.GreaterThan(restHeight * 2), "Лицо не стало крупным планом");
            var head = Camera.main.WorldToViewportPoint(Part("Head").position);
            Assert.That(head.x, Is.InRange(.05f, .95f));
            Assert.That(head.y, Is.InRange(.1f, .9f), "Камера не направлена на лицо");
            var points = Part("HeadPivot").GetComponentsInChildren<Renderer>().SelectMany(renderer =>
                Enumerable.Range(0, 8).Select(i => Camera.main.WorldToScreenPoint(renderer.bounds.center + Vector3.Scale(renderer.bounds.extents,
                    new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1))))).ToArray();
            var headRect = Rect.MinMaxRect(points.Min(p => p.x), points.Min(p => p.y), points.Max(p => p.x), points.Max(p => p.y));
            var tagCorners = new Vector3[4]; GameObject.Find("PlayerTag").GetComponent<RectTransform>().GetWorldCorners(tagCorners);
            var tagRect = Rect.MinMaxRect(tagCorners[0].x, tagCorners[0].y, tagCorners[2].x, tagCorners[2].y);
            Assert.That(headRect.Overlaps(tagRect), Is.False, "Плашка имени перекрывает голову крупным планом");
            foreach (var renderer in hero.GetComponentsInChildren<Renderer>())
            {
                var bounds = renderer.bounds;
                for (var i = 0; i < 8; i++)
                {
                    var corner = bounds.center + Vector3.Scale(bounds.extents,
                        new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    Assert.That(Camera.main.WorldToViewportPoint(corner).z, Is.GreaterThan(Camera.main.nearClipPlane + .05f), "Камера вошла внутрь модели");
                }
            }
            var close = Camera.main.transform.position;
            Scroll(HeadPointer(), 1000);
            yield return new WaitForSecondsRealtime(.5f);
            Assert.That(Vector3.Distance(Camera.main.transform.position, close), Is.LessThan(.01f), "Предел приближения не остановил камеру");
            Scroll(HeadPointer(), -1000);
            yield return new WaitForSecondsRealtime(.8f);
            var far = Camera.main.transform.position;
            Assert.That(far.z, Is.LessThan(close.z - 1), "Колёсико вниз не отдалило героя");
            Scroll(HeadPointer(), -1000);
            yield return new WaitForSecondsRealtime(.5f);
            Assert.That(Vector3.Distance(Camera.main.transform.position, far), Is.LessThan(.01f), "Предел отдаления не остановил камеру");
        }

        /// <summary>Колёсико над вкладкой и ползунком не приближает героя.</summary>
        /// <returns>Ожидание переключения вкладки и кадров после прокрутки.</returns>
        [UnityTest] public IEnumerator WheelOverControlsDoesNotMoveCamera()
        {
            var tab = GameObject.Find("TabSettings");
            var before = Camera.main.transform.position;
            var hit = Scroll(tab.GetComponent<RectTransform>().position, 5);
            Assert.That(hit.transform.IsChildOf(hero.transform), Is.False);
            yield return new WaitForSecondsRealtime(.4f);
            Assert.That(Vector3.Distance(Camera.main.transform.position, before), Is.LessThan(.0001f));
            tab.GetComponent<Button>().onClick.Invoke();
            yield return null;
            var slider = Object.FindObjectOfType<Slider>();
            Scroll(slider.GetComponent<RectTransform>().position, 5);
            yield return new WaitForSecondsRealtime(.4f);
            Assert.That(Vector3.Distance(Camera.main.transform.position, before), Is.LessThan(.0001f));
        }

        /// <summary>Приближение сохраняется во вкладках и при повторной раскладке, но сбрасывается при новом входе.</summary>
        /// <returns>Ожидание движения, переключений и загрузки новой сцены.</returns>
        [UnityTest] public IEnumerator ZoomSurvivesTabsAndRefitButResetsOnReentry()
        {
            var before = Camera.main.transform.position;
            Scroll(HeadPointer(), 5);
            yield return new WaitForSecondsRealtime(.8f);
            var close = Camera.main.transform.position;
            Assert.That(Vector3.Distance(close, before), Is.GreaterThan(.5f), "Приближение не применено");
            foreach (var name in new[] { "TabCharacter", "TabSettings", "TabPlay" })
            {
                GameObject.Find(name).GetComponent<Button>().onClick.Invoke();
                yield return null;
                Assert.That(Vector3.Distance(Camera.main.transform.position, close), Is.LessThan(.01f), "Вкладка сбросила приближение");
            }
            var layout = Object.FindObjectOfType<LobbyLayout>();
            layout.enabled = false; layout.enabled = true;
            yield return null;
            Assert.That(Vector3.Distance(Camera.main.transform.position, close), Is.LessThan(.01f), "Раскладка сбросила приближение");
            yield return SceneManager.LoadSceneAsync("Lobby");
            yield return null;
            Assert.That(Vector3.Distance(Camera.main.transform.position, before), Is.LessThan(.08f), "Новый вход сохранил старое приближение");
        }
    }
}
