using Newtonsoft.Json;

namespace ConfigService
{
    public class ConfigManager : IConfigService
    {
        private static ConfigData? _configData;
        private readonly string _filePath;

        public ConfigManager(string filePath) => _filePath = filePath;

        public ConfigData Load()
        {
            if (_configData != null) return _configData;

            if (!File.Exists(_filePath))
            {
                throw new FileNotFoundException("設定ファイルが見つかりません。", _filePath);
            }

            string jsonText = File.ReadAllText(_filePath);
            _configData = JsonConvert.DeserializeObject<ConfigData>(jsonText) ?? new ConfigData();

            return _configData;
        }

        public void Save(ConfigData configData)
        {
            string jsonText = JsonConvert.SerializeObject(configData, Formatting.Indented);
            File.WriteAllText(_filePath, jsonText);
            _configData = configData;
        }
    }


    public class ConfigData
    {
        [JsonProperty("host_name")]
        public string Host { get; set; } = string.Empty;

        [JsonProperty("port_no")]
        public string Port { get; set; } = string.Empty;

        [JsonProperty("path")]
        public string Path { get; set; } = string.Empty;

        [JsonProperty("authentication_method")]
        public string AuthenticationMethod { get; set; } = string.Empty;

        [JsonProperty("user")]
        public string User { get; set; } = string.Empty;

        [JsonProperty("password")]
        public string Password { get; set; } = string.Empty;

    }
}
