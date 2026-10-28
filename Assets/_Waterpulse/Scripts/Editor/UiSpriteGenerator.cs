using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using static Waterpulse.EditorTools.MatchSceneBuilder;

namespace Waterpulse.EditorTools
{
    // Genera las formas de la interfaz (panel redondeado, su borde, cápsula para barras e iconos) como PNG blancos en
    // Art/UI, para teñirlas desde cada Image. Solo crea las que faltan: un dibujo mejor con el mismo nombre no se pisa.
    // Las importa como Sprite y, las que se estiran, con sus bordes de 9-slice. Lo llaman los generadores de escena
    public static class UiSpriteGenerator
    {
        public const string SpriteDir = RootDir + "/Art/UI";
        public const string Rounded = "rounded";     // paneles y botones: esquinas de 16 px
        public const string Ring = "ring";           // borde fino de un panel redondeado
        public const string Pill = "pill";           // barras: extremos redondos
        public const string Bolt = "icon_bolt";      // energía
        public const string Wave = "icon_wave";      // estamina
        public const string Arrow = "icon_arrow";    // flecha hacia la portería
        public const string Chevron = "icon_chevron";   // flecha hacia abajo sobre quien lleva la pelota
        public const string Corner = "corner";          // esquina de las marcas del destino del pase o del cambio
        public const string ControlArcs = "control_arcs";   // tres arcos y una punta en el agua bajo el jugador controlado
        const int Supersample = 4;                   // muestras por lado de cada píxel (bordes suaves)
        const float CornerRadius = 16f;
        const float RingThickness = 3f;
        // Arcos del jugador controlado (textura de 256 px; la punta, arriba, es hacia donde mira)
        const float ArcsRadius = 100f;
        const float ArcsThickness = 8f;
        const float ArcsGapDegrees = 26f;
        const float CornerThickness = 5f;

        class Shape
        {
            public string Name;
            public int Size;
            public int Border;                               // 9-slice (0 = no se estira)
            public Func<float, float, bool> Inside;          // punto (x, y) en píxeles, y hacia arriba
        }

        static readonly Shape[] Shapes =
        {
            new Shape { Name = Rounded, Size = 64, Border = 20, Inside = (x, y) => RoundRect(x, y, 64f, CornerRadius) <= 0f },
            new Shape { Name = Ring, Size = 64, Border = 20, Inside = (x, y) => RoundRect(x, y, 64f, CornerRadius) is float d && d <= 0f && d >= -RingThickness },
            new Shape { Name = Pill, Size = 34, Border = 16, Inside = (x, y) => RoundRect(x, y, 34f, 16f) <= 0f },
            new Shape { Name = Bolt, Size = 64, Inside = (x, y) => InPolygon(x, y, BoltPoints) },
            new Shape { Name = Wave, Size = 64, Inside = (x, y) => OnWave(x, y, 42f) || OnWave(x, y, 22f) },
            new Shape { Name = Arrow, Size = 64, Inside = (x, y) => InPolygon(x, y, ArrowPoints) },
            new Shape { Name = Chevron, Size = 64, Inside = (x, y) => InPolygon(x, y, ChevronPoints) },
            new Shape { Name = Corner, Size = 32, Inside = (x, y) => x <= CornerThickness || y >= 32f - CornerThickness },
            new Shape { Name = ControlArcs, Size = 256, Inside = OnControlArcs },
        };

        static readonly Vector2[] BoltPoints = { new Vector2(40f, 62f), new Vector2(14f, 29f), new Vector2(30f, 29f), new Vector2(21f, 2f), new Vector2(51f, 37f), new Vector2(35f, 37f), new Vector2(53f, 62f) };
        static readonly Vector2[] ArrowPoints = { new Vector2(14f, 8f), new Vector2(58f, 32f), new Vector2(14f, 56f) };
        static readonly Vector2[] ChevronPoints = { new Vector2(8f, 50f), new Vector2(56f, 50f), new Vector2(32f, 14f) };
        static readonly Vector2[] ArcsPointer = { new Vector2(114f, 234f), new Vector2(142f, 234f), new Vector2(128f, 254f) };

        [MenuItem("Waterpulse/Generate UI Sprites", priority = 3)]
        static void GenerateFromMenu()
        {
            int created = EnsureSprites();
            Debug.Log($"[Waterpulse] Formas de la interfaz en {SpriteDir}: {created} nuevas (las que ya existían no se tocan).");
        }

        public static IEnumerable<string> SpriteNames()
        {
            foreach (var shape in Shapes) yield return shape.Name;
        }

        public static string PathOf(string name) => $"{SpriteDir}/{name}.png";

        public static Sprite Load(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(PathOf(name));

        // Dibuja las que faltan y deja todas importadas como Sprite. Devuelve cuántas ha creado
        public static int EnsureSprites()
        {
            EnsureFolder(SpriteDir);
            int created = 0;
            foreach (var shape in Shapes)
            {
                string path = PathOf(shape.Name);
                if (!File.Exists(path))
                {
                    File.WriteAllBytes(path, Render(shape));
                    AssetDatabase.ImportAsset(path);
                    created++;
                }
                ConfigureImporter(path, shape.Border);
            }
            return created;
        }

        // Solo la primera vez (si aún no es un Sprite): los ajustes que alguien cambie a mano se respetan
        static void ConfigureImporter(string path, int border)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null || importer.textureType == TextureImporterType.Sprite) return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = new Vector4(border, border, border, border);
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        // Blanco con la transparencia según cuánto del píxel cae dentro de la forma
        static byte[] Render(Shape shape)
        {
            int n = shape.Size;
            var pixels = new Color32[n * n];
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                int hits = 0;
                for (int sy = 0; sy < Supersample; sy++)
                for (int sx = 0; sx < Supersample; sx++)
                    if (shape.Inside(x + (sx + 0.5f) / Supersample, y + (sy + 0.5f) / Supersample)) hits++;
                byte alpha = (byte)Mathf.RoundToInt(255f * hits / (Supersample * Supersample));
                pixels[y * n + x] = new Color32(255, 255, 255, alpha);
            }
            var texture = new Texture2D(n, n, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            texture.Apply();
            byte[] png = texture.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(texture);
            return png;
        }

        // Distancia con signo a un cuadrado de lado "size" con esquinas de radio "radius" (negativa dentro)
        static float RoundRect(float x, float y, float size, float radius)
        {
            float half = size * 0.5f;
            float qx = Mathf.Abs(x - half) - (half - radius);
            float qy = Mathf.Abs(y - half) - (half - radius);
            float outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude;
            return outside + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
        }

        static bool InPolygon(float x, float y, Vector2[] points)
        {
            bool inside = false;
            for (int i = 0, j = points.Length - 1; i < points.Length; j = i++)
            {
                Vector2 a = points[i], b = points[j];
                if ((a.y > y) != (b.y > y) && x < (b.x - a.x) * (y - a.y) / (b.y - a.y) + a.x) inside = !inside;
            }
            return inside;
        }

        // Tres arcos alrededor del centro con un hueco delante (arriba), a 120° entre sí, y una punta en el hueco de delante
        static bool OnControlArcs(float x, float y)
        {
            if (InPolygon(x, y, ArcsPointer)) return true;
            float dx = x - 128f, dy = y - 128f;
            if (Mathf.Abs(Mathf.Sqrt(dx * dx + dy * dy) - ArcsRadius) > ArcsThickness * 0.5f) return false;
            float angle = Mathf.Repeat(Mathf.Atan2(dy, dx) * Mathf.Rad2Deg - 90f, 120f);   // 0 = centro de un hueco
            return angle > ArcsGapDegrees * 0.5f && angle < 120f - ArcsGapDegrees * 0.5f;
        }

        // Onda de grosor fijo entre x = 6 y x = 58, con los extremos redondos
        static bool OnWave(float x, float y, float centerY)
        {
            const float start = 6f, end = 58f, amplitude = 6f, wavelength = 26f, thickness = 7f;
            float WaveY(float px) => centerY + amplitude * Mathf.Sin((px - start) * 2f * Mathf.PI / wavelength);
            if (x < start || x > end)
            {
                float cx = x < start ? start : end;
                return new Vector2(x - cx, y - WaveY(cx)).magnitude <= thickness * 0.5f;
            }
            float slope = amplitude * 2f * Mathf.PI / wavelength * Mathf.Cos((x - start) * 2f * Mathf.PI / wavelength);
            return Mathf.Abs(y - WaveY(x)) <= thickness * 0.5f * Mathf.Sqrt(1f + slope * slope);
        }
    }
}
