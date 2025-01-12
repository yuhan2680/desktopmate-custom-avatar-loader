namespace CustomAvatarLoader;

using CustomAvatarLoader.Logging;
using CustomAvatarLoader.Modules;
using CustomAvatarLoader.Versioning;
using Il2Cpp;
using Il2CppUniGLTF;
using Il2CppUniVRM10;
using MelonLoader;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UnityEngine;

public class Core : MelonMod
{
    private bool init;

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int MessageBox(IntPtr hWnd, String text, String caption, uint type);

    protected virtual IServiceProvider Services { get; }

    protected const string REPOSITORY_NAME = "YusufOzmen01/desktopmate-custom-avatar-loader";

    protected virtual Logging.ILogger Logger { get; private set; }

    protected virtual GitHubVersionChecker VersionChecker { get; private set; }

    protected virtual string CurrentVersion { get; private set; }

    protected virtual MelonPreferences_Category Settings { get; private set; }

    protected virtual MelonPreferences_Entry<string> VrmPath { get; private set; }

    protected virtual CharaData CharaData { get; private set; }

    protected virtual RuntimeAnimatorController RuntimeAnimatorController { get; private set; }

    public IList<IModule> Modules { get; } = new List<IModule>();

    public override void OnInitializeMelon()
    {
        CurrentVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();

        Logger = new MelonLoaderLogger(LoggerInstance);
        VersionChecker = new GitHubVersionChecker(REPOSITORY_NAME, Logger);

        // Initialize your preferences
        Settings = MelonPreferences.CreateCategory("settings");
        VrmPath = Settings.CreateEntry("vrmPath", "");

        var hasLatestVersion = VersionChecker.IsLatestVersionInstalled(CurrentVersion);

        if (!hasLatestVersion)
        {
            Logger.Info("[VersionCheck] New version available");

            const uint MB_YESNO = 0x00000004;
            const uint MB_ICONQUESTION = 0x00000020;

            int result = MessageBox(IntPtr.Zero,
                "A new version of the custom avatar loader is available - do you want to download it?",
                "Update Available",
                MB_YESNO | MB_ICONQUESTION);

            switch (result)
            {
                case 6: // IDYES
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = $"https://github.com/{REPOSITORY_NAME}/releases",
                        UseShellExecute = true
                    });
                    break;

                case 7: // IDNO
                    Logger.Info("[VersionCheck] User chose to skip update for now.");
                    break;
            }
        }
        else
        {
            Logger.Info("[VersionCheck] Latest version installed");
        }
    }

    public override void OnUpdate()
    {
        if (Input.GetKeyDown(KeyCode.F4))
        {
            string path = FileHelper.OpenFileDialog();
            if (!string.IsNullOrEmpty(path) && LoadCharacter(path))
            {
                VrmPath.Value = path;
                init = true;
                MelonPreferences.Save();
            }
        }

        if (!init && GameObject.Find("/CharactersRoot").transform.GetChild(0) != null)
        {
            init = true;
            if (VrmPath.Value != "") LoadCharacter(VrmPath.Value);
        }

        if (!init || GameObject.Find("/CharactersRoot/VRMFILE") != null || VrmPath.Value == "")
            return;

        VrmPath.Value = "";
        MelonPreferences.Save();
    }

    private bool LoadCharacter(string path)
    {
        if (!File.Exists(path))
        {
            Logger.Error("VRM file does not exist: " + path);
            return false;
        }

        GameObject newChara;
        try
        {
            var data = new GlbFileParser(path).Parse();
            var vrmdata = Vrm10Data.Parse(data);
            if (vrmdata == null)
            {
                MigrationData migrationData;
                Vrm10Data.Migrate(data, out vrmdata, out migrationData);
                if (vrmdata == null)
                {
                    throw new Exception("Cannot load vrm file!");
                }
            }

            var context = new Vrm10Importer(vrmdata);
            var loaded = context.Load();

            loaded.EnableUpdateWhenOffscreen();
            loaded.ShowMeshes();
            loaded.gameObject.name = "VRMFILE";
            newChara = loaded.gameObject;
        }
        catch (Exception ex)
        {
            Logger.Error("Error trying to load the VRM file!", ex);
            return false;
        }

        var chara = GameObject.Find("/CharactersRoot").transform.GetChild(0).gameObject;
        CharaData = chara.GetComponent<CharaData>();
        RuntimeAnimatorController = chara.GetComponent<Animator>().runtimeAnimatorController;

        Logger.Debug("Chara copied! Removing default chara...");
        UnityEngine.Object.Destroy(chara);

        newChara.transform.parent = GameObject.Find("/CharactersRoot").transform;

        CharaData newCharaData = newChara.AddComponent<CharaData>();
        CopyCharaData(CharaData, newCharaData);

        MainManager manager = GameObject.Find("MainManager").GetComponent<MainManager>();
        manager.charaData = newCharaData;

        Animator charaAnimator = newChara.GetComponent<Animator>();
        charaAnimator.applyRootMotion = true;
        charaAnimator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
        charaAnimator.runtimeAnimatorController = RuntimeAnimatorController;

        Logger.Debug("Chara replaced!");

        return true;
    }

    private void CopyCharaData(CharaData source, CharaData target)
    {
        target.alarmAnim = source.alarmAnim;
        target.draggedAnims = source.draggedAnims;
        target.hideLeftAnims = source.hideLeftAnims;
        target.hideRightAnims = source.hideRightAnims;
        target.jumpInAnim = source.jumpInAnim;
        target.jumpOutAnim = source.jumpOutAnim;
        target.pickedSittingAnim = source.pickedSittingAnim;
        target.pickedStandingAnim = source.pickedStandingAnim;
        target.sittingOneShotAnims = source.sittingOneShotAnims;
        target.sittingRandomAnims = source.sittingRandomAnims;
        target.standingOneShotAnims = source.standingOneShotAnims;
        target.standingRandomAnims = source.standingRandomAnims;
        target.strokedSittingAnim = source.strokedSittingAnim;
        target.strokedStandingAnim = source.strokedStandingAnim;
    }
}