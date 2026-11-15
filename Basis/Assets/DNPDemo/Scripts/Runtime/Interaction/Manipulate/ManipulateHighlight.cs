using System.Collections.Generic;
using Basis.Scripts.BasisSdk.Highlight;
using UnityEngine;

namespace DNPDemo.Interaction.Manipulate
{
    /// <summary>
    /// Lights up a manipulated object with Basis's highlight (the same outline as a hovered pen) while the pen's tip can
    /// grab it or holds it.
    /// </summary>
    public sealed class ManipulateHighlight
    {
        readonly List<Renderer> _renderers = new();
        bool _lit;

        /// <summary>Switches to <paramref name="target"/>'s renderers. The previous object is unlit.</summary>
        public void Bind(Transform target)
        {
            Set(false);
            _renderers.Clear();
            if (target != null)
            {
                target.GetComponentsInChildren(false, _renderers);
            }
        }

        public void Set(bool lit)
        {
            if (_lit == lit)
            {
                return;
            }
            _lit = lit;
            foreach (Renderer renderer in _renderers)
            {
                if (renderer == null)
                {
                    continue;
                }
                if (lit)
                {
                    BasisHighlightManager.Highlight(renderer);
                }
                else
                {
                    BasisHighlightManager.Unhighlight(renderer);
                }
            }
        }
    }
}
