using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TinyTrouble
{
    /// <summary>
    /// Управляет вкладками, настройками и переходом на игровой уровень.
    /// </summary>
    /// <remarks>
    /// MonoBehaviour — компонент Unity, который можно прикрепить к объекту сцены. Само меню уже сохранено в сцене.
    /// </remarks>
    public sealed class LobbyController : MonoBehaviour
    {
        /// <summary>Общие операции магазина и инвентаря этого лобби.</summary>
        public CosmeticService Cosmetics { get; private set; }
        /// <summary>
        /// Содержимое четырёх вкладок. SerializeField сохраняет ссылки на объекты и показывает их в Inspector.
        /// </summary>
        [SerializeField] GameObject[] panels;
        /// <summary>
        /// Кнопки вкладок: 0 — «Играть», 1 — «Персонаж», 2 — «Магазин», 3 — «Настройки».
        /// </summary>
        [SerializeField] Button[] tabs;
        /// <summary>
        /// Большая кнопка «ИГРАТЬ», которая открывает игровой уровень.
        /// </summary>
        [SerializeField] Button playButton;
        /// <summary>
        /// Ползунок громкости со значением от 0 до 1.
        /// </summary>
        [SerializeField] Slider volumeSlider;
        /// <summary>
        /// Надпись с процентом громкости рядом с ползунком.
        /// </summary>
        [SerializeField] Text volumeText;
        /// <summary>
        /// Четыре кнопки выбора цвета толстовки.
        /// </summary>
        [SerializeField] Button[] colorButtons;
        /// <summary>
        /// Компоненты, которые рисуют только части толстовки героя.
        /// </summary>
        [SerializeField] Renderer[] hoodieRenderers;
        /// <summary>Получает сообщение только при выборе действительно другого цвета.</summary>
        [SerializeField] CharacterLife characterLife;
        /// <summary>Последний применённый номер цвета; восстановление при запуске не вызывает жест.</summary>
        int currentColor;
        /// <summary>
        /// Сообщение возле кнопки запуска: готовность, загрузка или ошибка.
        /// </summary>
        [SerializeField] Text statusText;
        /// <summary>
        /// Название сцены для перехода; сейчас в SampleScene только камера и свет.
        /// </summary>
        [SerializeField] string gameSceneName = "SampleScene";
        /// <summary>Сохранённый каталог всех косметических вещей.</summary>
        [SerializeField] CosmeticCatalog catalog;
        /// <summary>Единственный владелец личных материалов одежды.</summary>
        [SerializeField] CharacterAppearance appearance;
        /// <summary>Временная примерка одежды, предметов и питомцев.</summary>
        [SerializeField] CosmeticPreview preview;
        /// <summary>Два режима общего интерфейса вещей.</summary>
        [SerializeField] CosmeticPanel shopPanel, inventoryPanel;
        /// <summary>Баланс собственной игровой валюты в шапке.</summary>
        [SerializeField] Text balanceText;
        /// <summary>Последняя закреплённая одежда для одной реакции при настоящем изменении.</summary>
        string currentClothes;
        /// <summary>
        /// Показывает, что уровень уже загружается и повторное нажатие запрещено.
        /// </summary>
        public bool IsLoading { get; private set; }
        /// <summary>
        /// Проверяет Tab каждый кадр; при зажатом Shift выбирает обратное направление.
        /// </summary>
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab))
                AdvanceFocus(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
        }
        /// <summary>
        /// Переносит клавиатурное выделение на следующий доступный элемент.
        /// </summary>
        /// <param name="reverse">false — вперёд, true — назад.</param>
        /// <remarks>
        /// Выделение показывает, какой элемент сработает при Enter. EventSystem передаёт интерфейсу нажатия мыши и клавиатуры.
        /// </remarks>
        public void AdvanceFocus(bool reverse)
        {
            if (IsLoading || !EventSystem.current) return;
            // Собираем только доступные вкладки и элементы открытой панели.
            var choices = new List<Selectable>();
            foreach (var tab in tabs) if (tab.isActiveAndEnabled && tab.IsInteractable()) choices.Add(tab);
            foreach (var panel in panels)
                if (panel.activeInHierarchy)
                    foreach (var control in panel.GetComponentsInChildren<Selectable>())
                        if (control.isActiveAndEnabled && control.IsInteractable()) choices.Add(control);
            if (choices.Count == 0) return;
            // После последнего элемента возвращаемся к первому: % вычисляет остаток от деления.
            var current = choices.FindIndex(s => s.gameObject == EventSystem.current.currentSelectedGameObject);
            var next = current < 0 ? 0 : (current + (reverse ? choices.Count - 1 : 1)) % choices.Count;
            EventSystem.current.SetSelectedGameObject(choices[next].gameObject);
        }
        /// <summary>
        /// При запуске создаёт личные материалы героя и восстанавливает сохранённые настройки.
        /// </summary>
        void Awake()
        {
            appearance.Configure(hoodieRenderers);
            Cosmetics = new CosmeticService(catalog, new CosmeticProfileStore(new PlayerPrefsCosmeticPreferences()), LobbyPreferences.LoadHoodieColor());
            Cosmetics.Changed += OnCosmeticsChanged;
            shopPanel.Configure(catalog, Cosmetics, preview, CosmeticPanelMode.Shop);
            inventoryPanel.Configure(catalog, Cosmetics, preview, CosmeticPanelMode.Inventory);
            var hero = characterLife.transform;
            preview.Configure(catalog, appearance, hero.GetComponent<CharacterDance>(), hero,
                GetComponentsInChildren<Camera>(true).FirstOrDefault(), GetComponentsInChildren<RawImage>(true).FirstOrDefault(),
                GameObject.Find("CosmeticPreviewRoot").transform, GameObject.Find("PetAnchor").transform);
            OnCosmeticsChanged();
            ApplyVolume(LobbyPreferences.LoadVolume()); SelectTab(0);
        }
        /// <summary>После успешной записи обновляет обе панели и закреплённый вид.</summary>
        void OnCosmeticsChanged()
        {
            var profile = Cosmetics.Snapshot; balanceText.text = "Ш  " + profile.Balance + "  ШАЛКОИНЫ";
            var changed = currentClothes != null && currentClothes != profile.EquippedIds[0];
            currentClothes = profile.EquippedIds[0];
            if (catalog.TryGet(currentClothes, out var item)) { appearance.Apply(item); currentColor = item.BasicColorIndex; }
            preview.Restore(profile);
            shopPanel.Refresh(); inventoryPanel.Refresh();
            for (var i = 0; i < colorButtons.Length; i++) colorButtons[i].transform.Find("SelectionFrame").gameObject.SetActive(i == currentColor);
            if (changed && characterLife) characterLife.NotifyColorChanged();
        }
        /// <summary>
        /// Показывает выбранную панель и переводит выделение на видимый элемент.
        /// </summary>
        /// <param name="index">Номер вкладки: 0 «Играть», 1 «Персонаж», 2 «Магазин», 3 «Настройки».</param>
        public void SelectTab(int index)
        {
            if (IsLoading || index < 0 || index >= panels.Length) return;
            preview.Close(Cosmetics.Snapshot);
            // Показываем одну панель и меняем цвет кнопки активной вкладки.
            for (var i = 0; i < panels.Length; i++)
            {
                panels[i].SetActive(i == index);
                tabs[i].image.color = i == index ? new Color(1f, .85f, .29f) : new Color(.13f, .20f, .36f);
                tabs[i].GetComponentInChildren<Text>().color = i == index ? new Color(.04f, .09f, .18f) : Color.white;
            }
            // Стрелка вниз с вкладок ведёт к нужному элементу открытой панели.
            Selectable first = index == 0 ? (Selectable)playButton : index == 1 ? inventoryPanel.FirstSelectable : index == 2 ? shopPanel.FirstSelectable : volumeSlider;
            for (var i = 0; i < tabs.Length; i++)
            {
                var nav = tabs[i].navigation;
                nav.selectOnDown = first;
                tabs[i].navigation = nav;
            }
            // Стрелка вверх возвращает к вкладке.
            foreach (var selectable in panels[index].GetComponentsInChildren<Selectable>())
            {
                var nav = selectable.navigation;
                nav.selectOnUp = tabs[index];
                selectable.navigation = nav;
            }
            if (EventSystem.current) EventSystem.current.SetSelectedGameObject(first.gameObject);
        }
        /// <summary>
        /// Сохраняет выбранный цвет и сразу меняет толстовку.
        /// </summary>
        /// <param name="index">Номер образца цвета от 0 до 3.</param>
        public void SetHoodieColor(int index)
        {
            if (IsLoading || Cosmetics == null) return;
            Cosmetics.Equip(new[] { "base-yellow", "base-blue", "base-red", "base-purple" }[Mathf.Clamp(index, 0, 3)]);
        }
        /// <summary>
        /// Сохраняет громкость от ползунка и сразу меняет звук.
        /// </summary>
        /// <param name="value">Число от 0 (тишина) до 1 (100%).</param>
        public void SetVolume(float value) => ApplyVolume(LobbyPreferences.SaveVolume(value));
        /// <summary>
        /// Меняет общую громкость Unity, положение ползунка и надпись с процентом.
        /// </summary>
        /// <param name="value">Проверенное значение от 0 до 1.</param>
        /// <remarks>
        /// Общая громкость действует и после перехода на другую сцену.
        /// </remarks>
        void ApplyVolume(float value)
        {
            AudioListener.volume = value;
            // Меняем положение без вызова события ползунка, чтобы не сохранять настройку повторно.
            volumeSlider.SetValueWithoutNotify(value);
            // Из доли делаем процент: например, 0.37 × 100 = 37.
            volumeText.text = Mathf.RoundToInt(value * 100f) + " %";
        }
        /// <summary>
        /// Открывает игровой уровень по нажатию «ИГРАТЬ».
        /// </summary>
        /// <remarks>
        /// Повторная загрузка блокируется. Если уровень не найден или начало загрузки вызвало ошибку, меню остаётся доступным.
        /// </remarks>
        public void Play()
        {
            if (IsLoading) return;
            // Проверяем, что уровень есть в списке сцен игры.
            if (!Application.CanStreamedLevelBeLoaded(gameSceneName))
            {
                statusText.text = "Не удалось найти игровой уровень";
                return;
            }
            // Блокируем повторное нажатие до начала загрузки.
            IsLoading = true;
            preview.Close(Cosmetics.Snapshot);
            playButton.interactable = false;
            foreach (var tab in tabs) tab.interactable = false;
            statusText.text = "ЗАГРУЗКА…";
            // Async загружает уровень постепенно. При ошибке возвращаем доступ к кнопкам.
            try { SceneManager.LoadSceneAsync(gameSceneName); }
            catch (Exception)
            {
                IsLoading = false;
                playButton.interactable = true;
                foreach (var tab in tabs) tab.interactable = true;
                statusText.text = "Не удалось открыть уровень. Попробуйте ещё раз";
            }
        }
        /// <summary>Отписывается от обновлений профиля; личные материалы освобождает CharacterAppearance.</summary>
        void OnDestroy() { if (Cosmetics != null) Cosmetics.Changed -= OnCosmeticsChanged; }
    }
}
