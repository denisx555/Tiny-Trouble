namespace TinyTrouble
{
    /// <summary>Отделяет правила покупок от способа сохранения.</summary>
    public interface ICosmeticProfileStore
    {
        /// <summary>Читает профиль и проверяет выбор по каталогу.</summary>
        /// <param name="catalog">Известные вещи.</param>
        /// <param name="legacyColor">Цвет из прежнего меню.</param>
        /// <returns>Профиль и разрешение изменения.</returns>
        CosmeticLoadResult Load(CosmeticCatalog catalog, int legacyColor);
        /// <summary>Сохраняет профиль и при необходимости прежний номер цвета.</summary>
        /// <param name="profile">Новое целое состояние.</param>
        /// <param name="legacyColor">Бесплатный цвет либо null для остальных вещей.</param>
        void Save(CosmeticProfile profile, int? legacyColor);
    }
}
