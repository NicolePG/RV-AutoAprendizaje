using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Objeto que se lleva de un lado a otro sin tener que sostener el gatillo.
//
// Un toque y el objeto se queda adelante de la vista, acompañando al jugador. Otro
// toque y lo suelta donde está. Para ponerlo en su lugar se toca el encaje (ver
// Encaje.cs), que es mucho más cómodo que tener que soltarlo justo encima.
//
// Solo se puede llevar una cosa por vez: al agarrar algo, lo que estaba en la mano
// se suelta. "EnLaMano" es lo que el jugador tiene ahora.
//
// En la escena: va en el objeto, junto a un XR Simple Interactable.
[RequireComponent(typeof(XRSimpleInteractable))]
public class ObjetoLlevable : MonoBehaviour
{
    [Tooltip("A qué distancia de la vista se lleva, en metros")]
    public float distancia = 0.42f;

    [Tooltip("Cuánto se baja respecto de la línea de la vista")]
    public float bajar = 0.2f;

    [Tooltip("Qué tan rápido acompaña al jugador")]
    public float velocidad = 9f;

    [Tooltip("Si está marcado, el objeto se queda con el jugador para siempre y viaja a " +
             "un costado de la vista. No ocupa la mano, así que agarrar otra cosa no lo " +
             "suelta. Es lo que hace el medallón, que se usa recién dos acertijos después.")]
    public bool quedaConElJugador;

    // El objeto que el jugador tiene ahora en la mano (solo puede ser uno)
    public static ObjetoLlevable EnLaMano { get; private set; }

    // El que se lleva encima todo el juego, aparte de lo que tenga en la mano
    public static ObjetoLlevable DelJugador { get; private set; }

    public bool Colocado { get; private set; }

    // En qué encaje está puesto ahora, si está en alguno. Lo usa el propio encaje.
    public Encaje EncajeActual { get; set; }

    XRSimpleInteractable interactable;
    Transform camara;
    bool enMano;

    // Dónde estaba al empezar: si el jugador agarra otra cosa, este vuelve acá en vez
    // de quedarse flotando en el aire, así nunca se pierde nada
    Transform padreOriginal;
    Vector3 posicionOriginal;
    Quaternion rotacionOriginal;

    void Awake()
    {
        interactable = GetComponent<XRSimpleInteractable>();
        padreOriginal = transform.parent;
        posicionOriginal = transform.position;
        rotacionOriginal = transform.rotation;
    }

    void OnEnable() => interactable.selectEntered.AddListener(Tocar);

    void OnDisable()
    {
        interactable.selectEntered.RemoveListener(Tocar);
        if (EnLaMano == this) EnLaMano = null;
    }

    void Tocar(SelectEnterEventArgs args)
    {
        if (enMano) { Soltar(); return; }
        Agarrar();
    }

    public void Agarrar()
    {
        // Lo que estaba en la mano se queda donde está. Lo que se lleva encima no
        // molesta: son dos cosas distintas.
        if (!quedaConElJugador && EnLaMano != null && EnLaMano != this) EnLaMano.Soltar();

        // Si estaba puesto en un encaje, ese encaje queda vacío
        if (EncajeActual != null)
        {
            var encaje = EncajeActual;
            EncajeActual = null;
            encaje.Sacar(this);
        }

        transform.SetParent(null, true);
        enMano = true;
        Colocado = false;
        if (quedaConElJugador) DelJugador = this;
        else EnLaMano = this;
        Colisiones(false);
    }

    // Deja de llevarlo y lo devuelve a donde estaba
    public void Soltar()
    {
        DejarDeLlevar();

        transform.SetParent(padreOriginal, true);
        transform.position = posicionOriginal;
        transform.rotation = rotacionOriginal;
    }

    // Lo llama el encaje: el objeto queda acomodado ahí y deja de seguir al jugador
    public void Colocar(Transform destino)
    {
        DejarDeLlevar();
        Colocado = true;

        transform.SetParent(destino, true);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    void DejarDeLlevar()
    {
        enMano = false;
        if (EnLaMano == this) EnLaMano = null;
        if (DelJugador == this) DelJugador = null;
        Colisiones(true);
    }

    void Update()
    {
        if (!enMano) return;

        if (camara == null)
        {
            if (Camera.main == null) return;
            camara = Camera.main.transform;
        }

        // Lo que se lleva encima va a un costado, para no taparle la vista ni confundirse
        // con lo que tiene en la mano
        Vector3 destino = camara.position + camara.forward * distancia - camara.up * bajar;
        if (quedaConElJugador) destino -= camara.right * 0.16f;
        Quaternion giro = Quaternion.LookRotation(camara.forward, Vector3.up);

        float t = 1f - Mathf.Exp(-velocidad * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, destino, t);
        transform.rotation = Quaternion.Slerp(transform.rotation, giro, t);
    }

    void Colisiones(bool prendidas)
    {
        foreach (var col in GetComponentsInChildren<Collider>()) col.enabled = prendidas;
    }
}
