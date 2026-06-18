using System;
using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public class ArenaDoorController : MonoBehaviour
{
    [FormerlySerializedAs("doorRoot")]
    [SerializeField] private GameObject visualRoot;
    [SerializeField] private GameObject blockingRoot;
    [SerializeField] private Collider2D[] blockingColliders = Array.Empty<Collider2D>();
    [SerializeField] private Animator animator;
    [SerializeField] private string openBoolParameter = "open";
    [SerializeField] private bool openOnStart = true;

    public bool IsOpen { get; private set; }

    private void Awake()
    {
        if (visualRoot == null)
        {
            Transform visualChild = transform.Find("Visual");
            if (visualChild != null)
            {
                visualRoot = visualChild.gameObject;
            }
        }

        if (blockingRoot == null)
        {
            Transform blockingChild = transform.Find("Blocking");
            if (blockingChild != null)
            {
                blockingRoot = blockingChild.gameObject;
            }
        }

        if (animator == null)
        {
            animator = visualRoot != null
                ? visualRoot.GetComponentInChildren<Animator>(true)
                : GetComponentInChildren<Animator>(true);
        }

        if (blockingColliders == null || blockingColliders.Length == 0)
        {
            blockingColliders = blockingRoot != null
                ? blockingRoot.GetComponentsInChildren<Collider2D>(true)
                : GetComponentsInChildren<Collider2D>(true);
        }
    }

    private void Start()
    {
        SetOpen(openOnStart);
    }

    public void SetLocked(bool locked)
    {
        SetOpen(!locked);
    }

    public void SetOpen(bool open)
    {
        IsOpen = open;

        if (visualRoot != null && !visualRoot.activeSelf)
        {
            visualRoot.SetActive(true);
        }

        if (blockingRoot != null)
        {
            blockingRoot.SetActive(!open);
        }

        if (blockingColliders != null)
        {
            for (int i = 0; i < blockingColliders.Length; i++)
            {
                if (blockingColliders[i] != null)
                {
                    blockingColliders[i].enabled = !open;
                }
            }
        }

        if (animator != null && !string.IsNullOrWhiteSpace(openBoolParameter))
        {
            animator.SetBool(openBoolParameter, open);
        }
    }
}
