using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Llave que se lleva de un clic, sin tener que mantener el gatillo apretado.
//
// 1) El jugador le hace un clic y la llave se le queda pegada al mando, en la punta,
//    como si la llevara en la mano. No se cae ni hay que sostener nada.
// 2) Hay que caminar (o teletransportarse) hasta la puerta: cuando la llave está cerca
//    de la cerradura aparece una "sombra de llave" en la ranura, marcando dónde va.
// 3) Al hacerle clic a esa sombra, la llave viaja sola hasta la ranura y se encaja.
// 4) Ya encajada, tocarla la hace girar y la puerta se abre.
//
// En la escena: va en la llave, junto a un XR Simple Interactable. En "ranura" va el
// objeto vacío que está dentro de la cerradura de la puerta, y en "fantasma" la llave
// de sombra que cuelga de esa misma ranura.
[RequireComponent(typeof(XRSimpleInteractable))]
public class LlaveAutomatica : MonoBehaviour
{
    [Tooltip("El punto de la cerradura donde tiene que entrar la llave")]
    public Transform ranura;

    [Tooltip("La llave de sombra que se ve en la cerradura cuando el jugador se acerca")]
    public GameObject fantasma;

    [Tooltip("A qué distancia de la ranura aparece la sombra, en metros")]
    public float distanciaParaEncajar = 1.6f;

    [Tooltip("Qué tan rápido sigue al mando y viaja hasta la cerradura")]
    public float velocidad = 9f;

    [Tooltip("Dónde se apoya respecto del mando: por defecto en la punta")]
    public Vector3 enLaMano = new Vector3(0f, 0f, 0.12f);

    [Tooltip("Qué pasa cuando la llave queda encajada")]
    public UnityEvent alEncajar = new UnityEvent();

    [Tooltip("Qué pasa cuando el jugador toca la llave ya encajada, para girarla")]
    public UnityEvent alGirar = new UnityEvent();

    public bool EnMano { get; private set; }
    public bool Encajada { get; private set; }

    XRSimpleInteractable interactable;
    Transform mano;      // el mando que la lleva
    bool entrando;       // está viajando hacia la ranura

    void Awake()
    {
        interactable = GetComponent<XRSimpleInteractable>();
        if (fantasma != null) fantasma.SetActive(false);
    }

    void OnEnable() => interactable.selectEntered.AddListener(Tocar);
    void OnDisable() => interactable.selectEntered.RemoveListener(Tocar);

    void Tocar(SelectEnterEventArgs args)
    {
        // Ya puesta en la cerradura: este toque es el jugador girándola
        if (Encajada)
        {
            alGirar.Invoke();
            return;
        }

        if (EnMano || entrando) return;

        EnMano = true;
        // El mando que le hizo clic es el que se la lleva
        mano = args.interactorObject != null ? args.interactorObject.transform : null;
        // Mientras la lleva no tiene que taparle el rayo al mando
        Colisiones(false);
    }

    // La llama la sombra de la llave cuando el jugador le hace clic
    public void Encajar()
    {
        if (Encajada || !EnMano || ranura == null) return;

        EnMano = false;
        entrando = true;
        if (fantasma != null) fantasma.SetActive(false);
    }

    void Update()
    {
        if (Encajada) return;

        // Viajando sola hasta la cerradura
        if (entrando)
        {
            Viajar(ranura.position, ranura.rotation, true);
            return;
        }

        if (!EnMano) return;

        // Si el mando se perdió (por ejemplo al cambiar de control), se usa la vista
        Transform guia = mano != null ? mano : (Camera.main != null ? Camera.main.transform : null);
        if (guia == null) return;

        Viajar(guia.TransformPoint(enLaMano), guia.rotation, false);

        // Cerca de la cerradura aparece la sombra: ahí hay que hacerle clic
        if (fantasma != null)
            fantasma.SetActive(ranura != null &&
                               Vector3.Distance(transform.position, ranura.position) <= distanciaParaEncajar);
    }

    void Viajar(Vector3 destino, Quaternion giro, bool haciaLaRanura)
    {
        float t = 1f - Mathf.Exp(-velocidad * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, destino, t);
        transform.rotation = Quaternion.Slerp(transform.rotation, giro, t);

        if (!haciaLaRanura || Vector3.Distance(transform.position, destino) > 0.015f) return;

        // Llegó: queda colgada de la ranura, así después gira junto con ella
        transform.SetParent(ranura, true);
        transform.position = destino;
        transform.rotation = giro;

        entrando = false;
        Encajada = true;
        // Se le devuelven los colliders: ahora hay que tocarla para girarla
        Colisiones(true);
        alEncajar.Invoke();
    }

    void Colisiones(bool prendidas)
    {
        foreach (var col in GetComponentsInChildren<Collider>()) col.enabled = prendidas;
    }
}
