
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
[RequireComponent(typeof(Mask))]
public class CircleMaskSprite : MonoBehaviour
{
    [SerializeField]
    [Range(64, 512)]
    private int resolution = 256;

    private Texture2D generatedTexture;
    private Sprite generatedSprite;

    private void Awake()
    {
        GenerateCircle();
    }

    private void GenerateCircle()
    {
        generatedTexture = new Texture2D(
            resolution,
            resolution,
            TextureFormat.RGBA32,
            false
        );

        generatedTexture.filterMode =
            FilterMode.Bilinear;

        Color[] pixels =
            new Color[resolution * resolution];

        float center = (resolution - 1) * 0.5f;
        float radius = resolution * 0.5f - 1f;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float dx = x - center;
                float dy = y - center;

                float distance = Mathf.Sqrt(
                    dx * dx + dy * dy
                );

                float alpha = Mathf.Clamp01(
                    radius - distance
                );

                pixels[y * resolution + x] =
                    new Color(1f, 1f, 1f, alpha);
            }
        }

        generatedTexture.SetPixels(pixels);
        generatedTexture.Apply();

        generatedSprite = Sprite.Create(
            generatedTexture,
            new Rect(
                0,
                0,
                resolution,
                resolution
            ),
            new Vector2(0.5f, 0.5f)
        );

        Image image = GetComponent<Image>();

        image.sprite = generatedSprite;
        image.type = Image.Type.Simple;

        Mask mask = GetComponent<Mask>();
        mask.showMaskGraphic = false;
    }

    private void OnDestroy()
    {
        if (generatedSprite != null)
            Destroy(generatedSprite);

        if (generatedTexture != null)
            Destroy(generatedTexture);
    }
}
