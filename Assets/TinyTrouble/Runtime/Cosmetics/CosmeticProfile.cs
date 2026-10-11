using System;

namespace TinyTrouble
{
    /// <summary>Сохранение игрока: деньги, купленные вещи и десять независимых выборов.</summary>
    [Serializable] public sealed class CosmeticProfile
    {
        /// <summary>Номер формата; новая версия должна иметь явное правило переноса.</summary>
        public int Version = 1;
        /// <summary>Оставшиеся Шалкоины.</summary>
        public int Balance;
        /// <summary>Коды купленных вещей; бесплатные цвета не требуют покупки.</summary>
        public string[] OwnedIds = Array.Empty<string>();
        /// <summary>Коды выбранных вещей в порядке CosmeticSlot.</summary>
        public string[] EquippedIds = new string[10];
        /// <summary>Создаёт независимую копию для подготовки изменения.</summary>
        /// <returns>Копия всех данных.</returns>
        public CosmeticProfile Clone() => new CosmeticProfile
        {
            Version = Version, Balance = Balance,
            OwnedIds = OwnedIds == null ? Array.Empty<string>() : (string[])OwnedIds.Clone(),
            EquippedIds = EquippedIds == null ? new string[10] : (string[])EquippedIds.Clone()
        };
    }
    /// <summary>Результат чтения; испорченное сохранение запрещает изменения.</summary>
    public sealed class CosmeticLoadResult
    {
        /// <summary>Доступное состояние для отображения.</summary>
        public CosmeticProfile Profile;
        /// <summary>Можно ли покупать и сохранять выбор.</summary>
        public bool CanWrite;
        /// <summary>Понятное сообщение об ошибке чтения или записи.</summary>
        public string Error;
    }
}
