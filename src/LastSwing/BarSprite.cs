using UnityEngine;

namespace LastSwing
{
    /// <summary>
    /// Generates the rounded shapes the bar is drawn from, at runtime.
    ///
    /// Nothing here ships as an art file — the same approach Chest Labels used for its pencil
    /// icon, and for the same reason: an archive containing only a DLL is easier to trust, and
    /// the game has no bar or meter graphic to borrow in the first place.
    ///
    /// Both sprites are drawn <b>white</b> and 9-sliced, so a single texture serves every
    /// segment at any size and colour comes entirely from <see cref="GamePalette"/> via the
    /// Image tint. A flat rectangle is the single thing that reads as bolted-on; every panel in
    /// the game is rounded, so these are too. See 10-visual-integration.md.
    /// </summary>
    internal static class BarSprite
    {
        private const int Size = 32;

        private static Sprite segment;
        private static Sprite plate;

        /// <summary>A rounded pill for one segment of the bar, or the continuous fill.</summary>
        internal static Sprite Segment()
        {
            return segment ?? (segment = Build("LastSwing_Segment", corner: 8, edge: 0f));
        }

        /// <summary>
        /// The darker backing the segments sit on, with a rim. Gives the bar an edge against a
        /// bright background — snow, water, a lit window — without an outline shader.
        /// </summary>
        internal static Sprite Plate()
        {
            return plate ?? (plate = Build("LastSwing_Plate", corner: 10, edge: 2.5f));
        }

        /// <summary>
        /// Signed-distance rounded rectangle, which gives a clean antialiased edge without
        /// supersampling. <paramref name="edge"/> above zero darkens a rim just inside the
        /// boundary; the alpha channel carries the shape either way.
        /// </summary>
        private static Sprite Build(string name, int corner, float edge)
        {
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = name
            };

            var pixels = new Color[Size * Size];

            for (var y = 0; y < Size; y++)
            {
                for (var x = 0; x < Size; x++)
                {
                    var distance = RoundedRectDistance(x + 0.5f, y + 0.5f, corner);

                    // Negative inside the shape, positive outside.
                    var coverage = Mathf.Clamp01(0.5f - distance);

                    var colour = Color.white;

                    if (edge > 0f)
                    {
                        // The band just inside the boundary becomes the rim. Darkening rather
                        // than recolouring keeps the sprite tintable.
                        var rim = Mathf.Clamp01(distance + edge) * Mathf.Clamp01(0.5f - distance);
                        colour = Color.Lerp(Color.white, new Color(0.45f, 0.35f, 0.55f), rim);
                    }

                    colour.a = coverage;
                    pixels[y * Size + x] = colour;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            // The border tells Unity which region stretches; corners keep their radius.
            var border = new Vector4(corner, corner, corner, corner);
            var built = Sprite.Create(
                texture, new Rect(0f, 0f, Size, Size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, border);
            built.name = name;
            return built;
        }

        private static float RoundedRectDistance(float x, float y, int corner)
        {
            const float half = Size * 0.5f;
            var innerHalf = half - corner;

            var dx = Mathf.Abs(x - half) - innerHalf;
            var dy = Mathf.Abs(y - half) - innerHalf;

            var outsideX = Mathf.Max(dx, 0f);
            var outsideY = Mathf.Max(dy, 0f);
            var outside = Mathf.Sqrt(outsideX * outsideX + outsideY * outsideY);

            var inside = Mathf.Min(Mathf.Max(dx, dy), 0f);

            return outside + inside - corner;
        }
    }
}
