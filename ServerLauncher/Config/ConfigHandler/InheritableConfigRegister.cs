namespace ServerLauncher.Config.ConfigHandler
{
    /// <summary>
    /// Реестр <see cref="ConfigEntry"/>. Этот абстрактный класс предоставляет базу для реализации обработчика конфигурации и наследования от <see cref="InheritableConfigRegister"/>.
    /// </summary>
    public abstract class InheritableConfigRegister : ConfigRegister
    {
        /// <summary>
        /// Создает <see cref="InheritableConfigRegister"/> с родительским <paramref name="parentConfigRegister"/>, чтобы наследовать неустановленные значения конфигурации.
        /// </summary>
        /// <param name="parentConfigRegister">Родительский <see cref="ConfigRegister"/>, от которого наследуются неустановленные значения конфигурации.</param>
        protected InheritableConfigRegister(ConfigRegister parentConfigRegister = null)
        {
            ParentConfigRegister = parentConfigRegister;
        }

        /// <summary>
        /// Родительский <see cref="ConfigRegister"/>, от которого происходит наследование.
        /// </summary>
        public ConfigRegister ParentConfigRegister { get; protected set; }

        /// <summary>
        /// Возвращает, следует ли наследовать <paramref name="configEntry"/> от родительского <see cref="ConfigRegister"/>.
        /// </summary>
        /// <param name="configEntry">Решаемый <see cref="ConfigEntry"/>, которому определяется наследование.</param>
        public abstract bool ShouldInheritConfigEntry(ConfigEntry configEntry);

        /// <summary>
        /// Обновляет значение <paramref name="configEntry"/>.
        /// </summary>
        /// <param name="configEntry">Регистрируемый <see cref="ConfigEntry"/>, которому присваивается значение.</param>
        public abstract void UpdateConfigValueInheritable(ConfigEntry configEntry);

        /// <summary>
        /// Обновляет значение <paramref name="configEntry"/> из этого <see cref="InheritableConfigRegister"/>, если <see cref="ParentConfigRegister"/> равен null или если <seealso cref="ShouldInheritConfigEntry"/> возвращает true.
        /// </summary>
        /// <param name="configEntry">Регистрируемый <see cref="ConfigEntry"/>, которому присваивается значение.</param>
        public override void UpdateConfigValue(ConfigEntry configEntry)
        {
            if (configEntry is null || !configEntry.Inherit || ParentConfigRegister is null ||
                !ShouldInheritConfigEntry(configEntry))
            {
                UpdateConfigValueInheritable(configEntry);
                return;
            }

            ParentConfigRegister.UpdateConfigValue(configEntry);
        }

        /// <summary>
        /// Возвращает массив иерархии <see cref="ConfigRegister"/>s.
        /// </summary>
        /// <param name="highestToLowest">Определяет, следует ли упорядочить возвращаемый массив от самого верхнего <see cref="ConfigRegister"/> в иерархии до самого низкого.</param>
        public ConfigRegister[] GetConfigRegisterHierarchy(bool highestToLowest = true)
        {
            var configRegisterHierarchy = new List<ConfigRegister>();

            ConfigRegister configRegister = this;

            while (configRegister is not null && !configRegisterHierarchy.Contains(configRegister))
            {
                configRegisterHierarchy.Add(configRegister);

                // Если существует еще один InheritableConfigRegister в качестве родителя, то получить родителя этого, в противном случае завершить цикл, так как больше нет родителей
                if (configRegister is not InheritableConfigRegister inheritableConfigRegister)
                {
                    break;
                }

                configRegister = inheritableConfigRegister.ParentConfigRegister;
            }

            if (highestToLowest)
            {
                configRegisterHierarchy.Reverse();
            }

            return configRegisterHierarchy.ToArray();
        }
    }
}