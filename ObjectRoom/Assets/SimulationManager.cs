using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Simulation;
using UnityEngine;
using Random = UnityEngine.Random;
using UnityEngine.Perception.Randomization.Randomizers.Utilities;
using Unity.Mathematics;
using UnityEngine.Perception.Randomization.Samplers;
using Unity.Collections; 

public class SimulationManager : MonoBehaviour
{
    public GameObject camera;

    [Range(1,10)]
    public int NumberOfObjectsMin;
    [Range(2, 20)]
    public int NumberOfObjectsMax;
    
    public GameObject[] StageElements;
    public GameObject[] RandomObjects;
    public Material RandMat;
    [Range(1,10)]
    public float SpawnAreaX = 3f;
    [Range(1, 10)]
    public float SpawnAreaY = 3f;

    private float Dist;

    private int NumOfObj;

    List<Vector2> newPosList = new List<Vector2>();
    List<GameObject> gameList = new List<GameObject>();

    [Serializable]
    private class ObjectPosition {
        public float[] position = new float[3]; 
        public String type;
        public int index;
    }
    [Serializable]
    private class SingleFrame {
        public List<ObjectPosition> foregroundObjects = new List<ObjectPosition>();
        public List<ObjectPosition> backgroundObjects = new List<ObjectPosition>();
        public int index;
    }
     
    [Serializable]
    private class FullContent{
        public List<SingleFrame> contentList;
    }
    int frameCounter = 0;

    void Start()
    {
        NumOfObj = Random.Range(NumberOfObjectsMin, NumberOfObjectsMax);
        gameList.Clear();

        foreach (var element in StageElements)
        {
            WriteToFile(element.name + "\t" + 
                        camera.transform.InverseTransformPoint(element.transform.position), "walls_to_camera.txt");
        }
        
    }

    void Update()
    {
        // camera.transform.localRotation = Quaternion.Euler(30, Random.Range(-30f,30f), 0);
        // DestroyAll();
        // RandomizeStage();
        // NativeList<float2> placementSamples = NewPositions();
        // SpawnObjects(NumOfObj, placementSamples);

        if (frameCounter % (gameList.Count + StageElements.Length + 1) == 0)
        {
            camera.transform.localRotation = Quaternion.Euler(30, Random.Range(-30f,30f), 0);    
            DestroyAll();
            RandomizeStage();
            NativeList<float2> placementSamples = NewPositions();
            SpawnObjects(NumOfObj, placementSamples);
        }
    
        if(gameList.Count != NumOfObj){
            throw(new Exception("Game List and NumOfObj have incompatible number of objects, " + gameList.Count + "!=" + NumOfObj ));
        }
        if(!(frameCounter % (gameList.Count + StageElements.Length + 1) == 0)){
            SwitchActiveObject();
        }
        else{
            foreach (var obj in gameList)
            {
                obj.SetActive(true);
            }  
            foreach (var obj in StageElements)
            {
                obj.SetActive(true);
            }            
        }
        frameCounter++;
    }

    void SwitchActiveObject()
    {
        foreach (var obj in gameList)
        {
            obj.SetActive(false);
        }
        for (int i = 0; i < StageElements.Length; i++)
        {
            StageElements[i].SetActive(false);
        }
        try{
            //when removing objects
            int current_index = frameCounter % (gameList.Count + StageElements.Length + 1);

            if(current_index <= gameList.Count){
                gameList[current_index - 1].SetActive(true);
            }
            else{
                StageElements[current_index - gameList.Count - 1].SetActive(true);
            }
        }
        catch(ArgumentOutOfRangeException){
            Debug.Log("________________________________________________-");
            Debug.Log("Oh boi, found an exception, here's some data");
            Debug.Log("frameCounter mod numofobj " + frameCounter % NumOfObj);
            Debug.Log("frameCounter "  + frameCounter);
            Debug.Log("NumofObj " + NumOfObj);
            Debug.Log("objlist " + gameList.Count);
        }
    }
 

    private void WriteToFile(string text, string fileName)
    {
        try
        {
            var filepath = Path.Combine(Manager.Instance.GetDirectoryFor(DataCapturePaths.Logs), fileName);
            using (var writer = File.AppendText(filepath))
            {
                writer.Write(text + Environment.NewLine);
            }
        }catch (Exception e)
        {
            Log.E("UpdateHeartbeat.Write exception : " + e.ToString());
        }
    }

    private void WriteToJSONFile(SingleFrame singleFrame, string fileName)
    {
        try
        {
            var filepath = Path.Combine(Manager.Instance.GetDirectoryFor(DataCapturePaths.Logs), fileName);

            FullContent singleRun = null;
            if(File.Exists(filepath)){
                string fileContents = File.ReadAllText(filepath);

                singleRun =  JsonUtility.FromJson<FullContent>(fileContents);
            }
            if(singleRun == null){
                singleRun = new FullContent();
                singleRun.contentList = new List<SingleFrame>();
            }
            singleRun.contentList.Add(singleFrame);
            string jsonString = JsonUtility.ToJson(singleRun, true);
            File.WriteAllText(filepath, jsonString);
        }catch (Exception e)
        {
            Log.E("UpdateHeartbeat.Write exception : " + e.ToString());
        }
    }

    private SingleFrame MakeFrameElement()
    {
        SingleFrame singleFrame = new SingleFrame();
        singleFrame.index = frameCounter;
        //Adding foreground elements
        for (int i = 0; i < gameList.Count; i++)
        {
            Vector3 objPosVector = gameList[i].transform.position;  
            objPosVector = camera.transform.InverseTransformPoint(objPosVector);
            ObjectPosition objPos = new ObjectPosition();
            objPos.position = new float[] {objPosVector.x, objPosVector.y, objPosVector.z};

            objPos.index = i;
            objPos.type = gameList[i].name;
            singleFrame.foregroundObjects.Add(objPos); 
        }

        //Adding background elements
        for (int i = 0; i < StageElements.Length; i++)
        {
            Vector3 objPosVector = StageElements[i].transform.position;  
            objPosVector = camera.transform.InverseTransformPoint(objPosVector);
            ObjectPosition objPos = new ObjectPosition();
            objPos.position = new float[] {objPosVector.x, objPosVector.y, objPosVector.z};

            objPos.index = i;
            objPos.type = StageElements[i].name;
            singleFrame.backgroundObjects.Add(objPos); 
        }

        return singleFrame;
    }

    void DestroyAll()
    {
        for (int i = 0; i < gameList.Count; i++)
        {
            Destroy(gameList[i]);
        }
        gameList.Clear();
    }
    NativeList<float2> NewPositions()
    {
        NumOfObj = Random.Range(NumberOfObjectsMin, NumberOfObjectsMax);
        // Dist = Mathf.Sqrt((SpawnAreaX * SpawnAreaY) / NumOfObj);
        Dist = Mathf.Sqrt(2f);
        var seed = SamplerState.NextRandomState();
        // newPosList = PoissonDiscSampling.GeneratePoints(Dist, new Vector2(SpawnAreaX, SpawnAreaY), NumOfObj);
        NativeList<float2> placementSamples = PoissonDiskSampling.GenerateSamples(
                SpawnAreaX, SpawnAreaY, Dist, seed);
        // Debug.Log("placementSamples size is " + placementSamples.Length);
        // Debug.Log("Number of objects we need is " + NumOfObj);
        // newPosList = PoissonDiscSampling.GeneratePointsInRestrictedViewPort(Dist, new Vector2(SpawnAreaX, SpawnAreaY), NumOfObj, camera);
        return placementSamples;
    }

    void SpawnObjects(int n_obj, NativeList<float2> placementSamples)
    {
        
        foreach (var sample in placementSamples)
        {
            if(n_obj == gameList.Count){
                break;
            }
            var offset = new Vector3(SpawnAreaX, 0, SpawnAreaY) * -0.5f;
            Vector3 newPos = new Vector3(sample.x, 0, sample.y) + offset;


            if(!IsInCameraViewport(new Vector2(sample.x, sample.y), camera, SpawnAreaX, SpawnAreaY)){
                continue;
            }
            GameObject newobj = Instantiate(RandomObjects[Random.Range(0, RandomObjects.Length)], newPos, Quaternion.Euler(0, 0, 0), this.transform);

            newobj.GetComponent<Renderer>().material.SetColor("_BaseColor", Color.HSVToRGB(Random.Range(0f, 1f), 0.8f, 0.8f));
            newobj.transform.Translate(new Vector3(0, (newobj.transform.position.y - newobj.GetComponent<MeshFilter>().mesh.bounds.min.y), 0));
            
            //TODO: do it in more convenient, json format
            WriteToFile(newobj.name +"\t" + camera.transform.InverseTransformPoint(newPos), "objects_relative_to_cam.txt");                
            gameList.Add(newobj);
            
        }

        // Debug.Log("Number of objects found in front of the camera is : " + gameList.Count);

        //TODO: save walls and plane in new content
        //More convenient JSON format for saving objects
        SingleFrame singleFrame = MakeFrameElement();
        WriteToJSONFile(singleFrame, "objects_relative_to_cam.json");

        placementSamples.Dispose();
    }

    bool checkAllObjectsActive(){
        foreach (GameObject item in gameList)
        {
            if(item.activeSelf == false){
                return false;
            }
        }

        for (int i = 0; i < StageElements.Length; i++)
        {
            if(StageElements[i].activeSelf == false){
                return false;
            }
        }
        return true;
    }
    void RandomizeStage()
    {
        for (int i = 0; i < StageElements.Length; i++)
        {
            StageElements[i].GetComponent<Renderer>().material.SetColor("_BaseColor", Color.HSVToRGB(Random.Range(0f, 1f), 0.8f, 0.8f));
        }
    }   

	static bool IsInCameraViewport(Vector2 candidate, GameObject camera, float SpawnAreaX, float SpawnAreaY)
	{
		Vector3 newPos = new Vector3(candidate.x - SpawnAreaX/2, 0, candidate.y - SpawnAreaY/2);
		// Vector3 viewPointNewPos = camera.GetComponent<Camera>().WorldToViewportPoint(newPos);
		Vector3 viewPointNewPos = camera.GetComponent<Camera>().WorldToViewportPoint(newPos);

		return viewPointNewPos.z > 0.1 && viewPointNewPos.x > 0.9 && viewPointNewPos.x < 0.9 && viewPointNewPos.y > 0.1 && viewPointNewPos.y < 0.9;
	}
}