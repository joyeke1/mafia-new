using UnityEngine;
using UnityEngine.EventSystems; // Essential for EventSystem class

public class EventSystemEnforcer : MonoBehaviour
{
    void Awake()
    {
        // Find all active EventSystem components in the scene
        EventSystem[] existingEventSystems = FindObjectsOfType<EventSystem>();

        // If there's more than one, destroy the others
        if (existingEventSystems.Length > 1)
        {
            foreach (EventSystem es in existingEventSystems)
            {
                // If it's not THIS EventSystem, and it's active, destroy it.
                // This assumes this script is attached to the EventSystem you want to keep.
                if (es.gameObject != this.gameObject)
                {
                    Debug.LogWarning($"EventSystemEnforcer: Destroying duplicate EventSystem '{es.name}'.");
                    Destroy(es.gameObject);
                }
            } 
        }
    }
}