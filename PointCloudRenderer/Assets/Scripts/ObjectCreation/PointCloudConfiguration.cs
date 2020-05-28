using BAPointCloudRenderer.CloudData;
using System.Collections.Generic;
using UnityEngine;

namespace BAPointCloudRenderer.ObjectCreation {

    /// <summary>
    /// This is the default Mesh Configuration, that is able to render points as pixels, quads, circles and also provides fragment and cone interpolations using the fragment shader (see Thesis chapter 3.3.4 "Interpolation").
    /// This works using Geometry Shader Quad Rendering, as described in the Bachelor Thesis in chapter 3.3.3.
    /// This configuration also supports changes of the parameters while the application is running. Just change the parameters and check the checkbox "reload".
    /// This class replaces GeoQuadMeshConfiguration in Version 1.2.
    /// </summary>
    class PointCloudConfiguration : MeshConfiguration {
        // -------------------------------------------------------------------------------------------------------------------------------------------
        [System.Serializable]
        public struct PointCLoudSettings
        {
            //public GameObject PointCacheSource;
            //public Texture meshTexture;
            public float particleSize;
            public float meshScaleFactor;
            public Vector3 meshPosOffset;

            public ComputeBuffer MeshDataBuffer;
            //public ComputeBuffer MeshUVDataBuffer;

            //[HideInInspector]
            //public Mesh meshToBake;

            public int length;
        }

        struct ParticleData
        {
            public Vector4 Position;
            public Vector4 Color;
        }

        struct MeshData
        {
            public Vector3 Position;
        }

        // -------------------------------------------------------------------------------------------------------------------------------------------
        public ComputeShader cp;
        public int numberOfParticles = 60000;
        public Material mt;

        [Range(0, 1)]
        public float ColorizationStrength;
        [Range(0, 1)]
        public float MeshOneTwoTransition;
        public bool autoTransition;


        [Header("Mesh")]
        public PointCLoudSettings MeshOne;
        //public MeshSettings MeshTwo;

        [Header("Coloring")]
        public Color KeyOne = new Color(0.4588236f, 0.4901961f, 0.3647059f);
        public Color KeyTwo = new Color(0.8509805f, 0.2901961f, 0.007843138f);
        public Color KeyThree = new Color(0.9245283f, 0.7576204f, 0.4099323f);
        public Color FogColor = new Color(0.9188747f, 1, 0.3113208f);

        ComputeBuffer ParticleBuffer;
        //ComputeBuffer OldParticleBuffer;

        /// <summary>
        /// The camera that's used for rendering. If not set, Camera.main is used. 
        /// This should usually be the same camera that's used as "User Camera" in the point cloud set.
        /// </summary>
        public Camera renderCamera = null;

        private Material material;
        private HashSet<GameObject> gameObjectCollection = null;

        private void Init() {
            MeshOne.length = 0;
            if (renderCamera == null)
            {
                renderCamera = Camera.main;
            }
        }

        public void Start() {
            ParticleBuffer = new ComputeBuffer(numberOfParticles, sizeof(float) * 8);

            Init();
        }

        public void Update() {
            ExecuteComputeShader();
        }

        private void OnRenderObject()
        {
            Rendershapes();
        }

        private void OnDestroy()
        {
            ParticleBuffer.Release();
        }

        // -------------------------------------------------------------------------------------------------------------------------------------------
        void Rendershapes()
        {
            mt.SetPass(0);
            Matrix4x4 ow = this.transform.localToWorldMatrix;
            mt.SetMatrix("My_Object2World", ow);

            mt.SetBuffer("_ParticleDataBuff", ParticleBuffer);
            mt.SetColor("_FogColorc", FogColor);
            //mt.SetFloat("_ParticleSize", Mathf.Lerp(MeshOne.particleSize, MeshTwo.particleSize, MeshOneTwoTransition));
            Graphics.DrawProceduralNow(MeshTopology.Triangles, 3 * 4, numberOfParticles);
        }

        void ExecuteComputeShader()
        {
            int kernelHandle = cp.FindKernel("CSMain");
            cp.SetBuffer(kernelHandle, "_inParticleBuffer", ParticleBuffer);
            cp.SetBuffer(kernelHandle, "_outParticleBuffer", ParticleBuffer);

            if (MeshOne.MeshDataBuffer != null)
            {
                //Setting meshOne variables
                cp.SetBuffer(kernelHandle, "_MeshDataOne", MeshOne.MeshDataBuffer);
                //cp.SetBuffer(kernelHandle, "_MeshDataUVOne", MeshOne.MeshUVDataBuffer);
                cp.SetInt("_CachePointVertexcoundOne", MeshOne.length);
                //cp.SetTexture(kernelHandle, "_MeshTextureOne", MeshOne.meshTexture);
                //cp.SetVector("_transformInfoOne", new Vector4(MeshOne.meshPosOffset.x, MeshOne.meshPosOffset.y,
                //    MeshOne.meshPosOffset.z, MeshOne.meshScaleFactor));


                cp.SetFloat("meshOneTwoTransition", MeshOneTwoTransition);
                cp.SetInt("_NumberOfParticles", numberOfParticles);
                cp.SetFloat("_Time", Time.time);
                KeyOne.a = ColorizationStrength;
                cp.SetVector("_Color1", KeyOne);
                cp.SetVector("_Color2", KeyTwo);
                cp.SetVector("_Color3", KeyThree);
                cp.SetVector("CameraPosition", this.transform.InverseTransformPoint(Camera.main.transform.position));
                cp.SetVector("CameraForward", this.transform.InverseTransformDirection(Camera.main.transform.forward));

                cp.Dispatch(kernelHandle, numberOfParticles, 1, 1);
            }
        }

        public override GameObject CreateGameObject(string name, Vector3[] vertexData, Color[] colorData, BoundingBox boundingBox, Transform parent) {
            GameObject gameObject = new GameObject(name);

            if (MeshOne.MeshDataBuffer == null)
            {
                MeshOne.MeshDataBuffer = new ComputeBuffer(vertexData.Length, sizeof(float) * 3);

                MeshOne.MeshDataBuffer.SetData(vertexData);

                //MeshOne.MeshUVDataBuffer = new ComputeBuffer(vertexData.Length, sizeof(float) * 2);

                MeshOne.length = vertexData.Length;
            }

            //toSet.MeshUVDataBuffer.SetData(toSet.meshToBake.uv);
            /*
            Mesh mesh = new Mesh();

            MeshFilter filter = gameObject.AddComponent<MeshFilter>();
            filter.mesh = mesh;
            MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.material = material;

            int[] indecies = new int[vertexData.Length];
            for (int i = 0; i < vertexData.Length; ++i) {
                indecies[i] = i;
            }
            mesh.vertices = vertexData;
            mesh.colors = colorData;
            mesh.SetIndices(indecies, MeshTopology.Points, 0);

            //Set Translation
            gameObject.transform.Translate(boundingBox.Min().ToFloatVector());
            gameObject.transform.SetParent(parent, false);
            
            BoundingBoxComponent bbc = gameObject.AddComponent<BoundingBoxComponent>();
            bbc.boundingBox = boundingBox; ;
            bbc.parent = parent;
            */



            if (gameObjectCollection != null) {
                gameObjectCollection.Add(gameObject);
            }

            return gameObject;
        }

        public override int GetMaximumPointsPerMesh() {
            return 65000;
        }

        public override void RemoveGameObject(GameObject gameObject) {
            if (gameObjectCollection != null) {
                gameObjectCollection.Remove(gameObject);
            }
            if (gameObject != null) {
                Destroy(gameObject.GetComponent<MeshFilter>().sharedMesh);
                Destroy(gameObject);
            }
        }
    }
}
