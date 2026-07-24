using UnityEngine;

public static class PaperTextureGenerator
{
    private static Texture2D cachedTexture;

    /// <summary>
    /// Generate a procedural paper texture (off-white/gray with text lines)
    /// </summary>
    /// <param name="width">Texture width in pixels</param>
    /// <param name="height">Texture height in pixels</param>
    /// <returns>A Texture2D that looks like a used document</returns>
    public static Texture2D Generate(int width = 256, int height = 256)
    {
        if (cachedTexture != null) return cachedTexture;

        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[width * height];

        // Base paper color: off-white / light beige-gray
        Color paperColor = new Color(0.78f, 0.74f, 0.68f); // warm gray-beige
        Color paperColor2 = new Color(0.74f, 0.70f, 0.64f); // slightly darker variant

        // Text color: dark gray (not pure black)
        Color textColor = new Color(0.2f, 0.18f, 0.15f);

        // Header stamp color: faded red
        Color stampColor = new Color(0.5f, 0.05f, 0.05f, 0.4f);

        System.Random rng = new System.Random(42); // fixed seed for consistency

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float u = (float)x / width;
                float v = (float)y / height;

                // Base color with subtle noise for paper texture
                float noise = (float)(rng.NextDouble() * 0.06 - 0.03);
                Color pixel = Color.Lerp(paperColor, paperColor2, Mathf.PerlinNoise(x * 0.05f, y * 0.05f) * 0.5f + 0.25f);
                pixel.r += noise;
                pixel.g += noise;
                pixel.b += noise;

                // === Draw text lines (simulated rows of text) ===
                // Text areas: rows of horizontal dashes at specific Y positions
                float[] textRows = new float[] {
                    0.15f, 0.20f, 0.25f, 0.30f, 0.35f,
                    0.42f, 0.47f, 0.52f, 0.57f, 0.62f,
                    0.70f, 0.75f, 0.80f
                };

                foreach (float row in textRows)
                {
                    float rowV = row;
                    float lineHeight = 0.025f;

                    if (v > rowV - lineHeight && v < rowV + lineHeight)
                    {
                        // Draw some text "words" as horizontal segments
                        float[] words = new float[] {
                            0.05f + (float)(rng.NextDouble() * 0.3),
                            0.45f + (float)(rng.NextDouble() * 0.5)
                        };

                        foreach (float wordStart in words)
                        {
                            float wordWidth = 0.05f + (float)(rng.NextDouble() * 0.15);
                            if (u > wordStart && u < wordStart + wordWidth)
                            {
                                // Subtle text with slight randomness
                                float textNoise = (float)(rng.NextDouble() * 0.05);
                                Color mixed = Color.Lerp(pixel, textColor, 0.7f + textNoise);
                                pixel = Color.Lerp(pixel, mixed, 1f - Mathf.Abs(v - rowV) * 20f);
                            }
                        }
                    }
                }

                // === Draw a header at the top ===
                if (v > 0.85f)
                {
                    // Confidential / Document header area
                    if (u > 0.15f && u < 0.55f && v > 0.88f && v < 0.93f)
                    {
                        pixel = Color.Lerp(pixel, textColor, 0.6f);
                    }
                    // Horizontal line under header
                    if (Mathf.Abs(v - 0.87f) < 0.005f && u > 0.05f && u < 0.95f)
                    {
                        pixel = Color.Lerp(pixel, textColor, 0.5f);
                    }
                }

                // === Draw a red "APPROVED" stamp at bottom-right ===
                if (v > 0.08f && v < 0.18f && u > 0.65f && u < 0.88f)
                {
                    // Circular stamp effect
                    float cx = 0.76f;
                    float cy = 0.13f;
                    float dist = Mathf.Sqrt((u - cx) * (u - cx) + (v - cy) * (v - cy));
                    if (dist < 0.07f && dist > 0.05f)
                    {
                        pixel = Color.Lerp(pixel, stampColor, 0.7f);
                    }
                    // Stamp inner text
                    if (dist < 0.045f)
                    {
                        pixel = Color.Lerp(pixel, stampColor, 0.4f);
                    }
                    // Stamp cross mark
                    if (Mathf.Abs(u - cx) < 0.02f && v > 0.05f && v < 0.21f)
                    {
                        pixel = Color.Lerp(pixel, stampColor, 0.5f);
                    }
                    if (Mathf.Abs(v - cy) < 0.02f && u > 0.55f && u < 0.97f)
                    {
                        pixel = Color.Lerp(pixel, stampColor, 0.5f);
                    }
                }

                // === Subtle folding crease ===
                if (Mathf.Abs(v - 0.38f) < 0.003f)
                {
                    pixel.r -= 0.03f;
                    pixel.g -= 0.03f;
                    pixel.b -= 0.03f;
                }

                // === Edge darkening (vignette) ===
                float edgeX = Mathf.Min(u, 1f - u);
                float edgeY = Mathf.Min(v, 1f - v);
                float edgeFade = Mathf.Min(edgeX / 0.08f, edgeY / 0.08f);
                edgeFade = Mathf.Clamp01(edgeFade);
                pixel *= Mathf.Lerp(0.7f, 1.0f, edgeFade);

                // Clamp
                pixel.r = Mathf.Clamp01(pixel.r);
                pixel.g = Mathf.Clamp01(pixel.g);
                pixel.b = Mathf.Clamp01(pixel.b);
                pixel.a = 1f;

                pixels[y * width + x] = pixel;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        cachedTexture = tex;
        return tex;
    }
}
