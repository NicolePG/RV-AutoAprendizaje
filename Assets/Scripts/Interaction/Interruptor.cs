using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Interruptor de pared (tecla basculante): cada toque lo cambia de prendido a apagado y al
// revés, con su clic y una lucecita que lo muestra. Lo que hace el interruptor NO está acá:
// se escucha su evento "alAccionar" (por ejemplo, TableroLuces del Cuarto 3).
//
// En la escena: va en el interruptor, junto a un XR Simple Interactable y un collider.
// En "tecla" va la pieza que se inclina.
[RequireComponent(typeof(XRSimpleInteractable))]
public class Interruptor : MonoBehaviour
{
    [Tooltip("La pieza que se inclina al accionarlo")]
    public Transform tecla;

    [Tooltip("Cuántos grados se inclina la tecla para cada lado")]
    public float angulo = 14f;

    [Tooltip("Lucecita que muestra si está prendido (opcional)")]
    public Renderer luz;
    public Material luzPrendida, luzApagada;

    [Tooltip("Sonido del clic (si está vacío se usa un clic generado)")]
    public AudioClip sonido;

    [Tooltip("Qué pasa cada vez que se acciona")]
    public UnityEvent alAccionar = new UnityEvent();

    [Tooltip("Trabado: ya no se puede accionar (por ejemplo, cuando el acertijo está resuelto)")]
    public bool bloqueado;

    public bool Prendido { get; private set; }

    XRSimpleInteractable interactable;
    Quaternion giroInicial;

    void Awake()
    {
        interactable = GetComponent<XRSimpleInteractable>();
        if (tecla != null) giroInicial = tecla.localRotation;
        Mostrar();
    }

    void OnEnable() => interactable.selectEntered.AddListener(Tocar);
    void OnDisable() => interactable.selectEntered.RemoveListener(Tocar);

    void Tocar(SelectEnterEventArgs args)
    {
        if (bloqueado) return;
        Prendido = !Prendido;
        Mostrar();
        SonidoSintetico.Tocar(sonido != null ? sonido : SonidoSintetico.Pitido(2200f, 0.025f), transform.position, 0.8f);
        alAccionar.Invoke();
    }

    void Mostrar()
    {
        if (tecla != null) tecla.localRotation = giroInicial * Quaternion.Euler(Prendido ? -angulo : angulo, 0f, 0f);
        if (luz != null) luz.sharedMaterial = Prendido ? luzPrendida : luzApagada;
    }
}
