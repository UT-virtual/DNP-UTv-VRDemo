using System;
using System.Collections.Generic;
using Basis.BasisUI;
using UnityEngine;

namespace DNPDemo.Menu
{
    /// <summary>
    /// A menu entry that opens a second row of buttons right above the hotbar, styled like the hotbar itself.
    /// Each button is one choice; the chosen one carries the hotbar's active indicator.
    /// </summary>
    public abstract class DemoBarMenuProvider : DemoMenuProvider
    {
        /// <summary>Gap between the hotbar and the bar, in hotbar canvas units.</summary>
        const float GapAboveHotbar = 30f;

        /// <summary>One button on the bar.</summary>
        protected readonly struct BarItem
        {
            public readonly string Title;
            public readonly string IconAddress;
            public readonly Action OnClick;
            public readonly Func<bool> IsSelected;

            public BarItem(string title, string iconAddress, Action onClick, Func<bool> isSelected)
            {
                Title = title;
                IconAddress = iconAddress;
                OnClick = onClick;
                IsSelected = isSelected;
            }
        }

        readonly List<(PanelButton Button, BarItem Item)> _buttons = new();

        /// <summary>The buttons, left to right. Read each time the bar opens.</summary>
        protected abstract IReadOnlyList<BarItem> CreateItems();

        protected sealed override BasisMenuPanel.PanelData CreatePanelData()
        {
            // Derived from the hotbar's own geometry so the bar stays aligned with it.
            BasisMenuPanel.PanelData data = BasisMenuPanel.PanelData.Toolbar(Title);
            data.PanelPosition += Vector3.up * (data.PanelSize.y + GapAboveHotbar);
            return data;
        }

        protected sealed override string PanelStyle => BasisMenuPanel.PanelStyles.Default;

        protected sealed override void BuildPanel(BasisMenuPanel panel)
        {
            _buttons.Clear();
            PanelElementDescriptor row = PanelElementDescriptor.CreateNew(
                PanelElementDescriptor.ElementStyles.ScrollViewHorizontal, panel.Descriptor.ContentParent);
            if (row.ContentParent.TryGetComponent(out BasisHorizontalLayout layout))
            {
                layout.spacing = 0;
            }

            foreach (BarItem item in CreateItems())
            {
                PanelButton button = PanelButton.CreateNew(PanelButton.ButtonStyles.Hotbar, row.ContentParent);
                if (button == null)
                {
                    continue;
                }
                button.Descriptor.SetTitle(item.Title);
                button.SetIcon(item.IconAddress);
                button.EnableIconHoverAnimation();
                BarItem captured = item;
                button.OnClicked += () =>
                {
                    captured.OnClick?.Invoke();
                    RefreshIndicators();
                };
                _buttons.Add((button, item));
            }
            RefreshIndicators();
        }

        public override void OnReleaseEvent()
        {
            _buttons.Clear();
            base.OnReleaseEvent();
        }

        void RefreshIndicators()
        {
            foreach ((PanelButton button, BarItem item) in _buttons)
            {
                if (button != null && button.ButtonStyling != null)
                {
                    button.ButtonStyling.ShowIndicator(item.IsSelected != null && item.IsSelected());
                }
            }
        }
    }
}
