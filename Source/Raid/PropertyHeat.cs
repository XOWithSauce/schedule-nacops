

namespace NACopsV1
{
    [Serializable]
    public class PropertyHeat
    {
        public string propertyCode;
        public int propertyHeat = 0;
        public int daysSinceLastRaid = 0;
    }

    [Serializable]
    public class PropertiesHeatSerialized
    {
        public List<PropertyHeat> loadedPropertyHeats = new();
    }

}