using System;
using System.Linq;

namespace TinyTrouble
{
    /// <summary>Причина результата покупки или выбора; интерфейс превращает её в понятную надпись.</summary>
    public enum CosmeticOperationResult { Success, AlreadyOwned, AlreadyEquipped, UnknownItem, NotOwned, InsufficientFunds, ReadOnly, Busy, SaveFailed }
    /// <summary>Управляет покупками и выбором без зависимости от кнопок и камеры.</summary>
    public sealed class CosmeticService
    {
        /// <summary>Текущее целое состояние игрока.</summary>
        CosmeticProfile profile;
        /// <summary>Каталог задаёт цену и место каждой вещи.</summary>
        readonly CosmeticCatalog catalog;
        /// <summary>Запись выполняется до публикации нового профиля.</summary>
        readonly ICosmeticProfileStore store;
        /// <summary>Запрещает вторую операцию внутри незавершённой записи.</summary>
        bool busy;
        /// <summary>Копия состояния; изменения этой копии не тратят настоящие монеты.</summary>
        public CosmeticProfile Snapshot => profile.Clone();
        /// <summary>Разрешены ли операции с прочитанным сохранением.</summary>
        public bool CanWrite { get; private set; }
        /// <summary>Пояснение ошибки сохранения.</summary>
        public string Error { get; private set; }
        /// <summary>Сообщает об успешно сохранённом изменении.</summary>
        public event Action Changed;
        /// <summary>Читает профиль через переданное хранилище.</summary>
        /// <param name="catalog">Известные определения товаров.</param>
        /// <param name="store">Способ сохранения.</param>
        /// <param name="legacyColor">Прежний номер цвета.</param>
        public CosmeticService(CosmeticCatalog catalog, ICosmeticProfileStore store, int legacyColor)
        {
            this.catalog = catalog; this.store = store;
            var result = store.Load(catalog, legacyColor);
            profile = result.Profile; CanWrite = result.CanWrite; Error = result.Error;
        }
        /// <summary>Проверяет владение платной либо бесплатной вещью.</summary>
        /// <param name="id">Код вещи.</param>
        /// <returns>Можно ли её выбрать.</returns>
        public bool Owns(string id) => catalog.TryGet(id, out var item) &&
            (item.Price == 0 || profile.OwnedIds.Contains(id));
        /// <summary>Покупает одну вещь без автоматического применения.</summary>
        /// <param name="id">Код товара.</param>
        /// <returns>Результат операции.</returns>
        public CosmeticOperationResult Buy(string id)
        {
            if (busy) return CosmeticOperationResult.Busy;
            if (!CanWrite) return CosmeticOperationResult.ReadOnly;
            if (!catalog.TryGet(id, out var item) || item.Price < 0) return CosmeticOperationResult.UnknownItem;
            if (Owns(id)) return CosmeticOperationResult.AlreadyOwned;
            if (profile.Balance < item.Price) return CosmeticOperationResult.InsufficientFunds;
            var next = profile.Clone();
            next.Balance -= item.Price;
            next.OwnedIds = next.OwnedIds.Concat(new[] { id }).ToArray();
            return Commit(next, null);
        }
        /// <summary>Сохраняет одну вещь в её собственном месте выбора.</summary>
        /// <param name="id">Код доступной вещи.</param>
        /// <returns>Результат операции.</returns>
        public CosmeticOperationResult Equip(string id)
        {
            if (busy) return CosmeticOperationResult.Busy;
            if (!CanWrite) return CosmeticOperationResult.ReadOnly;
            if (!catalog.TryGet(id, out var item) || (int)item.Slot < 0 || (int)item.Slot >= 10) return CosmeticOperationResult.UnknownItem;
            if (!Owns(id)) return CosmeticOperationResult.NotOwned;
            if (profile.EquippedIds[(int)item.Slot] == id) return CosmeticOperationResult.AlreadyEquipped;
            var next = profile.Clone();
            next.EquippedIds[(int)item.Slot] = id;
            return Commit(next, item.BasicColorIndex >= 0 ? (int?)item.BasicColorIndex : null);
        }
        /// <summary>Публикует новое состояние только после успешной записи на диск.</summary>
        /// <param name="next">Подготовленная копия.</param>
        /// <param name="legacyColor">Номер бесплатного цвета для прежнего API.</param>
        /// <returns>Успех либо ошибка сохранения.</returns>
        CosmeticOperationResult Commit(CosmeticProfile next, int? legacyColor)
        {
            busy = true;
            try
            {
                try { store.Save(next, legacyColor); }
                catch (Exception)
                { Error = "Не удалось сохранить изменение. Попробуйте ещё раз."; return CosmeticOperationResult.SaveFailed; }
                profile = next;
                Error = null;
                Changed?.Invoke();
                return CosmeticOperationResult.Success;
            }
            finally { busy = false; }
        }
    }
}
