using System;
using UnityEngine;

namespace Ares.World
{
    /// <summary>Unity locomotion extension; reference walking/campaign values remain separate.</summary>
    [Serializable]
    public sealed class SprintSettings
    {
        [Range(1.1f, 3f)] public float speedMultiplier = 1.75f;
        public float Speed(float walkingSpeed, bool requested) => walkingSpeed * (requested ? speedMultiplier : 1);
    }
}
