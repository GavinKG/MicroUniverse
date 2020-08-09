using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class Button : MonoBehaviour, IPointerDownHandler, IPointerUpHandler {

    public UnityEvent OnPointerDownEvent;
    public UnityEvent OnPointerUpEvent;


    public void OnPointerDown(PointerEventData eventData) {
        OnPointerDownEvent.Invoke();
    }

    /// <summary>
    /// Note that when holding the button while setActive(false), pointer up will still be triggered when your release the pointer.
    /// </summary>
    public void OnPointerUp(PointerEventData eventData) {
        OnPointerUpEvent.Invoke();
    }

}
