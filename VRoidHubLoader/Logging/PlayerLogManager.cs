namespace CustomAvatarLoader.Logging;

using CustomAvatarLoader.Settings;
using MelonLoader;

public class PlayerLogManager
{
    public PlayerLogManager(
        ISettingsProvider settingsProvider,
        ILogger logger)
    {
        SettingsProvider = settingsProvider;
        Logger = logger;
    }

    protected virtual ISettingsProvider SettingsProvider { get; }

    protected virtual ILogger Logger { get; }

    public void ClearLog(string logPath)
    {
        bool disableReadOnly = SettingsProvider.Get("disable_log_readonly", false);
        SettingsProvider.SaveSettings();

        if (File.Exists(logPath))
        {
            File.SetAttributes(logPath, FileAttributes.Normal);
            try
            {
                File.WriteAllText(logPath, string.Empty);
            }
            catch (Exception ex)
            {
                Logger.Warn($"Failed to clear log file at {logPath}", ex);
            }

            File.SetAttributes(logPath, disableReadOnly ? FileAttributes.Normal : FileAttributes.ReadOnly);
        }
    }
}