namespace Ares.Editor
{
    /// <summary>Legacy entry point redirects to the scoped Casa Marte verification.</summary>
    public static class ManualWateringChecks
    {
        [UnityEditor.MenuItem("ARES/Checks/Manual watering")]
        public static void RequestCheck() => GreenhouseChecks.RequestCheck();
    }
}
