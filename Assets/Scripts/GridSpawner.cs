using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridSpawner : MonoBehaviour
{

    public int gridX;
    public int gridZ;
    public GameObject prefabToSpawn;
    public GameObject plane;
    public Vector3 gridOrigin = Vector3.zero;
    public float gridOffset = 1f;
    public GameObject planesCombined;


    void OnEnable()
    {
        Generate();
        
    }

    public void Generate()
    {
        //Add a square plane as a basis of buildings cluster
        planesCombined = GameObject.Find("Planes Combined");
        GameObject planego = Instantiate(plane, transform.position, transform.rotation);
        planego.transform.SetParent(planesCombined.transform);

        SpawnGrid();      
    }


    void SpawnGrid()
    {
        //For each part in grid Instantiate a new building
        for (float x = -gridX/2.0f + 0.5f; x <= gridX/2.0f - 0.5f; x++)
        {
            for (float z = -gridZ/2.0f + 0.5f; z <= gridZ/2.0f - 0.5f; z++)
            {
                GameObject clone = Instantiate(prefabToSpawn, 
                    transform.position + gridOrigin + new Vector3(gridOffset * x, 0, gridOffset * z), transform.rotation);
                
                //Mark buildings that are inside of a grid
                if (z != -4 && x != -4 && z != 4 && x != 4)
                {
                    clone.GetComponent<BuildingGenerator>().isInterior = true;
                }
                
                clone.transform.SetParent(this.transform);

            }
        }
    }

    
}
