using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// Cajón que se abre jalando con la mano.
// Mientras el jugador lo agarra, el cajón sigue a la mano pero SOLO sobre su eje Z (hacia adelante/atrás)
// y nunca sale de sus límites: entre cerrado (0) y "aperturaMaxima".
//
// En la escena: va en el objeto raíz del cajón, junto a un XRSimpleInteractable.
// El cajón tiene que empezar CERRADO en el editor: esa posición se guarda como "cerrado".
// Lo que está dentro del cajón debe ser hijo del cajón para moverse con él.
[RequireComponent(typeof(XRSimpleInteractable))]
public class Drawer : MonoBehaviour
{
    [Tooltip("Cuánto se puede abrir el cajón, en metros")]
    public float aperturaMaxima = 0.4f;

    [Tooltip("Si está marcado, el cajón se abre y se cierra con un solo toque, sin jalarlo. " +
             "Con el control a distancia jalar es incómodo, así que en los cajones lejanos conviene.")]
    public bool abrirDeUnToque;

    [Tooltip("Qué tan rápido se abre solo, cuando abre de un toque")]
    public float velocidad = 3f;

    bool abierto;
    XRSimpleInteractable interactable;
    Vector3 posicionCerrado; // posición local con el cajón cerrado
    Vector3 eje;             // dirección en la que se abre (su Z local, en el espacio del padre)
    float aperturaAlAgarrar; // cuánto estaba abierto cuando lo agarraron
    float manoAlAgarrar;     // dónde estaba la mano (sobre el eje) cuando lo agarraron

    void Awake()
    {
        interactable = GetComponent<XRSimpleInteractable>();
        posicionCerrado = transform.localPosition;
        eje = transform.localRotation * Vector3.forward;
    }

    void OnEnable() => interactable.selectEntered.AddListener(AlAgarrar);
    void OnDisable() => interactable.selectEntered.RemoveListener(AlAgarrar);

    void AlAgarrar(SelectEnterEventArgs args)
    {
        if (abrirDeUnToque)
        {
            abierto = !abierto;
            return;
        }

        aperturaAlAgarrar = Vector3.Dot(transform.localPosition - posicionCerrado, eje);
        manoAlAgarrar = PosicionSobreEje(args.interactorObject);
    }

    void Update()
    {
        if (abrirDeUnToque)
        {
            // Se desliza solo hasta quedar abierto o cerrado del todo
            Vector3 destino = posicionCerrado + eje * (abierto ? aperturaMaxima : 0f);
            transform.localPosition = Vector3.Lerp(transform.localPosition, destino,
                                                   1f - Mathf.Exp(-velocidad * Time.deltaTime));
            return;
        }

        if (!interactable.isSelected) return;

        // Cuánto se movió la mano sobre el eje desde que agarró el cajón
        float movimientoMano = PosicionSobreEje(interactable.interactorsSelecting[0]) - manoAlAgarrar;
        float apertura = Mathf.Clamp(aperturaAlAgarrar + movimientoMano, 0f, aperturaMaxima);
        transform.localPosition = posicionCerrado + eje * apertura;
    }

    // Convierte la posición de la mano a coordenadas del padre y la mide sobre el eje del cajón
    float PosicionSobreEje(IXRSelectInteractor mano)
    {
        Vector3 posMano = mano.GetAttachTransform(interactable).position;
        Vector3 local = transform.parent != null ? transform.parent.InverseTransformPoint(posMano) : posMano;
        return Vector3.Dot(local, eje);
    }
}
