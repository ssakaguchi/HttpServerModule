using ConfigService;

namespace HttpServerWPF.ConfigMapper
{
    public interface IConfigMapper
    {
        // ConfigData -> ViewModelへ反映
        void ApplyTo(MainWindowViewModel vm, ConfigData config);

        // ViewModel -> ConfigData生成
        ConfigData CreateFrom(MainWindowViewModel vm);
    }
}
