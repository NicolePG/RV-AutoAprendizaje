using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// Objeto que se guarda en el inventario al agarrarlo por primera vez.
// Al agarrarlo: se avisa al Inventory, la mano vibra (haptics) y el objeto desaparece
// de la escena (ya quedó guardado, no hace falta seguir viéndolo/cargándolo).
//
// En la escena: va en el objeto especial (el que está dentro del cajón),
// junto a un XRGrabInteractable.
[RequireComponent(typeof(XRGrabInteractable))]
public class PickableItem : MonoBehaviour
{
    [Tooltip("El asset de datos de este objeto")]
    public ItemData datos;

    XRGrabInteractable interactable;

    void Awake()
    {
        interactable = GetComponent<XRGrabInteractable>();
    }

    void OnEnable() => interactable.selectEntered.AddListener(AlAgarrar);
    void OnDisable() => interactable.selectEntered.RemoveListener(AlAgarrar);

    void AlAgarrar(SelectEnterEventArgs args)
    {
        Inventory.Instancia.Agregar(datos);
        args.interactorObject.transform.GetComponent<XRBaseInputInteractor>()?.SendHapticImpulse(0.5f, 0.15f);
        gameObject.SetActive(false);
    }
}
