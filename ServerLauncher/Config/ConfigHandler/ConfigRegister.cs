namespace ServerLauncher.Config.ConfigHandler
{
    /// <summary>
    /// Реестр <see cref="ConfigEntry"/>. Этот абстрактный класс предоставляет базовую реализацию обработчика конфигурации
    /// </summary>
    public abstract class ConfigRegister
    {
        /// <summary>
        /// Список зарегистрированных <see cref="ConfigEntry"/>
        /// </summary>
        private readonly HashSet<ConfigEntry> RegisteredConfigs = [];

        /// <summary>
        /// Возвращает массив зарегистрированных <see cref="ConfigEntry"/>
        /// </summary>
        public ConfigEntry[] GetRegisteredConfigs() => RegisteredConfigs.ToArray();

        /// <summary>
        /// Возвращает первый <see cref="ConfigEntry"/>, ключ которого совпадает с <paramref name="key"/>.
        /// </summary>
        /// <param name="key">Ключ <see cref="ConfigEntry"/>, который необходимо извлечь.</param>
        public ConfigEntry GetRegisteredConfig(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }

            key = key.ToLower();

            return RegisteredConfigs.FirstOrDefault(registeredConfig =>
                key.Equals(registeredConfig.Key, StringComparison.CurrentCultureIgnoreCase));
        }

        /// <summary>
        /// Регистрирует <paramref name="configEntry"/> в <see cref="ConfigRegister"/> для присвоения значения.
        /// </summary>
        /// <param name="configEntry">Регистрируемый <see cref="ConfigEntry"/>.</param>
        /// <param name="updateValue">Определяет, следует ли обновить значение конфигурации после регистрации.</param>
        public void RegisterConfig(ConfigEntry configEntry, bool updateValue = true)
        {
            if (configEntry is null || string.IsNullOrEmpty(configEntry.Key))
            {
                return;
            }

            RegisteredConfigs.Add(configEntry);

            if (updateValue)
            {
                UpdateConfigValue(configEntry);
            }
        }

        /// <summary>
        /// Отменяет регистрацию <paramref name="configEntry"/> из <see cref="ConfigRegister"/>.
        /// </summary>
        /// <param name="configEntry">Отменяемый <see cref="ConfigEntry"/>.</param>
        public void UnRegisterConfig(ConfigEntry configEntry)
        {
            if (configEntry is null || string.IsNullOrEmpty(configEntry.Key))
            {
                return;
            }

            RegisteredConfigs.Remove(configEntry);
        }

        /// <summary>
        /// Отменяет регистрацию <see cref="ConfigEntry"/>, связанного с указанным <paramref name="key"/>, из <see cref="ConfigRegister"/>.
        /// </summary>
        /// <param name="key">Ключ <see cref="ConfigEntry"/>, который необходимо отменить.</param>
        public void UnRegisterConfig(string key)
        {
            UnRegisterConfig(GetRegisteredConfig(key));
        }

        /// <summary>
        /// Обновляет значение <paramref name="configEntry"/>.
        /// </summary>
        /// <param name="configEntry">Регистрируемый <see cref="ConfigEntry"/>, которому присваивается значение.</param>
        public abstract void UpdateConfigValue(ConfigEntry configEntry);

        /// <summary>
        /// Обновляет значения зарегистрированных <see cref="ConfigEntry"/>.
        /// </summary>
        public void UpdateRegisteredConfigValues()
        {
            foreach (var registeredConfig in RegisteredConfigs)
            {
                UpdateConfigValue(registeredConfig);
            }
        }
    }
}