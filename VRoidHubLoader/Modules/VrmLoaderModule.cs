using System.Runtime.InteropServices;
using CustomAvatarLoader.Settings;

namespace CustomAvatarLoader.Modules;

using CustomAvatarLoader.Helpers;
using Il2Cpp;
using MelonLoader;
using UnityEngine;
using ILogger = Logging.ILogger;

public class VrmLoaderModule : IModule
{
    private bool init;

    public VrmLoaderModule(ILogger logger, ISettingsProvider settingsProvider)
    {
        Logger = logger;
        SettingsProvider = settingsProvider;
        VrmLoader = new VrmLoader(Logger);
    }

    protected virtual ILogger Logger { get; }
    
    protected virtual ISettingsProvider SettingsProvider { get; }

    protected virtual VrmLoader VrmLoader { get; }

    protected virtual CharaData CharaData { get; set; }

    protected virtual RuntimeAnimatorController RuntimeAnimatorController { get; set; }

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern int MessageBox(IntPtr hwnd, String text, String caption, uint type);
    
    protected virtual AsyncHelper AsyncHelper { get; set; }
     
    public void OnInitialize()
    {
        AsyncHelper = new AsyncHelper();
    }

    public async void OnUpdate()
    {
        if (!init)
        {
            string vrmPath = SettingsProvider.Get("vrmPath", string.Empty);
            if (GameObject.Find("/CharactersRoot")?.transform?.GetChild(0) != null
                && !string.IsNullOrEmpty(vrmPath))
            {
                LoadCharacter(vrmPath);
            }

            init = true;
        }

        AsyncHelper.OnUpdate();

        if (Input.GetKeyDown(KeyCode.F4))
        {
            Logger.Debug($"OnUpdate: VrmLoaderModule F4 pressed");

            var fileHelper = new FileHelper();

            string path = await fileHelper.OpenFileDialog();

            AsyncHelper.RunOnMainThread(() => {
                if (!string.IsNullOrEmpty(path) && LoadCharacter(path))
                {
                    SettingsProvider.Set("vrmPath", path);
                    MelonPreferences.Save();

                    Logger.Debug($"OnUpdate: VrmLoaderModule file chosen");
                }
            });
        }
    }

    public bool LoadCharacter(string path)
    {
        if (!File.Exists(path))
        {
            Logger.Error("[Chara Loader] VRM file does not exist: " + path);
            
            return false;
        }

        var chara = GameObject.Find("/CharactersRoot").transform.GetChild(0).gameObject;
        CharaData = chara.GetComponent<CharaData>();
        RuntimeAnimatorController = chara.GetComponent<Animator>().runtimeAnimatorController;

        Logger.Debug("Character attributes have been copied!");

        GameObject newChara = VrmLoader.LoadVrmIntoScene(path);
        if (newChara == null)
        {
            Logger.Error("[Chara Loader] Failed to load VRM file: " + path);
            Task.Run(() => { MessageBox(new IntPtr(0), "Failed to load VRM file! Make sure the VRM file is compatible!", "Error", 0x00000010 /* MB_ICONERROR */); });

            return false;
        }
        
        Logger.Debug("Old character has been destroyed.");
        Object.Destroy(chara);

        newChara.transform.parent = GameObject.Find("/CharactersRoot").transform;

        CharaData newCharaData = newChara.AddComponent<CharaData>();
        CopyCharaData(CharaData, newCharaData);

        MainManager manager = GameObject.Find("MainManager").GetComponent<MainManager>();
        manager.charaData = newCharaData;

        Animator charaAnimator = newChara.GetComponent<Animator>();
        charaAnimator.applyRootMotion = true;
        charaAnimator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
        charaAnimator.runtimeAnimatorController = RuntimeAnimatorController;

        Logger.Debug("Character attribute replacement succeeded!");

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