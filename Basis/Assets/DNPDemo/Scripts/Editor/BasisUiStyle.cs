using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static DNPDemo.Editor.WorldUiBuilder;

namespace DNPDemo.Editor
{
    /// <summary>
    /// Builds world-space uGUI parts that look like the Basis menus, from the sprites, icons and colours of the Basis SDK
    /// (Packages/com.basis.sdk: Menu Panel, PE Button, Close Button).
    /// <para>
    /// The parts use the default UI material, not the Basis UI materials: those draw in the Overlay queue without the
    /// depth test (the Basis menus always draw over the world), which would put the demo's panels in front of hands and
    /// pens and break the ordering with objects shown on a panel.
    /// </para>
    /// </summary>
    public static class BasisUiStyle
    {
        const string BasisSdk = "Packages/com.basis.sdk";
        public const string RoundedSpritePath = BasisSdk + "/Sprites/Circle 512.png";
        public const string CloseIconPath = BasisSdk + "/Textures/Runtime/xWhite.png";
        public const string ArrowIconPath = BasisSdk + "/Sprites/Icons/Foldout Arrow.png";

        /// <summary>Close, arrows and other secondary buttons (Basis PE Button / Close Button).</summary>
        public static readonly Color SecondaryButtonColor = new(0.42573604f, 0.41214734f, 0.57232696f, 1f);

        /// <summary>The main action of a panel (Basis Button Standard).</summary>
        public static readonly Color PrimaryButtonColor = new(0.47413066f, 0.49170873f, 0.8238993f, 1f);

        /// <summary>
        /// Dark violet close to the Basis menu background, which Basis draws with an animated gradient shader in the
        /// Overlay queue (see the class summary).
        /// </summary>
        public static readonly Color PanelColor = new(0.09f, 0.075f, 0.16f, 0.94f);

        /// <summary>Basis button tints, except that a disabled button stays visible, greyed out, instead of disappearing.</summary>
        static readonly ColorBlock ButtonColors = new()
        {
            normalColor = new Color(1f, 1f, 1f, 0.75f),
            highlightedColor = Color.white,
            pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f),
            selectedColor = new Color(1f, 1f, 1f, 0.75f),
            disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.45f),
            colorMultiplier = 1f,
            fadeDuration = 0.1f,
        };

        // Circle 512 is sliced with 256 px borders at 512 pixels per unit; on a canvas with 100 reference pixels per unit,
        // a multiplier of 1 rounds corners with a 50 px radius.
        const float RoundedSpriteRadius = 50f;

        /// <summary>
        /// Rounded image named <paramref name="name"/> filling <paramref name="parent"/>, in <paramref name="color"/>.
        /// <paramref name="material"/> null uses the default UI material.
        /// </summary>
        public static Image CreatePanelImage(string name, Transform parent, Color color, float cornerRadius, Material material = null)
        {
            GameObject go = CreateChild(name, parent);
            Anchor((RectTransform)go.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return CreateRoundedImage(go, color, cornerRadius, material);
        }

        /// <summary>
        /// A Basis-styled button of the given colour with rounded corners of <paramref name="cornerRadius"/> canvas pixels.
        /// Position it with <see cref="WorldUiBuilder.Anchor"/>, then add an icon or a label.
        /// </summary>
        public static Button CreateButton(string name, Transform parent, Color color, float cornerRadius)
        {
            GameObject go = CreateChild(name, parent);
            Image image = CreateRoundedImage(go, color, cornerRadius, null);
            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            button.colors = ButtonColors;
            return button;
        }

        /// <summary>White icon centred in <paramref name="button"/>, <paramref name="inset"/> pixels from its edges, turned by <paramref name="rotation"/> degrees.</summary>
        public static Image AddIcon(Button button, string spritePath, float inset, float rotation = 0f)
        {
            GameObject go = CreateChild("Icon", button.transform);
            var rect = (RectTransform)go.transform;
            Anchor(rect, Vector2.zero, Vector2.one, new Vector2(inset, inset), new Vector2(-inset, -inset));
            rect.localRotation = Quaternion.Euler(0f, 0f, rotation);
            var image = go.AddComponent<Image>();
            image.sprite = LoadAsset<Sprite>(spritePath);
            image.color = new Color(0.98f, 1f, 1f, 1f);
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>White label filling <paramref name="button"/>.</summary>
        public static TMP_Text AddLabel(Button button, string text, float fontSize)
        {
            TMP_Text label = CreateText("Label", button.transform, text, fontSize, TextAlignmentOptions.Center);
            Anchor(label.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            label.raycastTarget = false;
            return label;
        }

        static Image CreateRoundedImage(GameObject go, Color color, float cornerRadius, Material material)
        {
            var image = go.AddComponent<Image>();
            image.sprite = LoadAsset<Sprite>(RoundedSpritePath);
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = RoundedSpriteRadius / Mathf.Max(1f, cornerRadius);
            image.material = material;
            image.color = color;
            return image;
        }

        static T LoadAsset<T>(string path) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                throw new InvalidOperationException($"{path} was not found.");
            }
            return asset;
        }
    }
}
