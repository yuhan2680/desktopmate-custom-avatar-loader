using MelonLoader;
using UnityEngine;
using Il2Cpp;
using Il2CppUniGLTF;
using Il2CppUniVRM10;

[assembly: MelonInfo(typeof(MelonLoaderMod1.Core), "Custom Avatar Loader Mod", "1.0.2", "SergioMarquina", null)]
[assembly: MelonGame("infiniteloop", "DesktopMate")]

namespace MelonLoaderMod1
{
    public class Core : MelonMod
    {
        CharaData charaData;
        RuntimeAnimatorController runtimeAnimatorController;
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

                GameObject newChara;
                try
                {
                    var data = new GlbFileParser(dlg.FileName).Parse();
                    var context = new Vrm10Importer(new Vrm10Data(data, new Vrm10ImportData(data).gltfVrm));
                    var loaded = context.Load();

                    loaded.EnableUpdateWhenOffscreen();
                    loaded.ShowMeshes();
                    newChara = loaded.gameObject;
                }
                catch (Exception e)
                {
                    LoggerInstance.Error("Error trying to load the VRM file! : " + e.Message);

                    MessageBox.Show("This VRM file seems to be either incompatible or corrupt! Please try a different model file.", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    return;
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
            }
        }
    }
}
