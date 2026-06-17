using UnityEngine.SceneManagement;

namespace Core.Scenes
{
    /// <summary>Names of scenes registered in Build Settings. Keep in sync with EditorBuildSettings.</summary>
    public static class SceneNames
    {
        public const string Init = "InitScene";
        public const string Town = "Town";
        public const string Game = "Game_TestScene";
    }

    public static class SceneLoader
    {
        /// <summary>
        /// Loads a scene asynchronously in Single mode. The async load finishes streaming in all
        /// of the scene's assets before it activates, so the swap only happens once everything is
        /// ready (no half-loaded frame).
        /// </summary>
        public static void Load(string sceneName)
        {
            SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        }
    }
}
