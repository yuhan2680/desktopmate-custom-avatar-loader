using System.IO;
using System.Runtime.InteropServices;
using CustomAvatarLoader;
using Il2Cpp;
using Il2CppUniGLTF;
using Il2CppUniVRM10;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(Core), "Custom Avatar Loader Mod", "1.0.3", "SergioMarquina, Misandrie")]
[assembly: MelonGame("infiniteloop", "DesktopMate")]

namespace CustomAvatarLoader;

public class Core : MelonMod
{
    CharaData _charaData;
    RuntimeAnimatorController _runtimeAnimatorController;
    private MelonPreferences_Category _settings;
    private MelonPreferences_Entry<string> _vrmPath;

    public override void OnInitializeMelon()
    {
        _settings = MelonPreferences.CreateCategory("settings");
        _vrmPath = _settings.CreateEntry("vrmPath", "");
    }

    private bool _init = false;

    public override void OnUpdate()
    {
        if (Input.GetKeyDown(KeyCode.F4))
        {
            string path = FileHelper.OpenFileDialog();
            if (!string.IsNullOrEmpty(path) && LoadCharacter(path))
            {
                _vrmPath.Value = path;
                _init = true;
                MelonPreferences.Save();
            }
        }

        if (!_init && GameObject.Find("/CharactersRoot").transform.GetChild(0) != null)
        {
            _init = true;
            if (_vrmPath.Value != "") LoadCharacter(_vrmPath.Value);
        }

        if (!_init || GameObject.Find("/CharactersRoot/VRMFILE") != null || _vrmPath.Value == "") 
            return;
        
        _vrmPath.Value = "";
        MelonPreferences.Save();
    }

    private bool LoadCharacter(string path)
    {
        if (!File.Exists(path))
        {
            LoggerInstance.Error("VRM file does not exist: " + path);
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
                    throw new System.Exception("Cannot load vrm file!");
                }
            }

            var context = new Vrm10Importer(vrmdata);
            var loaded = context.Load();

            loaded.EnableUpdateWhenOffscreen();
            loaded.ShowMeshes();
            loaded.gameObject.name = "VRMFILE";
            newChara = loaded.gameObject;
        }
        catch (System.Exception e)
        {
            LoggerInstance.Error("Error trying to load the VRM file! : " + e.Message);
            return false;
        }

        var chara = GameObject.Find("/CharactersRoot").transform.GetChild(0).gameObject;
        _charaData = chara.GetComponent<CharaData>();
        _runtimeAnimatorController = chara.GetComponent<Animator>().runtimeAnimatorController;

        LoggerInstance.Msg("Chara copied! Removing default chara...");
        UnityEngine.Object.Destroy(chara);

        newChara.transform.parent = GameObject.Find("/CharactersRoot").transform;

        CharaData newCharaData = newChara.AddComponent<CharaData>();
        CopyCharaData(_charaData, newCharaData);

        MainManager manager = GameObject.Find("MainManager").GetComponent<MainManager>();
        manager.charaData = newCharaData;

        Animator charaAnimator = newChara.GetComponent<Animator>();
        charaAnimator.applyRootMotion = true;
        charaAnimator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
        charaAnimator.runtimeAnimatorController = _runtimeAnimatorController;

        LoggerInstance.Msg("Chara replaced!");

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