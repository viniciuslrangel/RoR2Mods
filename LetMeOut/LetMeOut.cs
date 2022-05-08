using BepInEx;
using R2API;
using R2API.Utils;

namespace LetMeOut
{
    [BepInDependency(R2API.R2API.PluginGUID)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [R2APISubmoduleDependency(nameof(LanguageAPI))]
    public class LetMeOutMod : BaseUnityPlugin
    {
        //The Plugin GUID should be a unique ID for this plugin, which is human readable (as it is used in places like the config).
        //If we see this PluginGUID as it is on thunderstore, we will deprecate this mod. Change the PluginAuthor and the PluginName !
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "viniciuslrangel";
        public const string PluginName = "LetMeOut";
        public const string PluginVersion = "1.2.0";

        public static PluginInfo PInfo { get; private set; }

        public delegate void OnUpdateDelegate();

        public static event OnUpdateDelegate OnUpdate;


        public void Awake()
        {
            Log.Init(Logger);

            PInfo = Info;

            new LockInsideArtifact().Init();

            Log.LogInfo(nameof(Awake) + " done.");
        }

        private void Update()
        {
            OnUpdate?.Invoke();
        }
    }
}