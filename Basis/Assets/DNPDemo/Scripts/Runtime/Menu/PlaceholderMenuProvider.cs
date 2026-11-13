using Basis.BasisUI;
using UnityEngine;

namespace DNPDemo.Menu
{
    /// <summary>
    /// A menu entry whose feature is not built yet. Its page shows a single notice.
    /// When the feature is implemented, derive its provider from <see cref="DemoPageMenuProvider"/> or <see cref="DemoBarMenuProvider"/> instead.
    /// </summary>
    public abstract class PlaceholderMenuProvider : DemoPageMenuProvider
    {
        const string Notice = "準備中です。";

        protected override void BuildPage(RectTransform content)
        {
            PanelElementDescriptor notice = PanelElementDescriptor.CreateNew(PanelElementDescriptor.ElementStyles.Group, content);
            notice.SetDescription(Notice);
        }
    }
}
