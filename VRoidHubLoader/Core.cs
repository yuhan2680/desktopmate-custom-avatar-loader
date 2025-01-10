using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Forms; // May not be available on all platforms
using CustomAvatarLoader;
using Il2Cpp;
using Il2CppUniGLTF;
using Il2CppUniVRM10;
using MelonLoader;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

[assembly: MelonInfo(typeof(Core), "Custom Avatar Loader Mod", "1.0.2", "SergioMarquina, Misandrie")]
[assembly: MelonGame("infiniteloop", "DesktopMate")]

namespace CustomAvatarLoader
{
    public class Core : MelonMod
    {
        private string CurrentVersion =>
            MelonAssembly.Assembly
                .GetCustomAttribute<MelonInfoAttribute>()
                ?.Version ?? "Unknown";

        private MelonPreferences_Category _settings;
        private MelonPreferences_Entry<string> _vrmPath;

        private CharaData _charaData;
        private RuntimeAnimatorController _runtimeAnimatorController;
        private bool _init;

        public override void OnInitializeMelon()
        {
            // Initialize your preferences
            _settings = MelonPreferences.CreateCategory("settings");
            _vrmPath = _settings.CreateEntry("vrmPath", "");

            // Kick off the version check in the background
            // so we don't block Unity's main thread.
            CheckForUpdates();
        }

        /// <summary>
        /// Checks the GitHub tags endpoint for the latest version. 
        /// If the local version is behind, displays a message.
        /// </summary>
        private void CheckForUpdates()
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    MelonLogger.Msg($"[VersionCheck] Current version: {CurrentVersion}");

                    MelonLogger.Msg($"[VersionCheck] Checking for updates...");

                    // GitHub API requires a user-agent
                    client.DefaultRequestHeaders.UserAgent.Add(
                        new ProductInfoHeaderValue("DesktopMate-Mod", CurrentVersion));

                    // Fetch the tags from your GitHub repo
                    string json = client.GetStringAsync(
                        "https://api.github.com/repos/YusufOzmen01/desktopmate-custom-avatar-loader/tags").Result;

                    // Deserialize the JSON array of tag objects
                    List<GitHubTag> tags = JsonSerializer.Deserialize<List<GitHubTag>>(json);

                    if (tags == null || tags.Count == 0)
                    {
                        return;
                    }

                    // Find the latest version by comparing the numeric parts
                    Version latestVersion = GetLatestVersion(tags);

                    // Compare it to our local version
                    var localVersion = new Version(CurrentVersion);

                    if (latestVersion > localVersion)
                    {
                        MelonLogger.Msg($"[VersionCheck] New version available: {latestVersion}");

                        DialogResult result = System.Windows.Forms.MessageBox.Show(
                            $"A new version of the custom avatar loader is available - do you want to download it?",
                            "Update Available",
                            MessageBoxButtons.YesNoCancel,
                            MessageBoxIcon.Question
                        );

                        if (result == DialogResult.Yes)
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = "https://github.com/YusufOzmen01/desktopmate-custom-avatar-loader/releases",
                                UseShellExecute = true
                            });
                        }
                        else if (result == DialogResult.No)
                        {
                            MelonLogger.Msg("[VersionCheck] User chose to skip update for now.");
                        }
                        else if (result == DialogResult.Cancel)
                        {
                            MelonLogger.Msg("[VersionCheck] User chose to skip this version.");
                        }
                    }
                    else
                    {
                        MelonLogger.Msg($"[VersionCheck] Latest version installed");
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[VersionCheck] Error checking for updates: {ex.Message}");
            }
        }

        /// <summary>
        /// Parse each tag's name (e.g. "v1.0.3") into a <see cref="System.Version"/> and return the highest.
        /// </summary>
        private Version GetLatestVersion(List<GitHubTag> tags)
        {
            Version highest = new Version(0, 0, 0);

            foreach (var tag in tags)
            {
                // Some tags start with 'v' (e.g. "v1.0.3"), strip that out
                string versionStr = tag.Name.StartsWith("v", StringComparison.OrdinalIgnoreCase)
                    ? tag.Name.Substring(1)
                    : tag.Name;

                if (Version.TryParse(versionStr, out Version parsed))
                {
                    if (parsed > highest)
                        highest = parsed;
                }
            }

            return highest;
        }

        // F4 and other logic from your original code

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

    /// <summary>
    /// Represents just enough JSON fields from GitHub's Tag API response for our usage.
    /// </summary>
    public class GitHubTag
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        // We ignore other fields not relevant to the version check.
    }
}
