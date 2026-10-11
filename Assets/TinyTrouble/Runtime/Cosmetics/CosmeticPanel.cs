using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TinyTrouble
{
    /// <summary>Выбирает покупку товара или применение собственной вещи.</summary>
    public enum CosmeticPanelMode
    {
        /// <summary>Магазин с покупкой за Шалкоины.</summary>
        Shop,
        /// <summary>Инвентарь купленных и бесплатных вещей.</summary>
        Inventory
    }
    /// <summary>Общий интерфейс магазина и инвентаря с отдельными просмотром и применением.</summary>
    public sealed class CosmeticPanel : MonoBehaviour
    {
        /// <summary>Место карточек и их сохраняемый шаблон.</summary>
        [SerializeField] RectTransform cardsRoot;
        /// <summary>Шаблон новой карточки, включая шрифт и обработчик кнопки.</summary>
        [SerializeField] CosmeticCard cardPrefab;
        /// <summary>Восемь кнопок категорий в порядке каталога.</summary>
        [SerializeField] Button[] categoryButtons;
        /// <summary>Покупка или применение и отдельный повтор танца.</summary>
        [SerializeField] Button actionButton, danceButton;
        /// <summary>Название, описание и результат операции.</summary>
        [SerializeField] Text detailTitle, description, operationStatus, emptyMessage;
        /// <summary>Сохранённые данные и временная примерка.</summary>
        CosmeticCatalog catalog;
        /// <summary>Общие операции профиля двух панелей.</summary>
        CosmeticService service;
        /// <summary>Просмотр, который никогда не записывает данные самостоятельно.</summary>
        CosmeticPreview preview;
        /// <summary>Режим покупки или собственной коллекции.</summary>
        CosmeticPanelMode mode;
        /// <summary>Карточки переиспользуются, чтобы обновление не теряло клавиатурный фокус.</summary>
        readonly Dictionary<string, CosmeticCard> cards = new Dictionary<string, CosmeticCard>();
        /// <summary>Выбранная категория и временно просматриваемая карточка.</summary>
        CosmeticCategory category;
        /// <summary>Выделенный товар, который может отличаться от надетого.</summary>
        CosmeticItem selected;
        /// <summary>Первый доступный элемент для входа клавиатурой.</summary>
        public Selectable FirstSelectable => categoryButtons[(int)category];
        /// <summary>Подключает каталог и общие операции профиля.</summary>
        /// <param name="catalog">Каталог вещей.</param>
        /// <param name="service">Операции покупки и выбора.</param>
        /// <param name="preview">Временная примерка.</param>
        /// <param name="mode">Режим этой панели.</param>
        public void Configure(CosmeticCatalog catalog, CosmeticService service, CosmeticPreview preview, CosmeticPanelMode mode)
        {
            this.catalog = catalog; this.service = service; this.preview = preview; this.mode = mode;
            foreach (var item in catalog.Items)
            {
                if (mode == CosmeticPanelMode.Shop && item.Price == 0) continue;
                if (!cards.TryGetValue(item.Id, out var card))
                {
                    var existing = item.BasicColorIndex >= 0 ? cardsRoot.Find("Color" + item.BasicColorIndex) : null;
                    card = existing ? existing.GetComponent<CosmeticCard>() : Instantiate(cardPrefab, cardsRoot);
                    card.name = item.BasicColorIndex >= 0 ? "Color" + item.BasicColorIndex : "Card-" + item.Id;
                    cards.Add(item.Id, card);
                }
                card.Configure(item, service, SelectItem);
            }
            Refresh();
        }
        /// <summary>Открывает категорию, завершая предыдущую примерку и танец.</summary>
        /// <param name="category">Категория вещей.</param>
        public void SelectCategory(CosmeticCategory category)
        {
            if (!System.Enum.IsDefined(typeof(CosmeticCategory), category) || service == null) return;
            preview.Close(service.Snapshot); this.category = category; selected = null; operationStatus.text = ""; Refresh();
            if (selected != null) preview.Show(selected);
            var scroll = cardsRoot.GetComponentInParent<ScrollRect>(); if (scroll) scroll.verticalNormalizedPosition = 1;
        }
        /// <summary>Сохранённый обработчик кнопки категории.</summary>
        /// <param name="index">Номер категории от нуля до семи.</param>
        public void SelectCategoryIndex(int index) { SelectCategory((CosmeticCategory)index); }
        /// <summary>Выделяет карточку без покупки или применения.</summary>
        /// <param name="id">Код выбранной вещи.</param>
        public void SelectItem(string id)
        {
            if (service == null || !catalog.TryGet(id, out var item) || item.Category != category || !cards.TryGetValue(id, out var card) || !card.gameObject.activeSelf) return;
            selected = item; operationStatus.text = ""; Refresh(); preview.Show(item);
        }
        /// <summary>Показывает выделенную вещь при входе на вкладку, не меняя надетое.</summary>
        public void ShowSelection() { if (selected != null) preview.Show(selected); }
        /// <summary>Обновляет карточки из профиля, сохраняя существующие объекты и выделение.</summary>
        public void Refresh()
        {
            if (service == null) return;
            foreach (var item in catalog.Items)
            {
                if (!cards.TryGetValue(item.Id, out var card)) continue;
                var visible = item.Category == category && (mode == CosmeticPanelMode.Shop || service.Owns(item.Id));
                card.gameObject.SetActive(visible); card.Configure(item, service, SelectItem);
            }
            if (selected == null || !cards.TryGetValue(selected.Id, out var selectedCard) || !selectedCard.gameObject.activeSelf)
                selected = catalog.Items.FirstOrDefault(i => mode == CosmeticPanelMode.Inventory && service.Snapshot.EquippedIds[(int)i.Slot] == i.Id && cards.TryGetValue(i.Id, out var card) && card.gameObject.activeSelf)
                    ?? catalog.Items.FirstOrDefault(i => cards.TryGetValue(i.Id, out var card) && card.gameObject.activeSelf);
            foreach (var pair in cards) pair.Value.SetHighlighted(selected != null && pair.Key == selected.Id);
            for (var i = 0; i < categoryButtons.Length; i++) categoryButtons[i].image.color = i == (int)category ? new Color(.12f, .48f, .63f) : new Color(.12f, .18f, .31f);
            emptyMessage.gameObject.SetActive(selected == null); actionButton.gameObject.SetActive(selected != null);
            danceButton.gameObject.SetActive(selected != null && mode == CosmeticPanelMode.Inventory && category == CosmeticCategory.Dance);
            detailTitle.text = selected == null ? "" : selected.Name + (selected.Category == CosmeticCategory.Weapon ? " · " + WeaponName(selected.Slot) : "");
            description.text = selected == null ? "" : selected.Description;
            if (selected != null)
            {
                var owned = service.Owns(selected.Id); var equipped = service.Snapshot.EquippedIds[(int)selected.Slot] == selected.Id;
                actionButton.GetComponentInChildren<Text>().text = mode == CosmeticPanelMode.Shop ? owned ? "Куплено" : "Купить · " + selected.Price + " Ш" : equipped ? "Выбрано" : EquipLabel(selected.Category);
                actionButton.interactable = service.CanWrite && CanAct() && (mode == CosmeticPanelMode.Shop ? !owned && service.Snapshot.Balance >= selected.Price : owned && !equipped);
                danceButton.interactable = owned && CanAct();
            }
            if (!service.CanWrite) operationStatus.text = "Сохранение недоступно. Покупки и выбор отключены.";
            else if (selected != null && mode == CosmeticPanelMode.Shop && !service.Owns(selected.Id) && service.Snapshot.Balance < selected.Price) operationStatus.text = "Не хватает Шалкоинов";
            RebuildNavigation();
            if (isActiveAndEnabled && EventSystem.current)
            {
                var focus = EventSystem.current.currentSelectedGameObject; var control = focus ? focus.GetComponent<Selectable>() : null;
                if (!control || !focus.activeInHierarchy || !control.IsInteractable()) EventSystem.current.SetSelectedGameObject(FirstSelectable.gameObject);
                else if (focus.transform.IsChildOf(cardsRoot)) EnsureSelectionVisible(control);
            }
        }
        /// <summary>Пропускает скрытые карточки и связывает стрелками текущую сетку и доступные действия.</summary>
        public void RebuildNavigation()
        {
            if (!cardsRoot || categoryButtons == null) return;
            var visible = cardsRoot.GetComponentsInChildren<CosmeticCard>().Select(c => c.GetComponent<Button>()).ToArray();
            var columns = cardsRoot.GetComponent<GridLayoutGroup>().constraintCount;
            var header = transform.parent.Find("Header"); var tab = header ? header.Find(mode == CosmeticPanelMode.Shop ? "TabShop" : "TabCharacter").GetComponent<Button>() : null;
            var first = visible.Length > 0 ? visible[0] : actionButton.isActiveAndEnabled && actionButton.IsInteractable() ? actionButton : categoryButtons[0];
            for (var i = 0; i < categoryButtons.Length; i++)
                categoryButtons[i].navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = categoryButtons[(i + 7) % 8], selectOnRight = categoryButtons[(i + 1) % 8], selectOnUp = i >= 4 ? categoryButtons[i - 4] : tab, selectOnDown = i < 4 ? categoryButtons[i + 4] : first };
            for (var i = 0; i < visible.Length; i++)
                visible[i].navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = i > 0 ? visible[i - 1] : categoryButtons[4], selectOnRight = i + 1 < visible.Length ? visible[i + 1] : categoryButtons[7], selectOnUp = i >= columns ? visible[i - columns] : categoryButtons[4 + i % 4], selectOnDown = i + columns < visible.Length ? visible[i + columns] : actionButton.isActiveAndEnabled && actionButton.IsInteractable() ? actionButton : categoryButtons[0] };
            var last = visible.Length > 0 ? visible[visible.Length - 1] : categoryButtons[4];
            actionButton.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = last, selectOnDown = categoryButtons[0], selectOnRight = danceButton.isActiveAndEnabled && danceButton.IsInteractable() ? danceButton : categoryButtons[0] };
            danceButton.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = last, selectOnDown = categoryButtons[0], selectOnLeft = actionButton.IsInteractable() ? actionButton : last };
        }
        /// <summary>Прокручивает выбранную карточку целиком в видимую область списка.</summary>
        /// <param name="selection">Выделенный клавиатурой элемент.</param>
        public void EnsureSelectionVisible(Selectable selection)
        {
            if (!selection || !selection.transform.IsChildOf(cardsRoot)) return;
            var scroll = cardsRoot.GetComponentInParent<ScrollRect>(); Canvas.ForceUpdateCanvases();
            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport, selection.transform); var area = scroll.viewport.rect;
            if (bounds.min.y < area.yMin) scroll.content.anchoredPosition += Vector2.up * (area.yMin - bounds.min.y);
            else if (bounds.max.y > area.yMax) scroll.content.anchoredPosition -= Vector2.up * (bounds.max.y - area.yMax);
        }
        /// <summary>Покупает или применяет только после повторной проверки сервиса.</summary>
        public void PerformAction()
        {
            if (selected == null || service == null || !CanAct()) return;
            var item = selected;
            var result = mode == CosmeticPanelMode.Shop ? service.Buy(item.Id) : service.Equip(item.Id);
            Refresh();
            if (result == CosmeticOperationResult.Success)
            {
                operationStatus.text = mode == CosmeticPanelMode.Shop ? "Куплено! Вещь появилась в инвентаре." : "Выбор сохранён";
                // Обновление профиля возвращает надетое; затем снова показываем выбранную карточку.
                preview.Show(item);
            }
            else if (result == CosmeticOperationResult.SaveFailed) operationStatus.text = "Не удалось сохранить. Изменение не выполнено.";
            else if (result == CosmeticOperationResult.ReadOnly) operationStatus.text = "Сохранение недоступно. Изменение не выполнено.";
            else if (result == CosmeticOperationResult.InsufficientFunds) operationStatus.text = "Не хватает Шалкоинов";
            else if (result == CosmeticOperationResult.AlreadyOwned) operationStatus.text = "Эта вещь уже куплена";
        }
        /// <summary>Повторяет принадлежащий игроку танец без новой записи выбора.</summary>
        public void PlayDance() { if (selected != null && category == CosmeticCategory.Dance && service.Owns(selected.Id) && CanAct()) preview.Show(selected); }
        /// <summary>Проверяет, что переход на уровень ещё не начался.</summary>
        /// <returns>Разрешено ли действие интерфейса.</returns>
        bool CanAct() { var controller = GetComponentInParent<LobbyController>(); return !controller || !controller.IsLoading; }
        /// <summary>Возвращает понятное название типа оружия.</summary>
        /// <param name="slot">Отдельное место скина оружия.</param>
        /// <returns>Название типа.</returns>
        static string WeaponName(CosmeticSlot slot) => slot == CosmeticSlot.Pistol ? "Пистолет" : slot == CosmeticSlot.Rifle ? "Автомат" : "Дробовик";
        /// <summary>Называет действие применения вещи.</summary>
        /// <param name="value">Категория товара.</param>
        /// <returns>Текст кнопки действия.</returns>
        static string EquipLabel(CosmeticCategory value)
        {
            switch (value)
            {
                case CosmeticCategory.Clothes: return "Надеть";
                case CosmeticCategory.Weapon: case CosmeticCategory.Pickaxe: return "Выбрать скин";
                case CosmeticCategory.Dance: return "Выбрать и танцевать";
                case CosmeticCategory.Spray: return "Выбрать спрей";
                case CosmeticCategory.Parachute: return "Выбрать парашют";
                case CosmeticCategory.Pet: return "Выбрать питомца";
                default: return "Выбрать след";
            }
        }
        /// <summary>Возвращает актуальные карточки при открытии вкладки.</summary>
        void OnEnable() { if (service != null) Refresh(); }
    }
}
