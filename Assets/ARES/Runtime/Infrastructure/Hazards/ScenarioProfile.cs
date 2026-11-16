using Ares.Domain.Hazards;
using UnityEngine;

namespace Ares.Infrastructure.Hazards
{
    [CreateAssetMenu(menuName="ARES/Hazards/Scenario Profile")]
    public sealed class ScenarioProfile : ScriptableObject
    {
        public ScenarioRules slots=new ScenarioRules();
        public HazardBalance provisionalBalance=new HazardBalance();
    }
}
