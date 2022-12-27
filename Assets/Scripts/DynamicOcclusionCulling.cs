using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DynamicOcclusionCulling : MonoBehaviour
{
    private Camera cam;

    //The maximum distance at which objects will be considered for occlusion culling
    public float maxDistance = 100f;

    //Number of raycasts shot from the camera
    public int numRaycasts = 100;

    //Time after which objects will be hidden again
    public float unhideDelay = 6.0f; 

    List<GameObject> hitObjects;

    private Dictionary<GameObject, IEnumerator> hideObjects = new Dictionary<GameObject, IEnumerator>();

    void Awake()
    {
        hitObjects = new List<GameObject>();
        cam = GetComponent<Camera>();
    }
    void Start()
    {
        //Hide all buildings after 0.5 seconds since start
        foreach (GameObject obj in GameObject.FindGameObjectsWithTag("Cube"))
        {
            
                IEnumerator hideCoroutine = HideObjects(obj, 0.5f);
                hideObjects.Add(obj, hideCoroutine);
                if (hideObjects.ContainsKey(obj))
                {
                    //Update coroutine for existing object
                    hideObjects[obj] = hideCoroutine;
                }
                else
                {
                    //Add new object and coroutine to dictionary
                    hideObjects.Add(obj, hideCoroutine);
                }
                StartCoroutine(hideCoroutine);
        }
    }
    void Update()
    {
        //Get the position and direction of the camera
        Vector3 camPos = cam.transform.position;
        Vector3 camDir = cam.transform.forward;
        

        //Perform the specified number of random raycasts
        for (int i = 0; i < numRaycasts; i++)
        {
            float x = Random.Range(-1.0f, 1.0f);
            float y = Random.Range(-1.0f, 1.0f);
            float z = Random.Range(-1.0f, 1.0f);

            //Generate a random direction for the raycast
            Vector3 randDir = new Vector3(x, y, z).normalized;

            //Debug.DrawRay(camPos, (camDir + randDir)*100, Color.cyan, 0.5f);

            // Perform the raycast and get the hit object
            RaycastHit hit;
            if (Physics.Raycast(camPos, camDir + randDir, out hit, maxDistance))
            {
                //Unhide object that was hit by raycast and cancel execution of hiding courotine
                hit.collider.gameObject.GetComponent<Renderer>().enabled = true;
                hitObjects.Add(hit.collider.gameObject);

                if (hideObjects.ContainsKey(hit.collider.gameObject))
                {
                    StopCoroutine(hideObjects[hit.collider.gameObject]);
                }
            }
        }
        
        //Hide all objects that were not hit by any raycasts
        foreach (GameObject obj in GameObject.FindGameObjectsWithTag("Cube"))
        {
            if (hitObjects.Contains(obj))
            {
                IEnumerator hideCoroutine = HideObjects(obj, unhideDelay);
                if (hideObjects.ContainsKey(obj))
                {
                    //Update coroutine for existing object
                    hideObjects[obj] = hideCoroutine;
                }
                else
                {
                    //Add new object and coroutine to dictionary
                    hideObjects.Add(obj, hideCoroutine);
                }
                StartCoroutine(hideCoroutine);
            }
        }

        hitObjects.Clear();
    }
    public IEnumerator HideObjects(GameObject obj, float delay)
    {
        //Hide object after delay
        yield return new WaitForSeconds(delay);
        obj.GetComponent<Renderer>().enabled = false;
        
    }
}