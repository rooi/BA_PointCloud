#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace TheTide.utils
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshFilter))]
    public class SerializeMesh : MonoBehaviour
    {
        [HideInInspector] [SerializeField] Vector2[] uv;
        [HideInInspector] [SerializeField] Vector3[] verticies;
        [HideInInspector] [SerializeField] int[] triangles;
        [HideInInspector] [SerializeField] bool serialized = false;
        // Use this for initialization

        void Awake()
        {
            if (serialized)
            {
                MeshFilter meshFilter = GetComponent<MeshFilter>();
                if (meshFilter)
                {
                    meshFilter.mesh = Rebuild();
                }
                else
                {
                    MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();
                    foreach(MeshFilter meshF in meshFilters)
                    {
                        meshF.mesh = Rebuild();
                    }
                }
            }
        }

        void Start()
        {
            if (serialized) return;

            Serialize();
        }

        public void Serialize()
        {
            /*
            MeshFilter meshFilter = GetComponent<MeshFilter>();
            if (meshFilter)
            {
                var mesh = meshFilter.mesh;

                uv = mesh.uv;
                verticies = mesh.vertices;
                triangles = mesh.triangles;

                serialized = true;
            }
            else*/
            {
                MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();
                foreach (MeshFilter meshF in meshFilters)
                {
                    var mesh = meshF.mesh;

                    uv = mesh.uv;
                    verticies = mesh.vertices;
                    triangles = mesh.triangles;

                    serialized = true;
                }
            }

            
        }

        public Mesh Rebuild()
        {
            Mesh mesh = new Mesh();
            mesh.vertices = verticies;
            mesh.triangles = triangles;
            mesh.uv = uv;

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(SerializeMesh))]
    class SerializeMeshEditor : Editor
    {
        SerializeMesh obj;
 
        void OnSceneGUI()
        {
            obj = (SerializeMesh)target;
        }
 
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
 
            if (GUILayout.Button("Rebuild"))
            {
                if (obj)
                {
                    MeshFilter meshFilter = obj.gameObject.GetComponent<MeshFilter>();
                    if(meshFilter)
                    {
                        meshFilter.mesh = obj.Rebuild();
                    }
                    else
                    {
                        MeshFilter[] meshFilters = obj.gameObject.GetComponentsInChildren<MeshFilter>();
                        foreach(MeshFilter meshF in meshFilters)
                        {
                            meshF.mesh = obj.Rebuild();
                        }
                    }
                }
            }
 
            if (GUILayout.Button("Serialize"))
            {
                if (obj)
                {
                   obj.Serialize();
                }
            }
        }
    }
#endif
}