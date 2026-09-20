using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Hueco donde se pone un objeto que el jugador trae en la mano (ver ObjetoLlevable).
//
// Tocar el hueco con algo en la mano lo deja puesto ahí, acomodado solo. Tocarlo de
// nuevo, con la mano vacía, lo saca y vuelve a la mano: así se puede corregir sin
// tener que empezar de cero.
//
// Es mucho más cómodo que un socket de XRI, porque el jugador no tiene que soltar el
// objeto justo encima del hueco: le apunta al hueco y listo.
//
// En la escena: va en el hueco, junto a un XR Simple Interactable.
[RequireComponent(typeof(XRSimpleInteractable))]
public class Encaje : MonoBehaviour
{
    [Tooltip("Dónde se acomoda el objeto. Si está vacío, se usa este mismo objeto")]
    public Transform punto;

    [Tooltip("Sonido al poner o sacar")]
    public AudioSource sonido;

    [Tooltip("Qué pasa cada vez que entra o sale algo")]
    public UnityEvent alCambiar = new UnityEvent();

    public ObjetoLlevable Contenido { get; private set; }

    XRSimpleInteractable interactable;

    void Awake() => interactable = GetComponent<XRSimpleInteractable>();

    void OnEnable() => interactable.selectEntered.AddListener(Tocar);
    void OnDisable() => interactable.selectEntered.RemoveListener(Tocar);

    void Tocar(SelectEnterEventArgs args)
    {
        var enMano = ObjetoLlevable.EnLaMano;

        // Con la mano vacía y algo puesto: lo saca y se lo lleva
        if (enMano == null)
        {
            if (Contenido == null) return;
            var sale = Contenido;
            Contenido = null;
            sale.Agarrar();
            Avisar();
            return;
        }

        // Con algo en la mano: si el hueco está ocupado, no se pisa lo que ya hay
        if (Contenido != null) return;

        Contenido = enMano;
        enMano.Colocar(punto != null ? punto : transform);
        Avisar();
    }

    void Avisar()
    {
        if (sonido != null) sonido.Play();
        alCambiar.Invoke();
    }
}
