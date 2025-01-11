namespace CustomAvatarLoader
{
    using CustomAvatarLoader.Logging;
    using CustomAvatarLoader.Versioning;
    using Il2Cpp;
    using Il2CppUniGLTF;
    using Il2CppUniVRM10;
    using MelonLoader;
    using System.Reflection;
    using System.Windows.Forms;
    using UnityEngine;

    public class Core : MelonMod
    {
        private bool init;

        protected const string REPOSITORY_NAME = "YusufOzmen01/desktopmate-custom-avatar-loader";

        protected virtual Logging.ILogger Logger { get; private set; }

        protected virtual GitHubVersionChecker VersionChecker { get; private set; }

        protected virtual string CurrentVersion { get; private set; }

        protected virtual MelonPreferences_Category Settings { get; private set; }

        protected virtual MelonPreferences_Entry<string> VrmPath { get; private set; }

        protected virtual CharaData CharaData { get; private set; }

        protected virtual RuntimeAnimatorController RuntimeAnimatorController { get; private set; }

        public override void OnInitializeMelon()
        {
            CurrentVersion = MelonAssembly.Assembly
                .GetCustomAttribute<MelonInfoAttribute>()
                ?.Version ?? "Unknown";

            Logger = new MelonLoaderLogger(LoggerInstance);
            VersionChecker = new GitHubVersionChecker(REPOSITORY_NAME, Logger);

            // Initialize your preferences
            Settings = MelonPreferences.CreateCategory("settings");
            VrmPath = Settings.CreateEntry("vrmPath", "");

            var hasLatestVersion = VersionChecker.IsLatestVersionInstalled(CurrentVersion);

            if (!hasLatestVersion)
            {
                Logger.Info($"[VersionCheck] New version available");

                DialogResult result = System.Windows.Forms.MessageBox.Show(
                    $"A new version of the custom avatar loader is available - do you want to download it?",
                    "Update Available",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );

                switch (result)
                {
                    case DialogResult.Yes:
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = $"https://github.com/{REPOSITORY_NAME}/releases",
                                UseShellExecute = true
                            });

                            break;
                        }

                    case DialogResult.No:
                        {
                            Logger.Info("[VersionCheck] User chose to skip update for now.");
                            break;
                        }
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
            catch (Exception e)
            {
                LoggerInstance.Error("Error trying to load the VRM file! : " + e.Message);
                return false;
            }

            var chara = GameObject.Find("/CharactersRoot").transform.GetChild(0).gameObject;
            CharaData = chara.GetComponent<CharaData>();
            RuntimeAnimatorController = chara.GetComponent<Animator>().runtimeAnimatorController;

            LoggerInstance.Msg("Chara copied! Removing default chara...");
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
}