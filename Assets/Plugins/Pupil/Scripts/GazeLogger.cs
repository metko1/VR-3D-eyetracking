using System.IO;
using UnityEngine;

public class GazeLogger : MonoBehaviour
{
    private StreamWriter writer;

    void Start()
    {
        writer = new StreamWriter("gaze_hits.csv");
        writer.WriteLine("Time,Object,X,Y,Z");
	Debug.Log("Start wurde aufgerufen");
    }

    public void LogHit(RaycastHit hit)
    {
	if (writer == null)
	{
    	  Debug.LogError("Writer ist NULL!");
    	  return;
	}
        writer.WriteLine(
            Time.time.ToString("F4") + "," +
            hit.collider.name + "," +
            hit.point.x + "," +
            hit.point.y + "," +
            hit.point.z);
    }

    void OnApplicationQuit()
    {
        writer.Close();
    }
}