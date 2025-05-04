using UnityEngine;

namespace MRKTExtensions
{
    using MixedReality.Toolkit.SpatialManipulation;
    using System.Threading;
    using System.Threading.Tasks;
    using UnityEngine.XR.Interaction.Toolkit;

    [RequireComponent(typeof(ObjectManipulator))]
    [DisallowMultipleComponent]
    public class BounceBackBehaviour : MonoBehaviour
    {
        [SerializeField]
        [Min(0.01f)]
        private float bounceBackTime = 1f;

        [SerializeField]
        [Min(0.01f)]
        private float bounceBackDelay = 0.5f;

        private Pose originalPose = Pose.identity;
        private CancellationTokenSource cancellationTokenSource;
        private bool initialLocationObtained = false;

        private ObjectManipulator objectManipulator;
        void Awake()
        {
            objectManipulator = GetComponent<ObjectManipulator>();
            objectManipulator.firstSelectEntered.AddListener(OnFirstSelectEntered);
            objectManipulator.lastSelectExited.AddListener(OnLastSelectExited);
        }

        private void OnFirstSelectEntered(SelectEnterEventArgs _)
        {
            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = null;
            if (!initialLocationObtained)
            {
                initialLocationObtained = true;
                originalPose = new Pose(objectManipulator.HostTransform.position, transform.rotation);
            }
        }
        
        private async void OnLastSelectExited(SelectExitEventArgs _)
        {
            cancellationTokenSource = new CancellationTokenSource();
            await Task.Delay((int)(bounceBackDelay * 1000), cancellationTokenSource.Token);
            if (!cancellationTokenSource.IsCancellationRequested)
            {
                await BounceBack();
            }
        }

        private async Task BounceBack()
        {
            var startPos = objectManipulator.HostTransform.position;
            var startRot = objectManipulator.HostTransform.rotation;
            var i = 0f;
            var rate = 1.0f / bounceBackTime;
            while (i <= 1 && cancellationTokenSource is { IsCancellationRequested: false })
            {
                i += Time.deltaTime * rate;
                objectManipulator.HostTransform.SetPositionAndRotation(
                    Vector3.Lerp(startPos, originalPose.position, Mathf.SmoothStep(0f, 1f, i)),
                    Quaternion.Lerp(startRot, originalPose.rotation, Mathf.SmoothStep(0f, 1f, i)));
                await Task.Yield();
            }

            if (cancellationTokenSource is { IsCancellationRequested: false })
            {
                transform.SetPositionAndRotation(originalPose.position, originalPose.rotation);
            }
        }
    }
}
