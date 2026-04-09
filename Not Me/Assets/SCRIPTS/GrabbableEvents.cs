using UnityEngine;
using UnityEngine.Events;
using Oculus.Interaction;

public class GrabbableEvents : MonoBehaviour
{
    private Grabbable grabbable;

    [Header("Interaction Events")]
    public UnityEvent hoverEvent;
    public UnityEvent unhoverEvent;
    public UnityEvent selectEvent;
    public UnityEvent unselectEvent;
    public UnityEvent moveEvent;
    public UnityEvent cancelEvent;

    void Start()
    {
        grabbable = GetComponent<Grabbable>();

        if (grabbable == null)
        {
            Debug.LogError("No Grabbable component found on " + gameObject.name);
            return;
        }

        // subscribe to all pointer events
        grabbable.WhenPointerEventRaised += OnPointerEvent;
    }

    void OnDestroy()
    {
        if (grabbable != null)
        {
            grabbable.WhenPointerEventRaised -= OnPointerEvent;
        }
    }

    private void OnPointerEvent(PointerEvent pointerEvent)
    {
        // switch for every event type that occurs
        switch (pointerEvent.Type)
        {
            case PointerEventType.Hover:
                //Debug.Log("[" + gameObject.name + "] HOVER - Hand is hovering");
                hoverEvent.Invoke();
                break;

            case PointerEventType.Unhover:
                //Debug.Log("[" + gameObject.name + "] UNHOVER - Hand stopped hovering");
                unhoverEvent.Invoke();
                break;

            case PointerEventType.Select:
                //Debug.Log("[" + gameObject.name + "] SELECT - Object grabbed!");
                selectEvent.Invoke();
                break;

            case PointerEventType.Unselect:
                //Debug.Log("[" + gameObject.name + "] UNSELECT - Object released!");
                unselectEvent.Invoke();
                break;

            case PointerEventType.Move:
                //Debug.Log("[" + gameObject.name + "] MOVE - Moving while grabbed");
                moveEvent.Invoke();
                break;

            case PointerEventType.Cancel:
                //Debug.Log("[" + gameObject.name + "] CANCEL - Interaction cancelled");
                cancelEvent.Invoke();
                break;

            default:
                //Debug.Log("[" + gameObject.name + "] UNKNOWN EVENT: " + pointerEvent.Type);
                break;
        }
    }
}