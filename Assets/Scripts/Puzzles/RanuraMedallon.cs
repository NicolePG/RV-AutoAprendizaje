using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Último paso del juego: la ranura de la puerta de emergencia del laboratorio.
//
// Se habilita recién cuando la secuencia de palancas queda bien. Ahí el jugador la toca:
// si trae el medallón que encontró en el cajón del Cuarto 2 (o sea, si lo tiene en el
// inventario), el medallón entra en la ranura y la puerta se abre. Si todavía no lo
// tiene, se prende la luz roja y suena el error.
//
// Este es el motivo por el que existe el inventario: el objeto se agarra en un cuarto y
// se usa en otro, dos acertijos después.
//
// En la escena: va en la placa de la ranura, junto a un XR Simple Interactable.
[RequireComponent(typeof(XRSimpleInteractable))]
public class RanuraMedallon : MonoBehaviour
{
    [Tooltip("El asset del objeto que hay que traer (el medallón del Cuarto 2)")]
    public ItemData medallon;

    [Tooltip("El medallón que se ve encajado en la ranura: arranca apagado")]
    public GameObject medallonPuesto;

    [Tooltip("Dónde se acomoda el medallón que el jugador trae en la mano")]
    public Transform punto;

    [Tooltip("Luz verde: se prende cuando la ranura queda habilitada")]
    public GameObject luzLista;

    [Tooltip("Luz roja: avisa que falta el medallón")]
    public GameObject luzError;

    public AudioSource sonidoOk;
    public AudioSource sonidoError;

    [Header("Cartel de la ranura: dice en qué estado está")]
    public TMP_Text cartel;
    public string textoBloqueada = "Bloqueada: falta la secuencia de palancas";
    public string textoLista = "Toca aca con el medallon del Cuarto 2";
    public string textoAbierta = "Abierta. Sali del laboratorio";

    [Tooltip("Qué pasa cuando el medallón entra")]
    public UnityEvent alAbrir = new UnityEvent();

    public bool Habilitada { get; private set; }

    XRSimpleInteractable interactable;
    bool usada;

    void Awake()
    {
        interactable = GetComponent<XRSimpleInteractable>();
        if (cartel != null) cartel.text = textoBloqueada;
    }

    void OnEnable() => interactable.selectEntered.AddListener(Tocar);
    void OnDisable() => interactable.selectEntered.RemoveListener(Tocar);

    // Lo llama el acertijo de las palancas cuando la secuencia sale bien
    public void Habilitar()
    {
        if (Habilitada) return;
        Habilitada = true;
        if (luzLista != null) luzLista.SetActive(true);
        if (cartel != null) cartel.text = textoLista;
    }

    void Tocar(SelectEnterEventArgs args)
    {
        if (usada) return;

        // Vale de las dos formas: trayéndolo en la mano o teniéndolo guardado en el
        // inventario (por si el jugador lo soltó en el camino)
        var enMano = ObjetoLlevable.EnLaMano;
        bool loTrae = enMano != null && EsElMedallon(enMano.gameObject);
        bool loTieneGuardado = medallon != null &&
                               Inventory.Instancia != null &&
                               Inventory.Instancia.Tiene(medallon);

        if (!Habilitada || (!loTrae && !loTieneGuardado))
        {
            if (luzError != null) luzError.SetActive(true);
            if (sonidoError != null) sonidoError.Play();
            return;
        }

        usada = true;
        if (luzError != null) luzError.SetActive(false);

        // Si lo trae en la mano, ese mismo medallón queda encajado en la ranura
        if (loTrae && punto != null) enMano.Colocar(punto);
        else if (medallonPuesto != null) medallonPuesto.SetActive(true);

        if (cartel != null) cartel.text = textoAbierta;
        if (sonidoOk != null) sonidoOk.Play();
        alAbrir.Invoke();
    }

    bool EsElMedallon(GameObject objeto)
    {
        var datos = objeto.GetComponentInParent<PickableItem>();
        return datos != null && datos.datos == medallon;
    }
}
