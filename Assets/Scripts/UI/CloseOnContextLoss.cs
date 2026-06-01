using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CloseOnContextLoss : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    private bool inContext;
    private bool justClosed;

    [SerializeField] private List<GameObject> GOToEnable;
    [SerializeField] private List<Button> ButtonToEnable;

    private void LateUpdate()
    {
        // Reset after one frame
        justClosed = false;
    }

    void Update()
    {
        // Use MouseButtonDown instead of Up
        if (Input.GetMouseButtonDown(0) && !inContext && !justClosed)
        {
            CloseContext();
        }
    }

    void CloseContext()
    {
        justClosed = true;

        gameObject.SetActive(false);

        foreach (GameObject go in GOToEnable)
        {
            go.SetActive(true);
        }

        foreach (Button b in ButtonToEnable)
        {
            b.interactable = true;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        inContext = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        inContext = false;
    }
}