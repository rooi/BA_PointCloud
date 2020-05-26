using BAPointCloudRenderer.CloudData;
using System.Collections.Generic;
using UnityEngine;

namespace BAPointCloudRenderer.ObjectCreation
{
    /// <summary>
    /// Renders every point as a single pixel using the points primitive. As described in the Bachelor Thesis in chapter 3.3.1 "Single-Pixel Point Rendering".
    /// </summary>
    class PointMeshConfiguration : MeshConfiguration
    {
        /// <summary>
        /// Radius of the point (in pixel or world units, depending on variable screenSize)
        /// </summary>
        public float pointRadius = 5;
        /// If changing the parameters should be possible during execution, this variable has to be set to true in the beginning! Later changes to this variable will not change anything
        /// </summary>
        public const bool reloadingPossible = true;
        /// <summary>
        /// Set this to true to reload the shaders according to the changed parameters. After applying the changes, the variable will set itself back to false.
        /// </summary>
        public bool reload = false;
        /// <summary>
        /// If set to true, the Bounding Boxes of the individual octree nodes will be displayed.
        /// </summary>
        public bool displayLOD = false;

        private Material material;
        private HashSet<GameObject> gameObjectCollection = null;

		private void LoadShaders() {
			material = (Material)Resources.Load("Materials/PointMaterial", typeof(Material));
            if(!material) material = new Material(Shader.Find("Custom/PointShader"));
            gameObjectCollection = new HashSet<GameObject>();
			material.enableInstancing = true;
            material.SetFloat("_PointSize", pointRadius);
        }
		
        public void Start()
        {
			if (reloadingPossible) {
                gameObjectCollection = new HashSet<GameObject>();
            }
            LoadShaders();
        }

        public void Update()
        {
			if (reload && gameObjectCollection != null) {
                LoadShaders();
                foreach (GameObject go in gameObjectCollection) {
                    go.GetComponent<MeshRenderer>().material = material;
                }
                reload = false;
            }
            if (displayLOD)
            {
                foreach (GameObject go in gameObjectCollection)
                {
                    Utility.BBDraw.DrawBoundingBox(go.GetComponent<BoundingBoxComponent>().boundingBox, null, Color.red, false);
                }
            }
        }

        public override GameObject CreateGameObject(string name, Vector3[] vertexData, Color[] colorData, BoundingBox boundingBox, Transform parent)
        {
            GameObject gameObject = new GameObject(name);

            Mesh mesh = new Mesh();

            MeshFilter filter = gameObject.AddComponent<MeshFilter>();
            filter.mesh = mesh;
            MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.material = material;

            int[] indecies = new int[vertexData.Length];
            for (int i = 0; i < vertexData.Length; ++i)
            {
                indecies[i] = i;
            }
            mesh.vertices = vertexData;
            mesh.colors = colorData;
            mesh.SetIndices(indecies, MeshTopology.Points, 0);

            //Set Translation
            gameObject.transform.Translate(boundingBox.Min().ToFloatVector());
            gameObject.transform.SetParent(parent, false);

            gameObject.AddComponent<BoundingBoxComponent>().boundingBox = boundingBox; ;

            if (gameObjectCollection != null)
            {
                gameObjectCollection.Add(gameObject);
            }

            return gameObject;
        }

        public override int GetMaximumPointsPerMesh()
        {
            return 65000;
        }

        public override void RemoveGameObject(GameObject gameObject)
        {
            if (gameObjectCollection != null)
            {
                gameObjectCollection.Remove(gameObject);
            }
            if (gameObject != null)
            {
                Destroy(gameObject.GetComponent<MeshFilter>().sharedMesh);
                Destroy(gameObject);
            }
        }
    }
}