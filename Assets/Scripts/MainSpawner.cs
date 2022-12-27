using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
class RoadPiece: IEquatable<RoadPiece>
{
    public Vector3Int position;
    public MainSpawner.RoadType type;
    public int yRotation;
    public GameObject road;
    public bool Equals(RoadPiece other)
    {
        return ((position == other.position && type == other.type && Mathf.Abs(yRotation) == Mathf.Abs(other.yRotation)) ||
            position == other.position && type == MainSpawner.RoadType.STRAIGHT && other.type == MainSpawner.RoadType.STRAIGHT &&
            Mathf.Abs(yRotation - other.yRotation) == 180);
    }
}

public class MainSpawner : MonoBehaviour
{
    public GameObject crawler;
    public GameObject crossroad;
    public GameObject tjunc;
    public GameObject straight;
    public GameObject corner;

    public GameObject house;
    public GameObject planeStrip;

    public PerlinGenerator perlinGenerator;

    public LayerMask layerMask;

    public GameObject planesCombined;
    public GameObject roadsCombined;
    public GameObject buildingsCombined;
    public GameObject[] test;

    Vector3Int minDimensions = Vector3Int.zero;
    Vector3Int maxDimensions = Vector3Int.zero;
    public enum RoadType { STRAIGHT, CROSS, CORNER, TJUNC };

    List<RoadPiece> roadPieces = new List<RoadPiece>();

    Vector3Int crawlerPos;

    Vector3 dir = new Vector3(0, 0, 1);

    void Start()
    {
        perlinGenerator.Generate();
        for (int counter = 0; counter < 5; counter++)
        {
            Crawl();
            counter++;
        }
        FixRoads();
        BuildHouses();
        FillGaps();
        CombineMeshes(planesCombined);
        
        CombineMeshesWithSubmeshes(roadsCombined);
        
        foreach (Transform cluster in buildingsCombined.transform)
        {
            foreach (Transform building in cluster.transform) 
            {
                if (!building.GetComponent<BuildingGenerator>().isInterior)
                    CombineMeshesWithSubmeshes(building.gameObject);
            }
        }
        

    }

    void Crawl()
    {
        int randomTurn = UnityEngine.Random.Range(0, 3);
        float rot;
        GameObject go;
        RoadPiece newRoad;
        if (randomTurn == 0) //turn left
        {
            //rotate current crawler direction vector by -90
            dir = Quaternion.Euler(0, -90, 0) * dir;

            //instatiation of a corner rotated by the angle between default direction (0,0,1) and current direction(dir)
            rot = Vector3.SignedAngle(new Vector3(0, 0, 1), dir, Vector3.up) + 90;
            go = Instantiate(corner, crawlerPos, Quaternion.Euler(0, rot, 0), roadsCombined.transform);

            newRoad = new RoadPiece { position = crawlerPos, type = RoadType.CORNER, yRotation = (int)Mathf.Round(go.transform.rotation.eulerAngles.y), road = go };             
        }
        else if (randomTurn == 1) //turn right
        {
            //rotate current crawler direction vector by 90
            dir = Quaternion.Euler(0, 90, 0) * dir;

            //instatiation of a corner rotated by the angle between default direction (0,0,1) and current direction(dir)
            rot = Vector3.SignedAngle(new Vector3(0, 0, 1), dir, Vector3.up) + 180;
            go = Instantiate(corner, crawlerPos, Quaternion.Euler(0, rot, 0), roadsCombined.transform);

            newRoad = new RoadPiece { position = crawlerPos, type = RoadType.CORNER, yRotation = (int)Mathf.Round(go.transform.rotation.eulerAngles.y), road = go };
        }
        else //go straight
        {
            //instatiation of a straight road rotated by the angle between default direction (0,0,1) and current direction(dir)
            rot = Vector3.SignedAngle(new Vector3(0, 0, 1), dir, Vector3.up);
            go = Instantiate(straight, crawlerPos, Quaternion.Euler(0, rot, 0), roadsCombined.transform);

            newRoad = new RoadPiece { position = crawlerPos, type = RoadType.STRAIGHT, yRotation = (int)Mathf.Round(go.transform.rotation.eulerAngles.y), road = go };
        }
        AddNoDuplicates(newRoad);

        //instatiation of a straight road piece after every other piece to fill the gaps
        Vector3Int straightPos = crawlerPos + Vector3Int.RoundToInt(dir * 10);
        rot = Vector3.SignedAngle(new Vector3(0, 0, 1), dir, Vector3.up);
        go = Instantiate(straight, straightPos, Quaternion.Euler(0, rot, 0), roadsCombined.transform);

        newRoad = new RoadPiece { position = straightPos, type = RoadType.STRAIGHT, yRotation = (int)Mathf.Round(go.transform.rotation.eulerAngles.y), road = go };
        AddNoDuplicates(newRoad);

        crawlerPos += Vector3Int.RoundToInt(dir * 20);

        crawler.transform.position = crawlerPos;

        //find where the extents are
        if (minDimensions.x > crawlerPos.x)
            minDimensions.x = crawlerPos.x;

        if (minDimensions.z > crawlerPos.z)
            minDimensions.z = crawlerPos.z;

        if (maxDimensions.x < crawlerPos.x)
            maxDimensions.x = crawlerPos.x;

        if (maxDimensions.z < crawlerPos.z)
            maxDimensions.z = crawlerPos.z;
    }
    void AddNoDuplicates(RoadPiece newPiece)
    {
        //destroy road piece if an equal road piece element already exists (equal means same road type, position and rotation)
        if (!roadPieces.Contains(newPiece))
        {
            roadPieces.Add(newPiece);
        }
        else
        {
            DestroyImmediate(newPiece.road);
        }
    }
    void FixRoads() 
    {
        ILookup<Vector3Int, RoadPiece> lookup = roadPieces.ToLookup(p => p.position, p => p);

        //swap overlapping elements with suitable single element (crossroad or tjunction)
        foreach (IGrouping<Vector3Int, RoadPiece> roadGroup in lookup)
        {
            if (roadGroup.Count() > 1) 
            {
                int straightCount = roadGroup.Count(p => p.type == RoadType.STRAIGHT);

                // 2 straights in the same position
                if (straightCount == 2) 
                {
                    Instantiate(crossroad, roadGroup.Key, Quaternion.identity, roadsCombined.transform);
                }

                // 1 coorner and 1 straight in the same position
                if (straightCount == 1) 
                {
                    IOrderedEnumerable<RoadPiece> roadGroupByOrder = roadGroup.OrderBy(p => p.type);

                    if (roadGroupByOrder.First().yRotation == 0 || roadGroupByOrder.First().yRotation == 180)
                    {
                        if (roadGroupByOrder.Last().yRotation == 0 || roadGroupByOrder.Last().yRotation == 90)
                        {
                            Instantiate(tjunc, roadGroup.Key, Quaternion.identity, roadsCombined.transform);
                        }
                        else
                        {
                            Instantiate(tjunc, roadGroup.Key, Quaternion.Euler(0, 180, 0), roadsCombined.transform);

                        }
                    }
                    else if (roadGroupByOrder.First().yRotation == 90 || roadGroupByOrder.First().yRotation == 270)
                    {
                        if (roadGroupByOrder.Last().yRotation == 0 || roadGroupByOrder.Last().yRotation == 270)
                        {
                            Instantiate(tjunc, roadGroup.Key, Quaternion.Euler(0, -90, 0), roadsCombined.transform);
                        }
                        else
                        {
                            Instantiate(tjunc, roadGroup.Key, Quaternion.Euler(0, 90, 0), roadsCombined.transform);
                        }
                    }                

                }

                // 2 corners in the same position
                if (straightCount == 0) 
                {
                    if (roadGroup.Any(p => p.yRotation == 180))
                    {
                        if (roadGroup.Any(p => p.yRotation == 0))
                        {
                            Instantiate(crossroad, roadGroup.Key, Quaternion.identity, roadsCombined.transform);
                        }
                        else if (roadGroup.Any(p => p.yRotation == 90))
                        {
                            Instantiate(tjunc, roadGroup.Key, Quaternion.Euler(0, 90, 0), roadsCombined.transform);
                        }
                        else if (roadGroup.Any(p => p.yRotation == 270))
                        {
                            Instantiate(tjunc, roadGroup.Key, Quaternion.Euler(0, 180, 0), roadsCombined.transform);
                        }
                    }
                    else if (roadGroup.Any(p => p.yRotation == 270))
                    {
                        if (roadGroup.Any(p => p.yRotation == 90))
                        {
                            Instantiate(crossroad, roadGroup.Key, Quaternion.identity, roadsCombined.transform);
                        }
                        else if (roadGroup.Any(p => p.yRotation == 0))
                        {
                            Instantiate(tjunc, roadGroup.Key, Quaternion.Euler(0, -90, 0), roadsCombined.transform);
                        }
                    }
                    else
                    {
                        Instantiate(tjunc, roadGroup.Key, Quaternion.identity, roadsCombined.transform);
                    }


                }

                //destroy old road piece elements
                foreach (RoadPiece r in roadGroup) 
                {
                    DestroyImmediate(r.road);
                }
            }

        }
    }   
    void BuildHouses()
    {
        Collider[] colliders = new Collider[1];
        for (float z = (float)minDimensions.z - 8.75f; z < maxDimensions.z + 11.25f; z = z + 0.5f)
        {
            for (float x = (float)minDimensions.x - 8.75f; x < maxDimensions.x + 11.25f; x = x + 0.5f)
            {
                Vector3 pos = new Vector3(x, 0, z);
                RaycastHit hit;

                //check if there is a any road (besides corners) in one of the 4 directions
                if (Physics.Raycast(pos - new Vector3(0, 0, 0 - 4.75f), Vector3.forward, out hit, 0.16f, ~layerMask) ||
                    Physics.Raycast(pos - new Vector3(0, 0, 0 + 4.75f), -Vector3.forward, out hit, 0.16f, ~layerMask) ||
                    Physics.Raycast(pos - new Vector3(0 + 4.75f, 0, 0), -Vector3.right, out hit, 0.16f, ~layerMask) ||
                    Physics.Raycast(pos - new Vector3(0 - 4.75f, 0, 0), Vector3.right, out hit, 0.16f, ~layerMask))
                {
                    //check if there is a place to instantiate a plane square
                    int numberOfColliders = Physics.OverlapBoxNonAlloc(pos, new Vector3(4.74f, 0.51f, 4.74f), colliders);
                    if (numberOfColliders == 0)
                    {
                        GameObject go = Instantiate(house, pos, Quaternion.identity, buildingsCombined.transform);
                        go.transform.LookAt(hit.point);
                        go.transform.Rotate(0, -90, 0);
                    }
                }
            }
        }
    }
    void FillGaps()
    {
        Collider[] collidersX = new Collider[1];
        Collider[] collidersZ = new Collider[1];
        for (float z = (float)minDimensions.z - 8.75f; z < maxDimensions.z + 11.25f; z = z + 0.5f)
        {
            for (float x = (float)minDimensions.x - 8.75f; x < maxDimensions.x + 11.25f; x = x + 0.5f)
            {
                Vector3 pos = new Vector3(x, 0, z);
                //check if there is any road in one of the 4 directions
                if (Physics.Raycast(pos - new Vector3(0, 0, 0 - 4.74f), Vector3.forward, 0.3f) || 
                    Physics.Raycast(pos - new Vector3(0, 0, 0 + 4.74f), -Vector3.forward, 0.3f) ||
                    Physics.Raycast(pos - new Vector3(0 + 4.74f, 0, 0), -Vector3.right, 0.3f) ||
                    Physics.Raycast(pos - new Vector3(0 - 4.74f, 0, 0), Vector3.right, 0.3f))
                {
                    //check if there is a place to instantiate a plane strip
                    int numberOfCollidersZ = Physics.OverlapBoxNonAlloc(pos, new Vector3(0.24f, 1, 4.74f), collidersZ);
                    int numberOfCollidersX = Physics.OverlapBoxNonAlloc(pos, new Vector3(4.74f, 1, 0.24f), collidersX);

                    if (numberOfCollidersZ == 0)
                    {
                        GameObject go = Instantiate(planeStrip, pos, Quaternion.identity);
                        go.transform.SetParent(planesCombined.transform);
                      
                    }
                    else if (numberOfCollidersX == 0)
                    {
                        GameObject go = Instantiate(planeStrip, pos, Quaternion.Euler(0, 90, 0));
                        go.transform.SetParent(planesCombined.transform);


                    }
                }
            }
        }
    }
    public void CombineMeshes(GameObject obj)
    {
        //Temporarily set position to zero to make matrix math easier
        Vector3 position = obj.transform.position;
        obj.transform.position = Vector3.zero;

        //Get all mesh filters and combine
        MeshFilter[] meshFilters = obj.GetComponentsInChildren<MeshFilter>();
        CombineInstance[] combine = new CombineInstance[meshFilters.Length];
        int i = 1;
        while (i < meshFilters.Length)
        {
            combine[i].mesh = meshFilters[i].sharedMesh;
            combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
            Destroy(meshFilters[i].gameObject);
            i++;
        }

        obj.transform.GetComponent<MeshFilter>().mesh = new Mesh();
        obj.transform.GetComponent<MeshFilter>().mesh.CombineMeshes(combine, true, true);
        obj.transform.gameObject.SetActive(true);

        //Return to original position
        obj.transform.position = position;
    }
    public void CombineMeshesWithSubmeshes(GameObject obj)
    {
        //Temporarily set position to zero to make matrix math easier
        Vector3 position = obj.transform.position;
        obj.transform.position = Vector3.zero;

        //Get all materials that combined mesh will have
        Material[] materials = obj.GetComponent<MeshRenderer>().sharedMaterials;

        
        CombineInstance[] finalCombine = new CombineInstance[materials.Length];

        //Get all mesh filters
        MeshFilter[] meshFilters = obj.GetComponentsInChildren<MeshFilter>();

        //Create an array of every mesh filter for each element of all materials array  
        CombineInstance[][] combine = new CombineInstance[materials.Length][];
        for (int x = 0; x < materials.Length; x++)
        {
            combine[x] = new CombineInstance[meshFilters.Length];
        }

        //Assign every submesh of every mesh filter to combine[] array 
        int i = 1;
        while (i < meshFilters.Length)
        {
            //Get all materials of current mesh filter
            Material[] localMaterials = meshFilters[i].GetComponent<MeshRenderer>().sharedMaterials;

            //Compare each material to all materials of current mesh filter
            int j = 0;
            while (j < materials.Length)
            {
                for (int localMaterialIndex = 0; localMaterialIndex < localMaterials.Length; localMaterialIndex++)
                {
                    if (localMaterials[localMaterialIndex] == materials[j])
                    {
                        combine[j][i].mesh = meshFilters[i].sharedMesh;
                        combine[j][i].subMeshIndex = localMaterialIndex;
                        combine[j][i].transform = meshFilters[i].transform.localToWorldMatrix;
                    }
                }
                j++;
            }
            
            DestroyImmediate(meshFilters[i].gameObject);
            i++;

        }

        //Combine all submeshes with the same material of all mesh filters 
        for (int combineIndex = 0; combineIndex < combine.Length; combineIndex++)
        {

            Mesh mesh = new Mesh {indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.CombineMeshes(combine[combineIndex], true, true);
            finalCombine[combineIndex].mesh = mesh;
            finalCombine[combineIndex].transform = Matrix4x4.identity;
        }

        //Combine all combined submeshes
        obj.transform.GetComponent<MeshFilter>().mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        obj.transform.GetComponent<MeshFilter>().mesh.CombineMeshes(finalCombine, false, true);
        obj.transform.GetComponent<MeshFilter>().mesh.Optimize();
        obj.transform.gameObject.SetActive(true);

        //Return to original position
        obj.transform.position = position;

        obj.AddComponent<BoxCollider>().size = new Vector3(1, obj.transform.GetComponent<MeshFilter>().mesh.bounds.size.y, 1);

    }
}
