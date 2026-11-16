using UnityEngine;

namespace Ares.World
{
    [CreateAssetMenu(menuName="ARES/Hazards/Audio Profile")]
    public sealed class HazardAudioProfile:ScriptableObject
    {
        [Tooltip("PROVISIONAL overall hazard gain; output is routed to the Hazards mixer group.")]
        [Range(0,1)]public float gain=.38f;
        [Tooltip("Optional replacement clips, indexed Flare, CME, Radiation, Dust, Cold, LowOxygen, Power. Empty entries use original procedural synthesis.")]
        public AudioClip[] clips=new AudioClip[7];
        [Range(0,1)]public float indoorWeatherGain=.35f,modalGain=.2f;
    }
}
