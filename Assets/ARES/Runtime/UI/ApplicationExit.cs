using UnityEngine;

namespace Ares.UI
{
    /// <summary>Platform boundary kept out of menu construction. Never closes the Unity editor itself.</summary>
    public static class ApplicationExit
    {
        public static void Quit()
        {
            PlayerPrefs.Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
