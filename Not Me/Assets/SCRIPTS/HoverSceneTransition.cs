using UnityEngine;
using UnityEngine.SceneManagement;

public class HoverSceneTransition : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private string sceneToLoad = "NextScene";
    [SerializeField] private float transitionDelay = 0.5f;

    [Header("Left Hand Anchor")]
    [Tooltip("Drag your OVRCameraRig > TrackingSpace > LeftHandAnchor here")]
    [SerializeField] private Transform leftHandAnchor;

    [Header("Optional Transition Effects")]
    [SerializeField] private bool fadeTransition = true;
    [SerializeField] private float fadeTime = 1f;

    private bool hasTriggered = false;

    void Start()
    {
        var col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogError("HoverSceneTransition: No Collider found on " + gameObject.name);
        }
        else if (!col.isTrigger)
        {
            col.isTrigger = true;
        }

        // Auto-find left hand anchor if not assigned
        if (leftHandAnchor == null)
        {
            var rig = FindObjectOfType<OVRCameraRig>();
            if (rig != null)
                leftHandAnchor = rig.leftHandAnchor;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;

        // Only fire if the collider belongs to the left hand anchor hierarchy
        if (leftHandAnchor == null) return;
        if (!other.transform.IsChildOf(leftHandAnchor) && other.transform != leftHandAnchor)
            return;

        hasTriggered = true;
        Debug.Log("Left hand touched " + gameObject.name + "! Transitioning to: " + sceneToLoad);

        if (string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.LogError("Scene name is empty! Set it in the inspector.");
            return;
        }

        if (transitionDelay > 0)
            Invoke(nameof(LoadScene), transitionDelay);
        else
            LoadScene();
    }

    private void LoadScene()
    {
        // Disable GrabAndLocate components to prevent null refs during transition
        var grabAndLocates = FindObjectsOfType<Meta.XR.MRUtilityKit.BuildingBlocks.GrabAndLocate>();
        foreach (var component in grabAndLocates)
        {
            if (component != null)
                component.enabled = false;
        }

        if (fadeTransition)
            StartCoroutine(FadeAndLoadScene());
        else
            SceneManager.LoadScene(sceneToLoad);
    }

    private System.Collections.IEnumerator FadeAndLoadScene()
    {
        yield return new WaitForSeconds(fadeTime);
        SceneManager.LoadScene(sceneToLoad);
    }
}