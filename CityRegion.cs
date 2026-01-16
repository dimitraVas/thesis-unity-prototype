using UnityEngine;
using System.Collections.Generic;
using Debug = UnityEngine.Debug;

// Represents a city region defined by real-world coordinates.
// Each CityRegion has spatial boundaries (polygon vertices),
// basic land-use characteristics and optional subregions.
// Used to classify locations within the city


[System.Serializable]
public class CityRegion
{
    public string regionName; // Name of the region
    public int regionCode;// Code on google earth

   // Type of land use
    public int useRes;
    public int useCom;
    public int useEnv;
    public int useHist;
    public string regionInfo;
    
    public List<Vector2> polygonVertices; // List of points defining the polygon

    public List<SubRegion> subRegions; // List of subregions within this region

    // Constructor 
    public CityRegion(string name, int code, int useres, int usecom, int useenv, int usehist,string regioninfo, List<Vector2> vertices, List<SubRegion> subregions)
    {
        regionName = name;
        regionCode = code;
        useRes = useres;
        useCom = usecom;
        useEnv = useenv;
        useHist = usehist;
        regionInfo = regioninfo;
        polygonVertices = vertices;
        subRegions = subregions;
    }
}



