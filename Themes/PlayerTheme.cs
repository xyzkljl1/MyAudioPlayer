using System.Drawing;

namespace MyAudioPlayer.Themes
{
    internal sealed class PlayerTheme
    {
        public string Id { get; init; } = "";
        public string DisplayName { get; init; } = "";
        public Color WindowBackColor { get; init; }
        public Color SurfaceColor { get; init; }
        public Color SubtleSurfaceColor { get; init; }
        public Color TitleBackColor { get; init; }
        public Color TextColor { get; init; }
        public Color MutedTextColor { get; init; }
        public Color BorderColor { get; init; }
        public Color ButtonBackColor { get; init; }
        public Color ButtonHoverColor { get; init; }
        public Color ButtonDownColor { get; init; }
        public Color ButtonIconColor { get; init; }
        public Color AccentColor { get; init; }
        public Color AccentHoverColor { get; init; }
        public Color AccentDownColor { get; init; }
        public Color AccentIconColor { get; init; } = Color.White;
        public Color FavoriteColor { get; init; }
        public Color FavoriteHoverColor { get; init; }
        public Color FavoriteDownColor { get; init; }
        public Color DeleteColor { get; init; }
        public Color DeleteHoverColor { get; init; }
        public Color DeleteDownColor { get; init; }
        public Color DeletePartColor { get; init; }
        public Color DeletePartHoverColor { get; init; }
        public Color DeletePartDownColor { get; init; }
        public Color SliderTrackColor { get; init; }
        public Color SliderFillColor { get; init; }
        public Color SliderThumbColor { get; init; }
        public Color SliderTickColor { get; init; }
        public Color SliderShadowColor { get; init; }
        public Color ListBackColor { get; init; }
        public Color ListForeColor { get; init; }
        public Color ListSelectedBackColor { get; init; }
        public Color ListSelectedForeColor { get; init; }
        public int ButtonCornerRadius { get; init; } = 14;
        public float ButtonBorderWidth { get; init; } = 2F;
        public float SliderTrackHeight { get; init; } = 8F;
        public float SliderThumbSize { get; init; } = 19F;
        public float SliderActiveThumbSize { get; init; } = 22F;
    }
}
