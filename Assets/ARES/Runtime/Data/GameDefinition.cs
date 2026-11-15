using UnityEngine;

namespace Ares.Data
{
    [CreateAssetMenu(menuName = "ARES/Game Definition")]
    public sealed class GameDefinition : ScriptableObject
    {
        public LayoutData layout;
        public CampaignData campaign;
        public CampaignRules rules;
        public Ares.World.WorldConfiguration world;
        public TextAsset portuguese;
        public TextAsset english;
    }
}
