using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Botón que se presiona con la mano: tocándolo con el dedo (poke) o apuntándolo y apretando el grip.
// Al presionarlo: la parte móvil baja, suena un clic y se dispara el evento "alPresionar".
// Lo que hace el botón NO está en este script: se conecta en el evento desde el Inspector
// (por ejemplo, mostrar el mensaje del contestador). Así sirve para cualquier botón del juego.
//
// En la escena: va en el objeto del botón, junto a un XRSimpleInteractable, un collider y un AudioSource.
[RequireComponent(typeof(XRSimpleInteractable))]
public class PressableButton : MonoBehaviour
{
    [Tooltip("La pieza que se hunde al presionar (el capuchón del botón)")]
    public Transform parteMovil;

    [Tooltip("Cuánto se hunde, en metros")]
    public float recorrido = 0.008f;

    [Tooltip("Sonido al presionar (opcional)")]
    public AudioClip sonido;

    [Tooltip("Qué pasa al presionar el botón")]
    // Se crea aquí: si un script de editor agrega el componente, el evento todavía no existe
    // y conectarle acciones (como hace Cuarto1Builder) daría NullReferenceException.
    public UnityEvent alPresionar = new UnityEvent();

    XRSimpleInteractable interactable;
    AudioSource audioSource;
    Vector3 posicionInicial;

    void Awake()
    {
        interactable = GetComponent<XRSimpleInteractable>();
        audioSource = GetComponent<AudioSource>();
        if (parteMovil != null) posicionInicial = parteMovil.localPosition;
    }

    void OnEnable()
    {
        interactable.selectEntered.AddListener(Presionar);
        interactable.selectExited.AddListener(Soltar);
    }

    void OnDisable()
    {
        interactable.selectEntered.RemoveListener(Presionar);
        interactable.selectExited.RemoveListener(Soltar);
    }

    void Presionar(SelectEnterEventArgs args)
    {
        if (parteMovil != null) parteMovil.localPosition = posicionInicial + Vector3.down * recorrido;
        if (sonido != null && audioSource != null) audioSource.PlayOneShot(sonido);
        alPresionar.Invoke();
    }

    void Soltar(SelectExitEventArgs args)
    {
        if (parteMovil != null) parteMovil.localPosition = posicionInicial;
    }
}
