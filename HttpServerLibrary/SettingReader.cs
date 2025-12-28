using Newtonsoft.Json;

namespace HttpServerLibrary
{
    internal class SettingReader
    {
        private static ConfigData? _configData;
        public static ConfigData GetConfigData()
        {
            if (_configData == null)
            {
                string filePath = "external_setting_file.json";
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException("設定ファイルが見つかりません。", filePath);
                }

                string jsonText = File.ReadAllText(filePath);
                _configData = JsonConvert.DeserializeObject<ConfigData>(jsonText) ?? new ConfigData();
            }

            return _configData;
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

        [JsonProperty("timeout_seconds")]
        public int TimeoutSeconds { get; set; } = 20;
    }
}
