using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace BAPointCloudRenderer.CloudData
{
    /// <summary>
    /// Description of a Bounding Box. Created from the cloud.js-File.
    /// Contains all attributes from that file plus two more: cloudPath (folder path of the cloud) and cloudName (name of the cloud)
    /// </summary>
    [Serializable]
    public class PointCloudMetaData
    {

        public string version;
        public string octreeDir;
        public string projection;
        public int points;
        public BoundingBox boundingBox;
        public BoundingBox tightBoundingBox;
        //public List<PointAttributes> pointAttributes;
        public double spacing;
        public double scale;
        public int hierarchyStepSize;
        [NonSerialized]
        public string cloudPath;
        [NonSerialized]
        public string cloudName;

        /// <summary>
        /// Reads the metadata from a json-string.
        /// </summary>
        /// <param name="json">Json-String</param>
        /// <param name="moveToOrigin">True, iff the center of the bounding boxes should be moved to the origin</param>
        public static PointCloudMetaData ReadFromJson(string json, bool moveToOrigin)
        {
            PointCloudMetaData data = JsonConvert.DeserializeObject<PointCloudMetaData>(json);
            
            Version version = Version.Parse(data.version);

            if (version.Major == 1)
            {
                if (version.Minor <= 6)
                {
                    data = JsonConvert.DeserializeObject<PointCloudMetaData16>(json);
                }
                else if (version.Minor <= 7) // there seem to some ambiguity in versioning...
                {
                    PointCloudMetaData16 data16 = JsonConvert.DeserializeObject<PointCloudMetaData16>(json);
                    if (data16.pointAttributes.Count > 0 && data16.pointAttributes[0] != "") data = data16; // workaround for ambiguity
                    else
                    {
                        // Assume 1.7
                        PointCloudMetaData17 data17 = JsonConvert.DeserializeObject<PointCloudMetaData17>(json);
                        data = JsonConvert.DeserializeObject<PointCloudMetaData17>(json);
                    }
                }
                else
                {
                    UnityEngine.Debug.LogWarning("PointCloudMetaData.ReadFromJson - version " + data.version + " is explicitly supported. Trying 1.7 specification");
                    PointCloudMetaData17 data17 = JsonConvert.DeserializeObject<PointCloudMetaData17>(json);
                    data = JsonConvert.DeserializeObject<PointCloudMetaData17>(json);
                }
            }
            else UnityEngine.Debug.LogError("PointCloudMetaData.ReadFromJson - version " + data.version + " is not support");

            data.boundingBox.Init();
            data.boundingBox.SwitchYZ();
            data.tightBoundingBox.SwitchYZ();
            if (moveToOrigin)
            {
                data.boundingBox.MoveToOrigin();
                data.tightBoundingBox.MoveToOrigin();
            }
            return data;
        }


    }

    public class PointCloudMetaData16 : PointCloudMetaData
    {
        public List<string> pointAttributes;
    }

    public class PointCloudMetaData17 : PointCloudMetaData
    {
        public class PointAttributes
        {
            public string name { get; set; }
            public int size { get; set; }
            public int elements { get; set; }
            public int elementSize { get; set; }
            public string type { get; set; }
            public string description { get; set; }
        }

        public PointAttributes[] pointAttributes;
    }

}
