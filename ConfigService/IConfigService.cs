namespace ConfigService
{
    public interface IConfigService
    {
        public ConfigData Load();

        public void Save(ConfigData configData);

        bool ExistsConfigDifference(ConfigData configData);
    }
}
