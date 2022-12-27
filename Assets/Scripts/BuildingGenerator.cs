using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingGenerator : MonoBehaviour
{
    public int maxPieces = 15;
    public float perlinScaleFactor = 6f;
    public PerlinGenerator perlinGenerator;
    public bool isInterior = false;
    public int randomVariationMin = 0;
    public int randomVariationMax = 0;
    public GameObject[] baseParts;
    public GameObject[] middleParts;
    public GameObject[] topParts;


    void Awake()
    {
        Build();
    }
    /*
    public void BuildTopOnly()
    {
        float sampledValue = PerlinGenerator.instance.PerlinSteppedPosition(transform.position);

        int targetPieces = Mathf.FloorToInt(maxPieces * (sampledValue));
        targetPieces += Random.Range(randomVariationMin, randomVariationMax);

        if (targetPieces <= 0)
        {
            return;
        }

        float heightOffset = 0.6f;

        for (int i = 2; i < targetPieces; i++)
        {
            heightOffset += 0.6f;
        }

        SpawnPieceLayer(topParts, heightOffset);
    }
    */
    public void Build()
    {
        float sampledValue = PerlinGenerator.instance.PerlinSteppedPosition(transform.position);

        int targetPieces = Mathf.FloorToInt(maxPieces * (sampledValue));
        targetPieces += Random.Range(randomVariationMin, randomVariationMax);

        if (targetPieces <= 0)
        {
            return;
        }

        float heightOffset = 0;

        SpawnPieceLayer(baseParts, heightOffset);
        heightOffset += 0.6f;

        for (int i = 2; i < targetPieces; i++)
        {
            SpawnPieceLayer(middleParts, heightOffset);
            heightOffset += 0.6f;
        }

        SpawnPieceLayer(topParts, heightOffset);
    }

    void SpawnPieceLayer(GameObject[] pieceArray, float inputHeight)
    {
        Transform randomTransform = pieceArray[Random.Range(0, pieceArray.Length)].transform;
        GameObject clone = Instantiate(randomTransform.gameObject, this.transform.position + new Vector3(0, inputHeight, 0), transform.rotation);

        clone.transform.SetParent(this.transform);
    }

}
