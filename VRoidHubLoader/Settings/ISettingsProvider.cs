namespace CustomAvatarLoader.Settings;

public interface ISettingsProvider
{
    // Setting names are structured like this
    // setting1
    // setting2
    // module1.setting1
    // module1.setting2
    // module2.setting1

    T Get<T>(string setting, T defaultValue);

    bool Set<T>(string setting, T value);

    void SaveSettings();
}