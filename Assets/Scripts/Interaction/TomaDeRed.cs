using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// Una toma de red de la pared (como las rosetas de una sala de computación).
// Es un encaje (XRSocketInteractor): al soltar una ficha de red cerca, la ficha viaja sola y
// entra en la toma, igual que la llave en la cerradura del Cuarto 1. Solo acepta fichas de red
// (FichaRed): ni la linterna ni otra cosa encajan. Al acercar una ficha se ve su silueta en la
// toma, así se sabe dónde soltarla. La ficha se puede volver a sacar con la mano.
//
// En la escena: va en la toma, junto a un XRSocketInteractor con un collider "Is Trigger".
// ConstructorCuarto3 la arma sola.
[RequireComponent(typeof(XRSocketInteractor))]
public class TomaDeRed : MonoBehaviour, IXRSelectFilter, IXRHoverFilter
{
    [Tooltip("Lucecita de la toma: la prende RedSala (verde = ficha correcta, roja = equivocada)")]
    public Renderer luz;

    [Tooltip("Qué pasa cada vez que se enchufa o se desenchufa una ficha")]
    public UnityEvent alCambiar = new UnityEvent();

    public FichaRed Conectada { get; private set; }

    XRSocketInteractor socket;

    // Filtros: el encaje le pregunta a este script si un objeto puede entrar (o mostrar su silueta)
    public bool canProcess => isActiveAndEnabled;
    public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable) => EsFicha(interactable.transform);
    public bool Process(IXRHoverInteractor interactor, IXRHoverInteractable interactable) => EsFicha(interactable.transform);
    static bool EsFicha(Transform t) => t != null && t.GetComponent<FichaRed>() != null;

    void Awake() => socket = GetComponent<XRSocketInteractor>();

    void OnEnable()
    {
        socket.selectFilters.Add(this);
        socket.hoverFilters.Add(this);
        socket.selectEntered.AddListener(Entro);
        socket.selectExited.AddListener(Salio);
    }

    void OnDisable()
    {
        socket.selectFilters.Remove(this);
        socket.hoverFilters.Remove(this);
        socket.selectEntered.RemoveListener(Entro);
        socket.selectExited.RemoveListener(Salio);
    }

    void Entro(SelectEnterEventArgs args)
    {
        Conectada = args.interactableObject.transform.GetComponent<FichaRed>();
        SonidoSintetico.Tocar(SonidoSintetico.Pitido(2600f, 0.03f), transform.position, 0.7f);   // el clic de la ficha
        alCambiar.Invoke();
    }

    void Salio(SelectExitEventArgs args)
    {
        Conectada = null;
        alCambiar.Invoke();
    }
}
