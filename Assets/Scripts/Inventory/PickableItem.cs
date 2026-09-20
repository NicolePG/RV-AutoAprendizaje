using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// Objeto que se guarda en el inventario la primera vez que el jugador lo agarra.
// Al agarrarlo: se avisa al Inventory y la mano vibra (haptics).
//
// Sirve con las dos formas de agarrar que usa el juego: el XRGrabInteractable de toda
// la vida, o el XRSimpleInteractable de un toque que usan los objetos llevables.
//
// "ocultarAlAgarrar" decide qué pasa con el objeto después: si es true desaparece de la
// escena (ya quedó guardado), y si es false se queda a la vista, que es lo que conviene
// cuando el jugador lo lleva en la mano y tiene que verlo.
//
// En la escena: va en el objeto especial (el del cajón del escritorio).
public class PickableItem : MonoBehaviour
{
    [Tooltip("El asset de datos de este objeto")]
    public ItemData datos;

    [Tooltip("Si está marcado, el objeto desaparece al guardarse")]
    public bool ocultarAlAgarrar = true;

    XRBaseInteractable interactable;

    void Awake() => interactable = GetComponent<XRBaseInteractable>();

    void OnEnable()
    {
        if (interactable != null) interactable.selectEntered.AddListener(AlAgarrar);
    }

    void OnDisable()
    {
        if (interactable != null) interactable.selectEntered.RemoveListener(AlAgarrar);
    }

    void AlAgarrar(SelectEnterEventArgs args)
    {
        if (Inventory.Instancia != null) Inventory.Instancia.Agregar(datos);

        var mano = args.interactorObject.transform.GetComponent<XRBaseInputInteractor>();
        if (mano != null) mano.SendHapticImpulse(0.5f, 0.15f);

        if (ocultarAlAgarrar) gameObject.SetActive(false);
    }
}
