﻿using BAPointCloudRenderer.CloudData;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using UnityEngine;

namespace BAPointCloudRenderer.Loading {
    /// <summary>
    /// Provides methods for loading point clouds from the file system
    /// </summary>
    class CloudLoader {
        /* Loads the metadata from the json-file in the given cloudpath
         */
         /// <summary>
         /// Loads the meta data from the json-file in the given cloudpath. Attributes "cloudPath", and "cloudName" are set as well.
         /// </summary>
         /// <param name="cloudPath">Folderpath of the cloud</param>
         /// <param name="moveToOrigin">True, if the center of the cloud should be moved to the origin</param>
        public static PointCloudMetaData LoadMetaData(string cloudPath, bool moveToOrigin = false) {
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
            metaData.cloudName = cleanCloudPath.Substring(0, cleanCloudPath.Length-1).Substring(cleanCloudPath.Substring(0, cleanCloudPath.Length - 1).LastIndexOf("/") + 1);
            return metaData;
        }
        
        /// <summary>
        /// Loads the complete Hierarchy and ALL points from the pointcloud.
        /// </summary>
        /// <param name="metaData">MetaData-Object, as received by LoadMetaData</param>
        /// <returns>The Root Node of the point cloud</returns>
        public static Node LoadPointCloud(PointCloudMetaData metaData) {
            string dataRPath = metaData.cloudPath + metaData.octreeDir + "/r/";
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
        public static Node LoadHierarchyOnly(PointCloudMetaData metaData) {
            string dataRPath = metaData.cloudPath + metaData.octreeDir + "/r/";
            Node rootNode = new Node("", metaData, metaData.boundingBox, null);
            LoadHierarchy(dataRPath, metaData, rootNode);
            return rootNode;
        }

        /// <summary>
        /// Loads the points for the given node
        /// </summary>
        public static void LoadPointsForNode(Node node) {
            string dataRPath = node.MetaData.cloudPath + node.MetaData.octreeDir + "/r/";
            LoadPoints(dataRPath, node.MetaData, node);
        }

        /* Loads the complete hierarchy of the given node. Creates all the children and their data. Points are not yet stored in there.
         * dataRPath is the path of the R-folder
         */
        private static void LoadHierarchy(string dataRPath, PointCloudMetaData metaData, Node root) {
            byte[] data = FindAndLoadFile(dataRPath, metaData, root.Name, ".hrc");
            int nodeByteSize = 5;
            int numNodes = data.Length / nodeByteSize;
            int offset = 0;
            Queue<Node> nextNodes = new Queue<Node>();
            nextNodes.Enqueue(root);

            for (int i = 0; i < numNodes; i++) {
                Node n = nextNodes.Dequeue();
                byte configuration = data[offset];
                //uint pointcount = System.BitConverter.ToUInt32(data, offset + 1);
                //n.PointCount = pointcount; //TODO: Pointcount is wrong
                for (int j = 0; j < 8; j++) {
                    //check bits
                    if ((configuration & (1 << j)) != 0) {
                        //This is done twice for some nodes
                        Node child = new Node(n.Name + j, metaData, calculateBoundingBox(n.BoundingBox, j), n);
                        n.SetChild(j, child);
                        nextNodes.Enqueue(child);
                    }
                }
                offset += 5;
            }
            HashSet<Node> parentsOfNextNodes = new HashSet<Node>();
            while (nextNodes.Count != 0) {
                Node n = nextNodes.Dequeue().Parent;
                if (!parentsOfNextNodes.Contains(n)) {
                    parentsOfNextNodes.Add(n);
                    LoadHierarchy(dataRPath, metaData, n);
                }
                //Node n = nextNodes.Dequeue();
                //LoadHierarchy(dataRPath, metaData, n);
            }
        }

        private static BoundingBox calculateBoundingBox(BoundingBox parent, int index) {
            Vector3d min = parent.Min();
            Vector3d max = parent.Max();
            Vector3d size = parent.Size();
            //z and y are different here than in the sample-code because these coordinates are switched in unity
            if ((index & 2) != 0) {
                min.z += size.z / 2;
            } else {
                max.z -= size.z / 2;
            }
            if ((index & 1) != 0) {
                min.y += size.y / 2;
            } else {
                max.y -= size.y / 2;
            }
            if ((index & 4) != 0) {
                min.x += size.x / 2;
            } else {
                max.x -= size.x / 2;
            }
            return new BoundingBox(min, max);
        }

        /* Loads the points for just that one node
         */
        private static void LoadPoints(string dataRPath, PointCloudMetaData metaData, Node node) {

            byte[] data = FindAndLoadFile(dataRPath, metaData, node.Name, ".bin");
            int pointByteSize = 16;//TODO: Is this always the case?
            int numPoints = data.Length / pointByteSize;

            //Read in data
            if (metaData is PointCloudMetaData16) LoadPointAttributes(data, pointByteSize, numPoints, (PointCloudMetaData16)metaData, node);
            else if (metaData is PointCloudMetaData17) LoadPointAttributes(data, (PointCloudMetaData17)metaData, node); // pointByteSize and numPoints are specified in the attributes
            else UnityEngine.Debug.LogError("CloudLoader.LoadPoints - Cloud not load point attributes");

            
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
            while(offset < data.Length)
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
                        byte a = (pointAttribute.name.Equals(PointAttributes.RGBA_PACKED) || pointAttribute.name.Equals(PointAttributes.RGBA) )? data[offset + 3] : (byte)255;
                        colors[currentColor] = new Color32(r, g, b, 255);
                        currentColor++;
                        
                        offset += pointAttribute.size;
                    }
                    else // just skip the data; 
                    {
                        offset += pointAttribute.size;
                    }
                }
                if(startOffset >= offset)
                {
                    UnityEngine.Debug.LogError("CloudLoader.LoadPointAttributes - offset not increaing, why? Breaking off while loop");
                    offset = data.Length;
                    break;
                }
            }

            node.SetPoints(vertices, colors);

        }

        /* Finds a file for a node in the hierarchy.
         * Assuming hierarchyStepSize is 3 and we are looking for the file 0123456765.bin, it is in:
         * 012/012345/012345676/r0123456765.bin
         * 012/345/676/r012345676.bin
         */
        private static byte[] FindAndLoadFile(string dataRPath, PointCloudMetaData metaData, string id, string fileending) {
            
            int levels = id.Length / metaData.hierarchyStepSize;
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
        private static uint LoadAllPoints(string dataRPath, PointCloudMetaData metaData, Node node) {
            LoadPoints(dataRPath, metaData, node);
            uint numpoints = (uint)node.PointCount;
            for (int i = 0; i < 8; i++) {
                if (node.HasChild(i)) {
                    numpoints += LoadAllPoints(dataRPath, metaData, node.GetChild(i));
                }
            }
            return numpoints;
        }

        public static uint LoadAllPointsForNode(Node node) {
            string dataRPath = node.MetaData.cloudPath + node.MetaData.octreeDir + "/r/";
            return LoadAllPoints(dataRPath, node.MetaData, node);
        }
    }
}
