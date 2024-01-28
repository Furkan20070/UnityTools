using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class AutoLevelGenerator : MonoBehaviour
{
    [System.Serializable]
    public struct PropAttributes
    {
        [Tooltip("GameObject of your level actor.")]
        public GameObject prop;

        [Tooltip("Offset for prop object or model so that it lands perfectly in the middle when spawned.")]
        public Vector3 objectOffset; //offset for prop object or model so that it lands perfectly in the middle when spawned.
        [Tooltip("Offset for prop rotation so that it aligns perfectly with track.")]
        public Vector3 objectRotationOffset; //offset for prop rotation so that it aligns perfectly with track.

        //Should we emplace props uniformly or discretely?
        //What this does when set to true is it makes the generator place the prop between propEmplacements,
        //instead of taking them as absolute positions. Make sure you only enter two to propEmplacements
        //after making this true.
        [Tooltip("Should we emplace props uniformly or discretely?\n What this does when set to true is it makes the generator place the prop between propEmplacements, instead of taking them as absolute positions. Make sure you only enter two values to propEmplacements after making this true.")]
        public bool uniformPosition;
        //These two lists of vector3 are random positions / rotations to apply when prop is spawned.
        //For example, if you have a level prop that can be replaced in left or right, you can add
        //(-1, 0, 0) and (1, 0, 0) to emplacements list and upon spawning, your object will randomly be 
        //placed in one of those positions. Keep in mind that this values are added along with objectOffset.
        //Same goes for rotations.
        [Tooltip("These two lists of vector3 are random positions / rotations to apply when prop is spawned. For example, if you have a level prop that can be replaced in left or right, you can add (-1, 0, 0) and (1, 0, 0) to emplacements list and upon spawning, your object will randomly be placed in one of those positions. Keep in mind that this values are added along with objectOffset. Same goes for rotations.")]
        public List<Vector3> propEmplacements;
        [Tooltip("Same as uniformPosition, but with rotations.")]
        public bool uniformRotation; //same as uniformPosition, but with rotations.
        [Tooltip("In Euler angles.")]
        public List<Vector3> propRotations; //in euler angles.

        //Disabled this property since it broke the whole thing in the initial attempt.
        /*//At max how many of these should be present in a level? Values <= 0 will count for no restrictions.
        [Tooltip("At max how many of these should be present in a level? Values <= 0 will count for no restrictions.")]
        public int maxNumber;*/

        [Tooltip("Should the next prop be spawned from a fixed distance? If set to true, next prop will spawn at exactly minSafeDistance")]
        public bool discreteSafeDistance;
        [Tooltip("Minimum safe distance between this prop and the prop to spawn next.")]
        public int minSafeDistance;
        [Tooltip("Maximum safe distance between this prop and the prop to spawn next.")]
        public int maxSafeDistance;
    }

    [Tooltip("Seed for the level. Feed this the number of the level player is supposed to be to load the same level over and over again.")]
    public int seed = 0;

    [SerializeField]
    [Tooltip("Start position for generator to run. This should be set to your runner's starting position.")]
    private int startLine = 0;

    [SerializeField]
    [Tooltip("This should be set to the end of the level in world units.")]
    private int endLine = 2000;

    [Tooltip("You usually want a little distance before level actors start spawning.")]
    [SerializeField]
    private int propSpawnOffset = 50;

    [Tooltip("This offset stops actor spawning beyond endLine - propSpawnOffset")]
    [SerializeField]
    private int levelEndOffset = 50;

    [Tooltip("Add your level actors with their specific attributes here.")]
    public List<PropAttributes> levelProps;

#if UNITY_EDITOR
    private List<GameObject> spawnedObjects = new List<GameObject>();
#endif

    void Start()
    {

    }

    bool CheckCredibility()
    {
        bool errorFound = false;
        if (startLine + propSpawnOffset > endLine - levelEndOffset)
        {
            errorFound = true;
            Debug.Log("Starting Point minus Prop Spawn Offset is bigger than Ending Point minus Level End Offset! This is not allowed.");
        }
        for (int i = 0; i < levelProps.Count; i++)
        {
            if (levelProps[i].prop == null)
            {
                errorFound = true;
                Debug.Log("One of the level props has a null gameobject! This is not allowed.");
            }
            if (levelProps[i].minSafeDistance == 0 && levelProps[i].maxSafeDistance == 0)
            {
                errorFound = true;
                Debug.Log("One of the level props has a 0 minSafeDistance and 0 maxSafeDistance. This will cause an infinite loop in generator and not allowed.");
            }
            if (levelProps[i].uniformPosition && levelProps[i].propEmplacements.Count != 2)
            {
                errorFound = true;
                Debug.Log("If you have Uniform Position or Uniform Rotation checked, then you should have exactly two entries in their respective categories");
            }
        }
        return errorFound;
    }

    void SpawnProps()
    {
#if UNITY_EDITOR
        for (int i = 0; i < spawnedObjects.Count; i++)
        {
            DestroyImmediate(spawnedObjects[i]);
        }
#endif
        int workingPoint = startLine + propSpawnOffset;
        int endingPoint = endLine - levelEndOffset;

        while (workingPoint < endingPoint)
        {
            int randomIndex = Random.Range(0, levelProps.Count);

            Vector3 objectPosition = new Vector3(0, 0, workingPoint) + levelProps[randomIndex].objectOffset;
            if (levelProps[randomIndex].propEmplacements.Count > 0)
            {
                if (levelProps[randomIndex].uniformPosition)
                {
                    Vector3 helper = levelProps[randomIndex].propEmplacements[1] - levelProps[randomIndex].propEmplacements[0];
                    helper *= Random.value;
                    objectPosition += levelProps[randomIndex].propEmplacements[0] + helper;
                }
                else
                {
                    objectPosition += levelProps[randomIndex].propEmplacements[Random.Range(0, levelProps[randomIndex].propEmplacements.Count)];
                }
            }

            Vector3 objectRotation = Vector3.zero;
            if (levelProps[randomIndex].propRotations.Count > 0)
            {
                if (levelProps[randomIndex].uniformRotation)
                {
                    Vector3 helper2 = levelProps[randomIndex].propRotations[0] - levelProps[randomIndex].propRotations[1];
                    helper2 *= Random.value;
                    objectRotation += levelProps[randomIndex].propRotations[0] + helper2;
                }
                else
                {
                    objectRotation += levelProps[randomIndex].propRotations[Random.Range(0, levelProps[randomIndex].propRotations.Count)];
                }
            }
            Quaternion rotationFinal = Quaternion.Euler(objectRotation);

            if (levelProps[randomIndex].discreteSafeDistance)
            {
                workingPoint += levelProps[randomIndex].minSafeDistance;
            }
            else
            {
                int uniformSafeDistance = Random.Range(levelProps[randomIndex].minSafeDistance, levelProps[randomIndex].maxSafeDistance + 1);
                workingPoint += uniformSafeDistance;
            }
  
            GameObject spawned = Instantiate(levelProps[randomIndex].prop, objectPosition, rotationFinal, null);

#if UNITY_EDITOR
            spawnedObjects.Add(spawned);
#endif
        }
    }

    void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            Random.InitState(seed);
            if (!CheckCredibility())
            {
                SpawnProps();
            }
            else
            {
                Debug.Log("There are errors in your prop configuration. Level generation stopped.");
            }
        }   
#endif
    }
}
