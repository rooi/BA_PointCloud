using BAPointCloudRenderer.Loading;
using BAPointCloudRenderer.ObjectCreation;
using UnityEngine;
using UnityEditor;
using System;

namespace BAPointCloudRenderer.CloudController {

    /// <summary>
    /// Point Cloud Set to display a large point cloud. All the time, only the points which are needed for the current camera position are loaded from the disk (as described in the thesis).
    /// </summary>
    public class DynamicPointCloudSet : AbstractPointCloudSet {
        /// <summary>
        /// Point Budget - Maximum Number of Points in Memory / to Render
        /// </summary>
        public uint pointBudget = 1000000;
        /// <summary>
        /// Minimum Node Size
        /// </summary>
        public int minNodeSize = 10;
        /// <summary>
        /// Maximum number of nodes loaded per frame
        /// </summary>
        public uint nodesLoadedPerFrame = 15;
        /// <summary>
        /// Maximum number of nodes having their gameobjects created per frame
        /// </summary>
        public uint nodesGOsPerFrame = 30;
        /// <summary>
        /// Cache Size in POints
        /// </summary>
        public uint cacheSizeInPoints = 1000000;
        /// <summary>
        /// Camera to use. If none is specified, Camera.main is used
        /// </summary>
        public Camera userCamera;

        private Vector3 oldCenter;
        private Vector3 oldSize;

        public Action<Vector3, Vector3> OnBoundingBoxUpdated;

        // Use this for initialization
        protected override void Initialize() {
            if (userCamera == null) {
                userCamera = Camera.main;
            }

            PointRenderer = new V2Renderer(this, minNodeSize, pointBudget, nodesLoadedPerFrame, nodesGOsPerFrame, userCamera, meshConfiguration, cacheSizeInPoints);

            oldCenter = overallBoundingBox.Center().ToFloatVector();
            oldSize = overallBoundingBox.Size().ToFloatVector();
        }

        // Update is called once per frame
        void Update()
        {
            if (!CheckReady())
            {
                return;
            }
            PointRenderer.Update();

            UpdateBoxCollider();

            DrawDebugInfo();
        }

        private void UpdateBoxCollider()
        {
            var newCenter = overallBoundingBox.Center().ToFloatVector();
            var newSize = overallBoundingBox.Size().ToFloatVector();
            if (oldCenter != newCenter || oldSize != newSize)
            {
                oldCenter = newCenter;
                oldSize = newSize;
                OnBoundingBoxUpdated?.Invoke(oldCenter, oldSize);
            }
        }
    }
}
