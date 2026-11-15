using UnityEngine;
using UnityEngine.UIElements;

namespace Ares.UI
{
    /// <summary>Runs after terminal telemetry and world updates, so dynamic text cannot flicker back to its source language.</summary>
    [DefaultExecutionOrder(1000)]
    public sealed class PresentationLocalizationLoop : MonoBehaviour
    {
        VisualElement root;
        public void Bind(VisualElement view){root=view;PresentationLocalizer.Refresh(root);}
        void LateUpdate(){if(root?.panel!=null)PresentationLocalizer.Refresh(root);}
    }
}
