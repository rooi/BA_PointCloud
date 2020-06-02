using BAPointCloudRenderer.CloudData;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using Newtonsoft.Json.Serialization;
using System.Runtime.Serialization;
using Newtonsoft.Json.Linq;

namespace Pdal
{
    public class PipelineJSON
    {
        public Pipeline[] pipeline { get; set; }

        public class Pipeline
        {
            public string type { get; set; }
            public string filename { get; set; }
            //public string spatialreference { get; set; }
            //public string limits { get; set; }
            //public string dimensions { get; set; }
            //public string enumerate { get; set; }
        }
    }


    public class MetadataJSON
    {
        public Metadata metadata { get; set; }

        [OnError]
        internal void OnError(StreamingContext context, ErrorContext errorContext)
        {
            errorContext.Handled = true;
            UnityEngine.Debug.LogError("MetadataJSON - context = " + context.ToString() + "; errorContext = " + errorContext.ToString());
        }
    }

    public class Metadata
    {
        [JsonProperty(PropertyName = "readers.las")]
        public List<ReadersLas> _readerslas { get; set; }

        public List<ReadersLas> readerslas
        {
            get
            {
                if (_readerslas == null)
                {
                    throw new Exception("readerslas[] not loaded!");
                }

                return _readerslas;
            }
            set { _readerslas = value; }
        }

        [OnError]
        internal void OnError(StreamingContext context, ErrorContext errorContext)
        {
            errorContext.Handled = true;
            UnityEngine.Debug.LogError("readerslas[] - context = " + context.ToString() + "; errorContext = " + errorContext.ToString());
        }
    }

    public class ReadersLas
    {
        public ReadersLas _readerslas { get; set; }

        public ReadersLas readerslas
        {
            get
            {
                if (_readerslas == null)
                {
                    throw new Exception("readerslas not loaded!");
                }

                return _readerslas;
            }
            set { _readerslas = value; }
        }

        [OnError]
        internal void OnError(StreamingContext context, ErrorContext errorContext)
        {
            errorContext.Handled = true;
            UnityEngine.Debug.LogError("readerslas - context = " + context.ToString() + "; errorContext = " + errorContext.ToString());
        }

        public string comp_spatialreference { get; set; }
        public bool compressed { get; set; }
        public int count { get; set; }
        public int creation_doy { get; set; }
        public int creation_year { get; set; }
        public int dataformat_id { get; set; }
        public int dataoffset { get; set; }
        public int filesource_id { get; set; }
        public int global_encoding { get; set; }
        public string global_encoding_base64 { get; set; }
        public int header_size { get; set; }
        public int major_version { get; set; }
        public float maxx { get; set; }
        public float maxy { get; set; }
        public float maxz { get; set; }
        public int minor_version { get; set; }
        public int minx { get; set; }
        public float miny { get; set; }
        public float minz { get; set; }
        public int offset_x { get; set; }
        public float offset_y { get; set; }
        public float offset_z { get; set; }
        public string project_id { get; set; }
        public float scale_x { get; set; }
        public float scale_y { get; set; }
        public float scale_z { get; set; }
        public string software_id { get; set; }
        public string spatialreference { get; set; }
        public Srs srs { get; set; }
        public string system_id { get; set; }
    }

    public class Srs
    {
        public string compoundwkt { get; set; }
        public string horizontal { get; set; }
        public bool isgeocentric { get; set; }
        public bool isgeographic { get; set; }
        public string prettycompoundwkt { get; set; }
        public string prettywkt { get; set; }
        public string proj4 { get; set; }
        public Units units { get; set; }
        public string vertical { get; set; }
        public string wkt { get; set; }
    }

    public class Units
    {
        public string horizontal { get; set; }
        public string vertical { get; set; }
    }
}





namespace BAPointCloudRenderer.Loading
{
    /// <summary>
    /// Provides methods for loading point clouds from the file system
    /// </summary>
    class CloudLoader
    {
        /* Loads the metadata from the json-file in the given cloudpath
         */
        /// <summary>
        /// Loads the meta data from the json-file in the given cloudpath. Attributes "cloudPath", and "cloudName" are set as well.
        /// </summary>
        /// <param name="cloudPath">Folderpath of the cloud</param>
        /// <param name="moveToOrigin">True, if the center of the cloud should be moved to the origin</param>
        public static PointCloudMetaData LoadMetaData(string cloudPath, bool moveToOrigin = false)
        {
            string jsonfile;
            if (cloudPath.Contains("http"))
            {
                using (var webClient = new System.Net.WebClient())
                {
                    jsonfile = webClient.DownloadString(cloudPath);
                }
            }
            else
            {
                using (StreamReader reader = new StreamReader(cloudPath + "cloud.js", Encoding.Default))
                {
                    jsonfile = reader.ReadToEnd();
                    reader.Close();
                }
            }
            string cleanCloudPath = cloudPath;
            if (cleanCloudPath.Contains("cloud.js"))
            {
                cleanCloudPath = cleanCloudPath.Replace("cloud.js", "");
            }

            PointCloudMetaData metaData = PointCloudMetaData.ReadFromJson(jsonfile, moveToOrigin);
            metaData.cloudPath = cleanCloudPath;
            metaData.cloudName = cleanCloudPath.Substring(0, cleanCloudPath.Length - 1).Substring(cleanCloudPath.Substring(0, cleanCloudPath.Length - 1).LastIndexOf("/") + 1);
            return metaData;
        }

        /// <summary>
        /// Loads the complete Hierarchy and ALL points from the pointcloud.
        /// </summary>
        /// <param name="metaData">MetaData-Object, as received by LoadMetaData</param>
        /// <returns>The Root Node of the point cloud</returns>
        public static Node LoadPointCloud(PointCloudMetaData metaData)
        {
            string dataRPath = metaData.cloudPath + metaData.octreeDir + "/";
            if (metaData.hierarchyStepSize > 0) dataRPath += "r/";
            Node rootNode = new Node("", metaData, metaData.boundingBox, null);
            LoadHierarchy(dataRPath, metaData, rootNode);
            LoadAllPoints(dataRPath, metaData, rootNode);
            return rootNode;
        }

        /// <summary>
        /// Loads the hierarchy, but no points are loaded
        /// </summary>
        /// <param name="metaData">MetaData-Object, as received by LoadMetaData</param>
        /// <returns>The Root Node of the point cloud</returns>
        public static Node LoadHierarchyOnly(PointCloudMetaData metaData)
        {
            string dataRPath = metaData.cloudPath + metaData.octreeDir + "/";
            if (metaData.hierarchyStepSize > 0) dataRPath += "r/";
            Node rootNode = new Node("", metaData, metaData.boundingBox, null);
            LoadHierarchy(dataRPath, metaData, rootNode);
            return rootNode;
        }

        /// <summary>
        /// Loads the points for the given node
        /// </summary>
        public static void LoadPointsForNode(Node node)
        {
            string dataRPath = node.MetaData.cloudPath + node.MetaData.octreeDir + "/r/";
            LoadPoints(dataRPath, node.MetaData, node);
        }


        /* Loads the complete hierarchy of the given node. Creates all the children and their data. Points are not yet stored in there.
         * dataRPath is the path of the R-folder
         */
        private static void LoadHierarchy(string dataRPath, PointCloudMetaData metaData, Node root)
        {
            byte[] data = FindAndLoadFile(dataRPath, metaData, root.Name, ".hrc");
            int nodeByteSize = 5;
            int numNodes = data.Length / nodeByteSize;
            int offset = 0;
            Queue<Node> nextNodes = new Queue<Node>();
            nextNodes.Enqueue(root);

            for (int i = 0; i < numNodes; i++)
            {
                Node n = nextNodes.Dequeue();
                byte configuration = data[offset];
                //uint pointcount = System.BitConverter.ToUInt32(data, offset + 1);
                //n.PointCount = pointcount; //TODO: Pointcount is wrong
                for (int j = 0; j < 8; j++)
                {
                    //check bits
                    if ((configuration & (1 << j)) != 0)
                    {
                        //This is done twice for some nodes
                        Node child = new Node(n.Name + j, metaData, calculateBoundingBox(n.BoundingBox, j), n);
                        n.SetChild(j, child);
                        nextNodes.Enqueue(child);
                    }
                }
                offset += 5;
            }
            HashSet<Node> parentsOfNextNodes = new HashSet<Node>();
            while (nextNodes.Count != 0)
            {
                Node n = nextNodes.Dequeue().Parent;
                if (!parentsOfNextNodes.Contains(n))
                {
                    parentsOfNextNodes.Add(n);
                    LoadHierarchy(dataRPath, metaData, n);
                }
                //Node n = nextNodes.Dequeue();
                //LoadHierarchy(dataRPath, metaData, n);
            }
        }

        private static BoundingBox calculateBoundingBox(BoundingBox parent, int index)
        {
            Vector3d min = parent.Min();
            Vector3d max = parent.Max();
            Vector3d size = parent.Size();
            //z and y are different here than in the sample-code because these coordinates are switched in unity
            if ((index & 2) != 0)
            {
                min.z += size.z / 2;
            }
            else
            {
                max.z -= size.z / 2;
            }
            if ((index & 1) != 0)
            {
                min.y += size.y / 2;
            }
            else
            {
                max.y -= size.y / 2;
            }
            if ((index & 4) != 0)
            {
                min.x += size.x / 2;
            }
            else
            {
                max.x -= size.x / 2;
            }
            return new BoundingBox(min, max);
        }

        /* Loads the points for just that one node
         */
        private static void LoadPoints(string dataRPath, PointCloudMetaData metaData, Node node)
        {
            if (metaData is PointCloudMetaData16LAZ || metaData is PointCloudMetaData17LAZ)
            {
                UnityEngine.Debug.LogError("CloudLoader.LoadHierarchy - LAZ/LAS not yet supported");
                //PointCloudMetaData16LAZ metaData16 = null;
                //PointCloudMetaData17LAZ metaData17 = null;
                //if (metaData is PointCloudMetaData16LAZ) metaData16 = (PointCloudMetaData16LAZ)metaData;
                //if (metaData is PointCloudMetaData17LAZ) metaData17 = (PointCloudMetaData17LAZ)metaData;

                //string extention = ".las";
                //if (metaData16 != null && metaData16.pointAttributes.ToUpper().Contains("LAZ")) extention = ".laz";
                //if (metaData17 != null && metaData17.pointAttributes.ToUpper().Contains("LAZ")) extention = ".laz";

                //byte[] data = FindAndLoadFile(dataRPath, metaData, node.Name, extention);

                //int pointByteSize = 16;//TODO: Is this always the case?
                //int numPoints = data.Length / pointByteSize;

                //Read in data
                if (metaData is PointCloudMetaData16LAZ) LoadPointAttributes(dataRPath, (PointCloudMetaData16LAZ)metaData, node);
                else if (metaData is PointCloudMetaData17LAZ) LoadPointAttributes(dataRPath, (PointCloudMetaData17LAZ)metaData, node); // pointByteSize and numPoints are specified in the attributes
                else UnityEngine.Debug.LogError("CloudLoader.LoadPoints - Cloud not load point attributes");
            }
            else
            {

                byte[] data = FindAndLoadFile(dataRPath, metaData, node.Name, ".bin");
                int pointByteSize = 16;//TODO: Is this always the case?
                int numPoints = data.Length / pointByteSize;

                //Read in data
                if (metaData is PointCloudMetaData16) LoadPointAttributes(data, pointByteSize, numPoints, (PointCloudMetaData16)metaData, node);
                else if (metaData is PointCloudMetaData17) LoadPointAttributes(data, (PointCloudMetaData17)metaData, node); // pointByteSize and numPoints are specified in the attributes
                else UnityEngine.Debug.LogError("CloudLoader.LoadPoints - Cloud not load point attributes");
            }
        }

        private static void LoadPointAttributes(byte[] data, int pointByteSize, int numPoints, PointCloudMetaData16 metaData, Node node)
        {
            int offset = 0;

            Vector3[] vertices = new Vector3[numPoints];
            Color[] colors = new Color[numPoints];

            foreach (string pointAttribute in metaData.pointAttributes)
            {
                if (pointAttribute.Equals(PointAttributes.POSITION_CARTESIAN))
                {
                    for (int i = 0; i < numPoints; i++)
                    {
                        //Reduction to single precision!
                        //Note: y and z are switched
                        float x = (float)(System.BitConverter.ToUInt32(data, offset + i * pointByteSize + 0) * metaData.scale/* + node.BoundingBox.lx*/);
                        float y = (float)(System.BitConverter.ToUInt32(data, offset + i * pointByteSize + 8) * metaData.scale/* + node.BoundingBox.lz*/);
                        float z = (float)(System.BitConverter.ToUInt32(data, offset + i * pointByteSize + 4) * metaData.scale/* + node.BoundingBox.ly*/);
                        vertices[i] = new Vector3(x, y, z);
                    }
                    offset += 12;
                }
                else if (pointAttribute.Equals(PointAttributes.COLOR_PACKED))
                {
                    for (int i = 0; i < numPoints; i++)
                    {
                        byte r = data[offset + i * pointByteSize + 0];
                        byte g = data[offset + i * pointByteSize + 1];
                        byte b = data[offset + i * pointByteSize + 2];
                        colors[i] = new Color32(r, g, b, 255);
                    }
                    offset += 3;
                }
            }

            node.SetPoints(vertices, colors);

        }

        private static void LoadPointAttributes(byte[] data, PointCloudMetaData17 metaData, Node node)
        {
            int offset = 0;

            // Determine numPoints
            int pointByteSize = 0;
            foreach (PointCloudMetaData17.PointAttributes pointAttribute in metaData.pointAttributes)
            {
                pointByteSize += pointAttribute.size;
            }

            int numPoints = data.Length / pointByteSize;

            Vector3[] vertices = new Vector3[numPoints];
            Color[] colors = new Color[numPoints];

            // read all data
            int currentVertex = 0;
            int currentColor = 0;
            while (offset < data.Length)
            {
                int startOffset = offset;
                foreach (PointCloudMetaData17.PointAttributes pointAttribute in metaData.pointAttributes)
                {
                    if (pointAttribute.name.Equals(PointAttributes.POSITION_CARTESIAN))
                    {
                        //Reduction to single precision!
                        //Note: y and z are switched
                        float x = (float)(System.BitConverter.ToUInt32(data, offset + 0) * metaData.scale/* + node.BoundingBox.lx*/);
                        float y = (float)(System.BitConverter.ToUInt32(data, offset + 8) * metaData.scale/* + node.BoundingBox.lz*/);
                        float z = (float)(System.BitConverter.ToUInt32(data, offset + 4) * metaData.scale/* + node.BoundingBox.ly*/);
                        vertices[currentVertex] = new Vector3(x, y, z);
                        currentVertex++;

                        offset += pointAttribute.size;
                    }
                    else if (pointAttribute.name.Equals(PointAttributes.COLOR_PACKED) ||
                         pointAttribute.name.Equals(PointAttributes.RGB_PACKED) ||
                         pointAttribute.name.Equals(PointAttributes.RGBA_PACKED) ||
                         pointAttribute.name.Equals(PointAttributes.RGB) ||
                         pointAttribute.name.Equals(PointAttributes.RGBA))
                    {

                        byte r = data[offset + 0];
                        byte g = data[offset + 1];
                        byte b = data[offset + 2];
                        byte a = (pointAttribute.name.Equals(PointAttributes.RGBA_PACKED) || pointAttribute.name.Equals(PointAttributes.RGBA)) ? data[offset + 3] : (byte)255;
                        colors[currentColor] = new Color32(r, g, b, 255);
                        currentColor++;

                        offset += pointAttribute.size;
                    }
                    else // just skip the data; 
                    {
                        offset += pointAttribute.size;
                    }
                }
                if (startOffset >= offset)
                {
                    UnityEngine.Debug.LogError("CloudLoader.LoadPointAttributes - offset not increaing, why? Breaking off while loop");
                    offset = data.Length;
                    break;
                }
            }

            node.SetPoints(vertices, colors);

        }

        private static void LoadPointAttributes(string dataRPath, PointCloudMetaData16LAZ metaData, Node node)
        {
            int offset = 0;

            //            Vector3[] vertices = new Vector3[numPoints];
            //            Color[] colors = new Color[numPoints];
#if FALSE
            foreach (string pointAttribute in metaData.pointAttributes)
            {
                if (pointAttribute.Equals(PointAttributes.POSITION_CARTESIAN))
                {
                    for (int i = 0; i < numPoints; i++)
                    {
                        //Reduction to single precision!
                        //Note: y and z are switched
                        float x = (float)(System.BitConverter.ToUInt32(data, offset + i * pointByteSize + 0) * metaData.scale/* + node.BoundingBox.lx);
                        float y = (float)(System.BitConverter.ToUInt32(data, offset + i * pointByteSize + 8) * metaData.scale/* + node.BoundingBox.lz*/);
                        float z = (float)(System.BitConverter.ToUInt32(data, offset + i * pointByteSize + 4) * metaData.scale/* + node.BoundingBox.ly*/);
                        vertices[i] = new Vector3(x, y, z);
                    }
                    offset += 12;
                }
                else if (pointAttribute.Equals(PointAttributes.COLOR_PACKED))
                {
                    for (int i = 0; i < numPoints; i++)
                    {
                        byte r = data[offset + i * pointByteSize + 0];
                        byte g = data[offset + i * pointByteSize + 1];
                        byte b = data[offset + i * pointByteSize + 2];
                        colors[i] = new Color32(r, g, b, 255);
                    }
                    offset += 3;
                }
            }
#endif
            //            node.SetPoints(vertices, colors);

        }

        public static void HandleDeserializationError(object sender, Newtonsoft.Json.Serialization.ErrorEventArgs errorArgs)
        {
            var currentError = errorArgs.ErrorContext.Error.Message;
            UnityEngine.Debug.LogError("CloudLoader.HandleDeserializationError -" + currentError);
            errorArgs.ErrorContext.Handled = true;
        }

        private static void LoadPointAttributes(string dataRPath, PointCloudMetaData17LAZ metaData, Node node)
        {
            string id = node.Name;
            int levels = metaData.hierarchyStepSize == 0 ? 0 : id.Length / metaData.hierarchyStepSize;
            string path = "";
            for (int i = 0; i < levels; i++)
            {
                path += id.Substring(i * metaData.hierarchyStepSize, metaData.hierarchyStepSize) + "/";
            }

            string fileending = "." + metaData.pointAttributes.ToLower();
            path += "r" + id + fileending;

            Pdal.PipelineJSON jsonObject = new Pdal.PipelineJSON();
            jsonObject.pipeline = new Pdal.PipelineJSON.Pipeline[1];
            jsonObject.pipeline[0] = new Pdal.PipelineJSON.Pipeline();
            jsonObject.pipeline[0].type = "readers.las";
            jsonObject.pipeline[0].filename = dataRPath + path;//"G:/dev/3rdParty/potree/pointclouds/lion_takanawa_las/data/r.las";//dataRPath + path;
            string json = JsonConvert.SerializeObject(jsonObject);
            UnityEngine.Debug.Log(json);

            pdal.Pipeline pipeline = new pdal.Pipeline(json);
            long pointCount = pipeline.Execute();

            //UnityEngine.Debug.Log("Executed pipeline at " + path);
            UnityEngine.Debug.Log("Point count: " + pointCount);
            UnityEngine.Debug.Log("Log Level: " + pipeline.LogLevel);
            UnityEngine.Debug.Log("Metadata: " + pipeline.Metadata);
            UnityEngine.Debug.Log("Schema: " + pipeline.Schema);
            UnityEngine.Debug.Log("Log: " + pipeline.Log);
            UnityEngine.Debug.Log("Pipeline JSON: " + json);
            UnityEngine.Debug.Log("Result JSON: " + pipeline.Json);


            JObject jsonSearch = JObject.Parse(pipeline.Metadata);

            // get JSON result objects into a list
            IList<JToken> results = jsonSearch["metadata"]["readers.las"].Children().ToList();


            // serialize JSON results into .NET objects
            Pdal.ReadersLas readersLas = null;
            //IList<Pdal.ReadersLas> searchResults = new List<Pdal.ReadersLas>();
            foreach (JToken result in results)
            {
                // JToken.ToObject is a helper method that uses JsonSerializer internally
                Pdal.ReadersLas searchResult = result.ToObject<Pdal.ReadersLas>();
                if (searchResult.project_id != null) readersLas = searchResult;
            }

            pdal.PointViewIterator views = pipeline.Views;
            pdal.PointView view = views != null ? views.Next : null;

            Vector3[] vertices = new Vector3[pointCount];
            Color[] colors = new Color[pointCount];

            ulong pointCounter = 0;

            while (view != null)
            {
                ulong viewPointCounter = pointCounter;
                UnityEngine.Debug.LogWarning("viewPointCounter @START = " + viewPointCounter);

                UnityEngine.Debug.Log("View " + view.Id);
                UnityEngine.Debug.Log("\tproj4: " + view.Proj4);
                UnityEngine.Debug.Log("\tWKT: " + view.Wkt);
                UnityEngine.Debug.Log("\tSize: " + view.Size + " points");
                UnityEngine.Debug.Log("\tEmpty? " + view.Empty);
                UnityEngine.Debug.Log("\tmetaData.scale? " + metaData.scale);


                Vector3[] viewVertices = new Vector3[view.Size];
                Color[] viewColors = new Color[view.Size];

                pdal.PointLayout layout = view.Layout;
                UnityEngine.Debug.Log("\tHas layout? " + (layout != null));

                if (layout != null)
                {
                    UnityEngine.Debug.Log("\tLayout - Point Size: " + layout.PointSize + " bytes");
                    pdal.DimTypeList types = layout.Types;
                    UnityEngine.Debug.Log("\tLayout - Has dimension type list? " + (types != null));

                    if (types != null)
                    {
                        uint size = types.Size;
                        UnityEngine.Debug.Log("\tLayout - Dimension type count: " + size + " dimensions");
                        UnityEngine.Debug.Log("\tLayout - Point size calculated from dimension type list: " + types.ByteCount + " bytes");

                        uint offset = 0;
                        ulong positionType = 0;

                        byte[] points = view.GetAllPackedPoints(types);

                        ulong numPoints = view.Size;
                        for (ulong p = 0; p < numPoints; p++)
                        {
                            //UnityEngine.Debug.Log("\tDimension Types (including value of first point in view)");

                            //byte[] point = view.GetPackedPoint(types, p);
                            int positionInterp = 0;

                            float x = 0, y = 0, z = 0;
                            ushort r = 0, g = 0, b = 0;

                            for (uint i = 0; i < size; ++i)
                            {
                                pdal.DimType type = types.at(i);
                                string interpretationName = type.InterpretationName;
                                int interpretationByteCount = type.InterpretationByteCount;
                                string value = "?";
                                UInt16 valueUint = 0;
                                double valueD = 0.0;

                                if (interpretationName == "double")
                                {
                                    valueD = BitConverter.ToDouble(points, (int)(offset + positionInterp));
                                    value = valueD.ToString();
                                }
                                else if (interpretationName == "float")
                                {
                                    value = BitConverter.ToSingle(points, (int)(offset + positionInterp)).ToString();
                                }
                                else if (interpretationName.StartsWith("uint64"))
                                {
                                    value = BitConverter.ToUInt64(points, (int)(offset + positionInterp)).ToString();
                                }
                                else if (interpretationName.StartsWith("uint32"))
                                {
                                    value = BitConverter.ToUInt32(points, (int)(offset + positionInterp)).ToString();
                                }
                                else if (interpretationName.StartsWith("uint16"))
                                {
                                    valueUint = BitConverter.ToUInt16(points, (int)(offset + positionInterp));
                                    value = valueUint.ToString();
                                }
                                else if (interpretationName.StartsWith("uint8"))
                                {
                                    value = points[offset + positionInterp].ToString();
                                }
                                else if (interpretationName.StartsWith("int64"))
                                {
                                    value = BitConverter.ToInt64(points, (int)(offset + positionInterp)).ToString();
                                }
                                else if (interpretationName.StartsWith("int32"))
                                {
                                    value = BitConverter.ToInt32(points, (int)(offset + positionInterp)).ToString();
                                }
                                else if (interpretationName.StartsWith("int16"))
                                {
                                    value = BitConverter.ToInt16(points, (int)(offset + positionInterp)).ToString();
                                }
                                else if (interpretationName.StartsWith("int8"))
                                {
                                    value = ((sbyte)points[offset + positionInterp]).ToString();
                                }

                                if (p == 0)
                                {
                                    UnityEngine.Debug.Log("\t\tType " + type.Id + " [" + type.IdName
                                        + " (" + type.Interpretation + ":" + type.InterpretationName + " <" + type.InterpretationByteCount + " bytes>"
                                        + "), position " + positionInterp
                                       + ", scale " + type.Scale
                                        + ", offset " + type.Offset + ", metaData.scale " + metaData.scale + "]: " + value);
                                }

                                //Note: y and z are switched
                                //if (type.IdName == "X") x = (float)((valueD * type.Scale) * metaData.scale - node.BoundingBox.lx + metaData.boundingBox.lx); // optimize this
                                //if (type.IdName == "Y") z = (float)((valueD * type.Scale) * metaData.scale - node.BoundingBox.lz + metaData.boundingBox.lz); // optimize this
                                //if (type.IdName == "Z") y = (float)((valueD * type.Scale) * metaData.scale - node.BoundingBox.ly + metaData.boundingBox.ly); // optimize this
                                if (type.IdName == "X") x = (float)(((valueD * type.Scale) - readersLas.offset_x));// * readersLas.scale_x);// + node.BoundingBox.lx); // optimize this
                                if (type.IdName == "Y") z = (float)(((valueD * type.Scale) - readersLas.offset_y));// * readersLas.scale_y);// + node.BoundingBox.ly); // optimize this
                                if (type.IdName == "Z") y = (float)(((valueD * type.Scale) - readersLas.offset_z));// * readersLas.scale_z);// + node.BoundingBox.lz); // optimize this

                                if (type.IdName == "Red") r = (ushort)(valueUint / 256);// optimize this
                                if (type.IdName == "Green") g = (ushort)(valueUint / 256); // optimize this
                                if (type.IdName == "Blue") b = (ushort)(valueUint / 256); // optimize this

                                positionInterp += interpretationByteCount;
                            }

                            //UnityEngine.Debug.Log("x = " + x + ", y = " + y + ", z = " + z + ",    metaData.boundingBox.lx = " + metaData.boundingBox.lx);
                            //viewVertices[(int)(viewPointCounter + p)] = new Vector3(x, y, z);
                            //viewColors[(int)(viewPointCounter + p)] = new Color32(r, g, b, 255);

                            vertices[(int)(pointCounter)] = new Vector3(x, y, z);
                            colors[(int)(pointCounter)] = new Color32((byte)r, (byte)g, (byte)b, 255);

                            //UnityEngine.Debug.Log("VERTEX[" + (viewPointCounter + p) + "] = " + viewVertices[(int)(viewPointCounter + p)]);
                            //UnityEngine.Debug.Log("COLOR[" + colors.Count + "] = " + colors.ElementAt(colors.Count-1));

                            pointCounter++;
                            offset += layout.PointSize;
                            positionType += types.ByteCount;
                        }

                    }

                    types.Dispose();
                }

                // Copy to main array
                //viewVertices.CopyTo(vertices, (int)viewPointCounter);
                //viewColors.CopyTo(colors, (int)viewPointCounter);
                //UnityEngine.Debug.Log("VERTEX[" + (viewPointCounter) + "] = " + vertices[(int)(viewPointCounter)]);
                //UnityEngine.Debug.Log("VERTEX[" + (viewPointCounter + 1) + "] = " + vertices[(int)(viewPointCounter + 1)]);
                //UnityEngine.Debug.Log("VERTEX[" + (viewPointCounter + 2) + "] = " + vertices[(int)(viewPointCounter + 2)]);

                view.Dispose();
                view = views.Next;
            }

            if (views != null)
            {
                views.Dispose();
            }

            UnityEngine.Debug.LogWarning("pointCounter @END = " + pointCounter);

            node.SetPoints(vertices, colors);
        }

        /* Finds a file for a node in the hierarchy.
         * Assuming hierarchyStepSize is 3 and we are looking for the file 0123456765.bin, it is in:
         * 012/012345/012345676/r0123456765.bin
         * 012/345/676/r012345676.bin
         */
        private static byte[] FindAndLoadFile(string dataRPath, PointCloudMetaData metaData, string id, string fileending)
        {

            int levels = metaData.hierarchyStepSize == 0 ? 0 : id.Length / metaData.hierarchyStepSize;
            string path = "";
            for (int i = 0; i < levels; i++)
            {
                path += id.Substring(i * metaData.hierarchyStepSize, metaData.hierarchyStepSize) + "/";
            }

            path += "r" + id + fileending;

            if (dataRPath.Contains("http"))
            {
                WebClient client = new WebClient();
                return client.DownloadData(dataRPath + path);
            }
            else
            {
                return File.ReadAllBytes(dataRPath + path);
            }
        }

        /* Loads the points for that node and all its children
         */
        private static uint LoadAllPoints(string dataRPath, PointCloudMetaData metaData, Node node)
        {
            LoadPoints(dataRPath, metaData, node);
            uint numpoints = (uint)node.PointCount;
            for (int i = 0; i < 8; i++)
            {
                if (node.HasChild(i))
                {
                    numpoints += LoadAllPoints(dataRPath, metaData, node.GetChild(i));
                }
            }
            return numpoints;
        }

        public static uint LoadAllPointsForNode(Node node)
        {
            string dataRPath = node.MetaData.cloudPath + node.MetaData.octreeDir + "/r/";
            return LoadAllPoints(dataRPath, node.MetaData, node);
        }
    }
}
