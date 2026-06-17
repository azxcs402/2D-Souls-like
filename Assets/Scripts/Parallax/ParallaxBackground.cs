using UnityEngine;

[DefaultExecutionOrder(10000)]
public class ParallaxBackground : MonoBehaviour
{
    private Camera mainCamera;
    private Vector3 startCameraPosition;
    private float cameraHalfWidth;
    private bool initialized;

    [SerializeField] private ParallaxLayer[] backgroundLayers;

    private void Awake()
    {
        InitializeLayers();
    }

    private void OnEnable()
    {
        InitializeLayers();
    }

    private void LateUpdate()
    {
        if (!initialized)
        {
            InitializeLayers();
        }

        if (mainCamera == null)
        {
            return;
        }

        Vector3 currentCameraPosition = mainCamera.transform.position;
        cameraHalfWidth = mainCamera.orthographicSize * mainCamera.aspect;

        float cameraLeftEdge = currentCameraPosition.x - cameraHalfWidth;
        float cameraRightEdge = currentCameraPosition.x + cameraHalfWidth;

        foreach (ParallaxLayer layer in backgroundLayers)
        {
            if (layer == null)
            {
                continue;
            }

            layer.Move(currentCameraPosition, startCameraPosition);
            layer.LoopBackground(cameraLeftEdge, cameraRightEdge);
        }
    }

    private void InitializeLayers()
    {
        mainCamera = Camera.main;

        if (mainCamera == null)
        {
            initialized = false;
            return;
        }

        startCameraPosition = mainCamera.transform.position;
        cameraHalfWidth = mainCamera.orthographicSize * mainCamera.aspect;

        foreach (ParallaxLayer layer in backgroundLayers)
        {
            layer?.Initialize();
        }

        initialized = true;
    }
}
