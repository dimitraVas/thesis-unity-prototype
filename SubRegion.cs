using UnityEngine;
using System.Collections.Generic;
using Debug = UnityEngine.Debug;

[System.Serializable]
public class SubRegion
{
    public string subRegionName; // Name of the subregion
    public int subRegionCode;
    public List<Vector2> subRegionVertices; // Vertices defining the subregion (polygon)
    
    // Constructor
    public SubRegion(string name, int code, List<Vector2> vertices)
    {
        subRegionName = name;
        subRegionCode = code;
        subRegionVertices = vertices;
    }
}