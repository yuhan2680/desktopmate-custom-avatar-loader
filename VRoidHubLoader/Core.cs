using MelonLoader;
using UnityEngine;
using Il2Cpp;
using Il2CppKirurobo;

[assembly: MelonInfo(typeof(MelonLoaderMod1.Core), "VRoid Hub Loader Mod", "1.0.0", "SergioMarquina", null)]
[assembly: MelonGame("infiniteloop", "DesktopMate")]

namespace MelonLoaderMod1
{
    public class Core : MelonMod
    {
        CharaData charaData;
        GameObject chara;
        RuntimeAnimatorController runtimeAnimatorController;

        bool cloned = false, replaced = false;

        public override void OnUpdate()
        {
            if (!cloned)
            {
                chara = GameObject.Find("iltan_prefab(Clone)");
                if (chara != null)
                {
                    charaData = chara.GetComponent<CharaData>();
                    runtimeAnimatorController = chara.GetComponent<Animator>().runtimeAnimatorController;

                    LoggerInstance.Msg("Chara copied! Removing default chara...");
                    chara.SetActive(false);
                    cloned = true;

                    UnityEngine.Object.Destroy(chara);
                    Thread.Sleep(1000);
                    GameObject.Find("/MenuCanvas/MenuParent/Old").SetActive(true);
                    GameObject.Find("/MenuCanvas/MenuParent/Old/ModelPageOld").SetActive(true);

                    GameObject.Find("/UniWindowController").GetComponent<UniWindowController>().isTransparent = false;
                    Screen.SetResolution(800, 600, false);
                    LoggerInstance.Msg("VRoid Hub menu enabled!");
                }

                return;
            }

            if (!replaced)
            {
                chara = GameObject.Find("VRM1");
                if (chara != null)
                {
                    CharaData newCharaData = chara.AddComponent<CharaData>();
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

                    Animator charaAnimator = chara.GetComponent<Animator>();
                    charaAnimator.applyRootMotion = true;
                    charaAnimator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
                    charaAnimator.runtimeAnimatorController = runtimeAnimatorController;

                    LoggerInstance.Msg("Chara replaced!");
                    Thread.Sleep(1000);
                    GameObject.Find("/MenuCanvas/MenuParent/Old").SetActive(false);
                    GameObject.Find("/UniWindowController").GetComponent<UniWindowController>().isTransparent = true;
                    LoggerInstance.Msg("UI fixed!");

                    replaced = true;
                }
            }
        }
    }
}