using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TinyTrouble.Tests
{
    /// <summary>Открывает настоящий герой и даёт проверкам доступ к его частям.</summary>
    public abstract class CharacterTestRig : CosmeticLobbyTestScope
    {
        /// <summary>Корень героя из сохранённой сцены.</summary>
        protected GameObject hero;
        /// <summary>Состояние случайных чисел до проверки.</summary>
        UnityEngine.Random.State randomState;
        /// <summary>Исходная скорость времени игры.</summary>
        float timeScale;
        /// <summary>Проверки поведения оставляют обычное обновление Life включённым.</summary>
        protected virtual bool FreezeLife => true;

        /// <summary>Загружает лобби и останавливает автоматические движения для точных измерений.</summary>
        /// <returns>Ожидание загрузки и первого кадра.</returns>
        [UnitySetUp] public IEnumerator OpenRig()
        {
            randomState = UnityEngine.Random.state;
            timeScale = Time.timeScale;
            yield return SceneManager.LoadSceneAsync("Lobby");
            yield return null;
            hero = GameObject.Find("LobbyCharacter");
            hero.GetComponent<CharacterIdle>().enabled = false;
            var life = hero.GetComponent("CharacterLife") as Behaviour;
            if (life && FreezeLife) life.enabled = false;
            UnityEngine.Random.InitState(12345);
        }

        /// <summary>Возвращает время и случайные числа после теста.</summary>
        [TearDown] public void RestoreRigEnvironment()
        {
            Time.timeScale = timeScale;
            UnityEngine.Random.state = randomState;
        }

        /// <summary>Находит часть модели по сохранённому имени.</summary>
        /// <param name="name">Имя детали.</param>
        /// <returns>Положение найденной детали.</returns>
        protected Transform Part(string name) => hero.GetComponentsInChildren<Transform>(true).Single(t => t.name == name);

        /// <summary>Проверяет наличие нового компонента без ошибки компиляции до его реализации.</summary>
        /// <param name="name">Название компонента.</param>
        /// <returns>Найденный компонент.</returns>
        protected Component Required(string name)
        {
            var component = hero.GetComponent(name);
            Assert.That(component, Is.Not.Null, name + " ещё не подключён к герою");
            return component;
        }

        /// <summary>Передаёт позу через настоящий метод, когда новый тип уже загружен Unity.</summary>
        /// <param name="component">Компонент позы.</param>
        /// <param name="method">Имя метода.</param>
        /// <param name="gesture">Название жеста.</param>
        /// <param name="tail">Остальные аргументы метода.</param>
        protected static void Pose(Component component, string method, string gesture, params object[] tail)
        {
            var info = component.GetType().GetMethod(method);
            Assert.That(info, Is.Not.Null, method);
            var enumType = info.GetParameters().First(p => p.ParameterType.IsEnum).ParameterType;
            var args = method == "ApplyPose" ? new[] { Enum.Parse(enumType, gesture) }.Concat(tail).ToArray()
                : new[] { tail[0], Enum.Parse(enumType, gesture) }.Concat(tail.Skip(1)).ToArray();
            info.Invoke(component, args);
        }
    }
}
