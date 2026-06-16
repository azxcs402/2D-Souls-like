using System;
using UnityEngine;

[DisallowMultipleComponent]
public class ArenaDoorController : MonoBehaviour
{
    [SerializeField] private GameObject doorRoot;
    [SerializeField] private Collider2D[] blockingColliders = Array.Empty<Collider2D>();
    [SerializeField] private Animator animator;
    [SerializeField] private string openBoolParameter = "open";
    [SerializeField] private bool openOnStart = true;

    public bool IsOpen { get; private set; }

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>(true);
        }

        if (blockingColliders == null || blockingColliders.Length == 0)
        {
            blockingColliders = GetComponentsInChildren<Collider2D>(true);
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

        if (doorRoot != null)
        {
            doorRoot.SetActive(!open);
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
