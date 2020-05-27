
namespace BAPointCloudRenderer.Loading
{
    /// <summary>
    /// Attributes for FileReading
    /// </summary>
    class PointAttributes
    {
        public const string POSITION_CARTESIAN = "POSITION_CARTESIAN";
        public const string COLOR_PACKED = "COLOR_PACKED";
        public const string RGB_PACKED = "RGB_PACKED";
        public const string RGBA_PACKED = "RGBA_PACKED";
        public const string RGB = "RGB"; // is in the definition? One cloud.js file seems to have one...
        public const string RGBA= "RGBA"; // is in the definition? One cloud.js file seems to have one...
    }
}
