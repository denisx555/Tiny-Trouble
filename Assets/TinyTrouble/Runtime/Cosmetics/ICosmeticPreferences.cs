namespace TinyTrouble
{
    /// <summary>Маленький доступ к ключам; в тестах заменяется хранением в памяти.</summary>
    public interface ICosmeticPreferences
    {
        /// <summary>Проверяет наличие ключа.</summary>
        /// <param name="key">Имя настройки.</param>
        /// <returns>Существует ли настройка.</returns>
        bool HasKey(string key);
        /// <summary>Читает текстовый документ.</summary>
        /// <param name="key">Имя настройки.</param>
        /// <returns>Строка настройки.</returns>
        string GetString(string key);
        /// <summary>Читает номер цвета.</summary>
        /// <param name="key">Имя настройки.</param>
        /// <returns>Целое значение.</returns>
        int GetInt(string key);
        /// <summary>Записывает текст в память настроек.</summary>
        /// <param name="key">Имя настройки.</param>
        /// <param name="value">Новый текст.</param>
        void SetString(string key, string value);
        /// <summary>Записывает номер цвета в память.</summary>
        /// <param name="key">Имя настройки.</param>
        /// <param name="value">Новое целое значение.</param>
        void SetInt(string key, int value);
        /// <summary>Удаляет отсутствовавший до неудачной записи ключ.</summary>
        /// <param name="key">Имя настройки.</param>
        void DeleteKey(string key);
        /// <summary>Передаёт настройки на диск; ошибка передаётся вызывающему коду.</summary>
        void Flush();
    }
}
