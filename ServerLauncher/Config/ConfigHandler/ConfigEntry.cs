using System.Text.Json.Serialization;

namespace ServerLauncher.Config.ConfigHandler
{
    /// <summary>
    /// Базовый <see cref="ConfigEntry"/> для хранения значений конфигурации. Может быть зарегистрирован в <see cref="ConfigRegister"/> для автоматического получения значений конфигурации
    /// </summary>
    public abstract class ConfigEntry
    {
        /// <summary>
        /// Ключ для чтения из файла конфигурации
        /// </summary>
        [JsonIgnore]
        public string Key { get; set; }

        /// <summary>
        /// Значение <see cref="ConfigEntry"/>
        /// </summary>
        public abstract object ObjectValue { get; set; }

        /// <summary>
        /// Значение по умолчанию <see cref="ConfigEntry"/>
        /// </summary>
        public abstract object ObjectDefault { get; set; }

        /// <summary>
        /// Определяет, следует ли наследовать это значение конфигурации от родительских <see cref="ConfigRegister"/>, если они поддерживают наследование значений
        /// </summary>
        [JsonIgnore]
        public bool Inherit { get; set; }

        /// <summary>
        /// Название <see cref="ConfigEntry"/>
        /// </summary>
        [JsonIgnore]
        public string Name { get; set; }

        /// <summary>
        /// Описание <see cref="ConfigEntry"/>.
        /// </summary>
        [JsonIgnore]
        public string Description { get; set; }

        /// <summary>
        /// Создает базовый <see cref="ConfigEntry"/>, не содержащий значений, и указывает, следует ли наследовать значение
        /// </summary>
        public ConfigEntry(string key, bool inherit = true, string name = null, string description = null)
        {
            Key = key;

            Inherit = inherit;

            Name = name;
            Description = description;
        }

        /// <summary>
        /// Создает базовый <see cref="ConfigEntry"/> 
        /// </summary>
        public ConfigEntry(string key, string name = null, string description = null) : this(key, true, name,
            description)
        {
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// Обобщенный <see cref="ConfigEntry{T}" /> для хранения значений конфигурации. Может быть зарегистрирован в <see cref="ConfigEntry{T}" /> для автоматического получения значений конфигурации
    /// </summary>
    public class ConfigEntry<T> : ConfigEntry
    {
        /// <summary>
        /// Типизированное значение <see cref="ConfigEntry{T}"/>
        /// </summary>
        private T Value { get; set; }

        /// <summary>
        /// Типизированное значение по умолчанию <see cref="ConfigEntry{T}"/>
        /// </summary>
        private T Default { get; set; }

        public override object ObjectValue
        {
            get => Value;
            set => Value = (T)value;
        }

        public override object ObjectDefault
        {
            get => Default;
            set => Default = (T)value;
        }

        /// <inheritdoc />
        /// <summary>
        /// Создает <see cref="ConfigEntry{T}" />, используя предоставленный тип, значение по умолчанию и указание на наследование значения
        /// </summary>
        public ConfigEntry(string key, T defaultValue = default, bool inherit = true, string name = null,
            string description = null) : base(key, inherit, name, description)
        {
            Default = defaultValue;
        }

        /// <inheritdoc />
        /// <summary>
        /// Создает <see cref="ConfigEntry{T}" /> с предоставленным типом и значением по умолчанию
        /// </summary>
        public ConfigEntry(string key, T defaultValue = default, string name = null, string description = null) : this(
            key, defaultValue, true, name, description)
        {
        }
    }
}