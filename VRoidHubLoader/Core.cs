using MelonLoader;
using UnityEngine;
using Il2Cpp;
using Il2CppUniGLTF;
using Il2CppUniVRM10;

[assembly: MelonInfo(typeof(MelonLoaderMod1.Core), "Custom Avatar Loader Mod", "1.0.3", "SergioMarquina", null)]
[assembly: MelonGame("infiniteloop", "DesktopMate")]

namespace MelonLoaderMod1
{
    public class Core : MelonMod
    {
        CharaData charaData;
        RuntimeAnimatorController runtimeAnimatorController;
        private MelonPreferences_Category settings;
        private MelonPreferences_Entry<string> vrmPath;
        public override void OnInitializeMelon()
        {
            settings = MelonPreferences.CreateCategory("settings");

            vrmPath = settings.CreateEntry("vrmPath", "");
        }
        private bool init = false;

        public override void OnUpdate()
        {
            if (Input.GetKeyDown(KeyCode.F4))
            {
                var dlg = new OpenFileDialog();
                dlg.Filter = "VRM Files (*.vrm)|*.vrm";
                dlg.Multiselect = false;

                if (dlg.ShowDialog() != DialogResult.OK)
                {
                    LoggerInstance.Error("Error opening file dialog!");

                    return;
                }

                if (LoadCharacter(dlg.FileName))
                {
                    vrmPath.Value = dlg.FileName;
                    init = true;
                }
            }

            if (!init && GameObject.Find("/CharactersRoot").transform.GetChild(0) != null)
            {
                init = true;

                if (vrmPath.Value != "") LoadCharacter(vrmPath.Value);
            }

            if (init && GameObject.Find("/CharactersRoot/VRMFILE") == null)
            {
                vrmPath.Value = "";
                MelonPreferences.Save();
            }
        }

        public bool LoadCharacter(string path)
        {
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
                Vrm10Data.Parse(data);
                var loaded = context.Load();

                loaded.EnableUpdateWhenOffscreen();
                loaded.ShowMeshes();
                loaded.gameObject.name = "VRMFILE";
                newChara = loaded.gameObject;
            }
            catch (Exception e)
            {
                LoggerInstance.Error("Error trying to load the VRM file! : " + e.Message);

                MessageBox.Show("This VRM file seems to be either incompatible, corrupt or doesn't exist anymore! Please try a different model file.", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);

                return false;
            }

            var chara = GameObject.Find("/CharactersRoot").transform.GetChild(0).gameObject;
            charaData = chara.GetComponent<CharaData>();
            runtimeAnimatorController = chara.GetComponent<Animator>().runtimeAnimatorController;

            LoggerInstance.Msg("Chara copied! Removing default chara...");
            UnityEngine.Object.Destroy(chara);

            newChara.transform.parent = GameObject.Find("/CharactersRoot").transform;

            CharaData newCharaData = newChara.AddComponent<CharaData>();
            newCharaData.alarmAnim = charaData.alarmAnim;
            newCharaData.draggedAnims = charaData.draggedAnims;
            newCharaData.hideLeftAnims = charaData.hideLeftAnims;
            newCharaData.hideRightAnims = charaData.hideRightAnims;
            newCharaData.jumpInAnim = charaData.jumpInAnim;
            newCharaData.jumpOutAnim = charaData.jumpOutAnim;
            newCharaData.pickedSittingAnim = charaData.pickedSittingAnim;
            newCharaData.pickedStandingAnim = charaData.pickedStandingAnim;
            newCharaData.sittingOneShotAnims = charaData.sittingOneShotAnims;
            newCharaData.sittingRandomAnims = charaData.sittingRandomAnims;
            newCharaData.standingOneShotAnims = charaData.standingOneShotAnims;
            newCharaData.standingRandomAnims = charaData.standingRandomAnims;
            newCharaData.strokedSittingAnim = charaData.strokedSittingAnim;
            newCharaData.strokedStandingAnim = charaData.strokedStandingAnim;

            MainManager manager = GameObject.Find("MainManager").GetComponent<MainManager>();
            manager.charaData = newCharaData;

            Animator charaAnimator = newChara.GetComponent<Animator>();
            charaAnimator.applyRootMotion = true;
            charaAnimator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            charaAnimator.runtimeAnimatorController = runtimeAnimatorController;

            LoggerInstance.Msg("Chara replaced!");
            MelonPreferences.Save();

            return true;
        }
    }
}
