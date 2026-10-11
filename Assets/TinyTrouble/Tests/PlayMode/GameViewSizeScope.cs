using System;
using System.Reflection;
using UnityEditor;

namespace TinyTrouble.Tests
{
    /// <summary>Меняет настоящий размер Game View и возвращает список размеров после проверки.</summary>
    public sealed class GameViewSizeScope : IDisposable
    {
        /// <summary>Окно игры, его выбор размера и группа временных вариантов.</summary>
        readonly EditorWindow view;
        /// <summary>Свойство выбранного размера редактора.</summary>
        readonly PropertyInfo selected;
        /// <summary>Группа встроенных и временных размеров.</summary>
        readonly object group;
        /// <summary>Исходный выбор, число встроенных и добавленных размеров.</summary>
        readonly int previous, builtIn;
        /// <summary>Количество вариантов, добавленных только этим тестом.</summary>
        int added;
        /// <summary>Запоминает состояние редактора перед изменениями.</summary>
        public GameViewSizeScope()
        {
            var assembly = typeof(Editor).Assembly; var type = assembly.GetType("UnityEditor.GameView"); view = EditorWindow.GetWindow(type);
            selected = type.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic); previous = (int)selected.GetValue(view);
            var sizesType = assembly.GetType("UnityEditor.GameViewSizes"); var sizes = typeof(ScriptableSingleton<>).MakeGenericType(sizesType).GetProperty("instance").GetValue(null);
            group = sizesType.GetMethod("GetGroup").Invoke(sizes, new[] { sizesType.GetProperty("currentGroupType").GetValue(sizes) });
            builtIn = (int)group.GetType().GetMethod("GetBuiltinCount").Invoke(group, null);
        }
        /// <summary>Добавляет и выбирает настоящий размер окна, не меняя код раскладки.</summary>
        /// <param name="width">Ширина в пикселях.</param>
        /// <param name="height">Высота в пикселях.</param>
        public void Set(int width, int height)
        {
            var assembly = typeof(Editor).Assembly; var type = assembly.GetType("UnityEditor.GameViewSize"); var kind = Enum.Parse(assembly.GetType("UnityEditor.GameViewSizeType"), "FixedResolution");
            var size = Activator.CreateInstance(type, new[] { kind, (object)width, height, "Проверка магазина" }); group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { size }); added++;
            var count = (int)group.GetType().GetMethod("GetCustomCount").Invoke(group, null); selected.SetValue(view, builtIn + count - 1); view.Repaint();
        }
        /// <summary>Возвращает прежний выбор и удаляет только добавленные нами размеры.</summary>
        public void Dispose()
        {
            selected.SetValue(view, previous);
            for (var i = 0; i < added; i++) { var count = (int)group.GetType().GetMethod("GetCustomCount").Invoke(group, null); group.GetType().GetMethod("RemoveCustomSize").Invoke(group, new object[] { builtIn + count - 1 }); }
            added = 0; view.Repaint();
        }
    }
}
