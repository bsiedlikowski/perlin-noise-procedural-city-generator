using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PerlinGenerator : MonoBehaviour
{
    public static PerlinGenerator instance = null;

    public int perlinTextureSizeX = 256;
    public int perlinTextureSizeY = 256;
    public bool randomizeNoiseOffset = true;
    public Vector2 perlinOffset;
    public float noiseScale = 4f;
    public int perlinGridStepSizeX = 40;
    public int perlinGridStepSizeY = 40;

    private Texture2D perlinTexture;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    public void Generate()
    {
        //Generate random offset vector
        if (randomizeNoiseOffset)
        {
            perlinOffset = new Vector2(Random.Range(0, 99999), Random.Range(0, 99999));
        }

        perlinTexture = new Texture2D(perlinTextureSizeX, perlinTextureSizeY);

        //Set each pixel in perlinTexture to PerlinNoise value
        for (int x = 0; x < perlinTextureSizeX; x++)
        {
            for (int y = 0; y < perlinTextureSizeY; y++)
            {
                perlinTexture.SetPixel(x, y, SampleNoise(x, y));
            }
        }

        perlinTexture.Apply();
    }

    Color SampleNoise(int x, int y)
    {
        float xCoord = (float)x / perlinTextureSizeX * noiseScale + perlinOffset.x;
        float yCoord = (float)y / perlinTextureSizeY * noiseScale + perlinOffset.y;

        float sample = Mathf.PerlinNoise(xCoord, yCoord);
        Color perlinColor = new Color(sample, sample, sample);

        return perlinColor;
    }
 
    public float PerlinSteppedPosition(Vector3 worldPosition)
    {
        //Pick a pixel from perlinTexture based on building position in world
        int xToSample = Mathf.FloorToInt(worldPosition.x + perlinGridStepSizeX * 0.5f);
        int yToSample = Mathf.FloorToInt(worldPosition.z + perlinGridStepSizeY * 0.5f);

        xToSample = xToSample % perlinGridStepSizeX;
        yToSample = yToSample % perlinGridStepSizeY;

        int gridStepSizeX = perlinTextureSizeX / perlinGridStepSizeX;
        int gridStepSizeY = perlinTextureSizeY / perlinGridStepSizeY;

        float sampledValue = perlinTexture.GetPixel(
            (Mathf.FloorToInt(xToSample * gridStepSizeX)),
            (Mathf.FloorToInt(yToSample * gridStepSizeY))
            ).grayscale;

        return sampledValue;
    }
}
