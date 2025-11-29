using UnityEngine;

[CreateAssetMenu(fileName = "NewMapMarker", menuName = "Architectural Whispers/Map Markers")]
public class MapMarkerData : ScriptableObject
{
    public string markerLabel;
    public float requiredRadius = 20f;

    [Header("GPS Coordinates")]
    public double latitude;
    public double longitude;

    [Header("Scene To Load")]
    public string sceneName;

    [Header("Marker Icons")]
    public Texture2D defaultIcon;
    public Texture2D nearIcon;
}
