using ConfigService;

namespace HttpServerWPF.ConfigMapper
{
    public sealed class ConfigMapper : IConfigMapper
    {
        public void ApplyTo(MainWindowViewModel vm, ConfigData config)
        {
            ArgumentNullException.ThrowIfNull(vm);
            ArgumentNullException.ThrowIfNull(config);

            vm.UseHttps.Value = string.Equals(config.Scheme, "https", StringComparison.OrdinalIgnoreCase);
            vm.HostName.Value = config.Host ?? string.Empty;
            vm.PortNo.Value = int.Parse(config.Port);
            vm.Path.Value = config.Path ?? string.Empty;

            if (Enum.TryParse<MainWindowViewModel.AuthenticationMethodType>(
                    config.AuthenticationMethod,
                    ignoreCase: true,
                    out var method))
            {
                vm.AuthenticationMethod.Value = method;
            }
            else
            {
                vm.AuthenticationMethod.Value = MainWindowViewModel.AuthenticationMethodType.Basic;
            }

            vm.User.Value = config.User ?? string.Empty;
            vm.Password.Value = config.Password ?? string.Empty;
            vm.UploadDirectoryPath.Value = config.UploadDirectoryPath ?? string.Empty;
        }

        public ConfigData CreateFrom(MainWindowViewModel vm)
        {
            ArgumentNullException.ThrowIfNull(vm);

            return new ConfigData
            {
                Scheme = vm.UseHttps.Value ? "https" : "http",
                Host = vm.HostName.Value ?? string.Empty,
                Port = vm.PortNo.Value.ToString(),
                Path = vm.Path.Value ?? string.Empty,
                AuthenticationMethod = vm.AuthenticationMethod.Value.ToString(),
                User = vm.User.Value ?? string.Empty,
                Password = vm.Password.Value ?? string.Empty,
                UploadDirectoryPath = vm.UploadDirectoryPath.Value ?? string.Empty,
            };
        }
    }
}
