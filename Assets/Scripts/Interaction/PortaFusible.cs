using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// Un portafusibles del tablero eléctrico del laboratorio (Cuarto 4, acertijo 1).
//
// Es un encaje (XRSocketInteractor): al soltar un fusible cerca, entra solo, como la llave
// del Cuarto 1. Solo acepta fusibles sanos. Al entrar, se compara su amperaje con el que
// necesita este circuito:
//  - igual: luz verde y un pitido; el circuito queda listo;
//  - menor: el circuito le pide más de lo que aguanta y se QUEMA (chispazo, humo y queda
//    negro). Hay que sacarlo y probar con otro;
//  - mayor: luz roja y zumbido: está sobredimensionado, no protegería el circuito.
//
// En la escena: va en el portafusibles, junto a un XRSocketInteractor con un collider
// "Is Trigger". ConstructorCuarto4 lo arma.
[RequireComponent(typeof(XRSocketInteractor))]
public class PortaFusible : MonoBehaviour, IXRSelectFilter, IXRHoverFilter
{
    [Tooltip("Amperaje del fusible que corresponde a este circuito")]
    public int amperaje = 10;

    [Tooltip("Lucecita del circuito")]
    public Renderer luz;
    public Material luzVerde, luzRoja, luzApagada;

    [Tooltip("Luz del chispazo cuando se quema un fusible (empieza apagada)")]
    public Light chispa;

    [Tooltip("Humo del chispazo (empieza apagado)")]
    public GameObject humo;

    [Tooltip("Aro ámbar que late mientras el portafusibles está vacío: \"el fusible va acá\"")]
    public GameObject aro;

    [Tooltip("Qué pasa cada vez que se pone o se saca un fusible")]
    public UnityEvent alCambiar = new UnityEvent();

    public FusibleLab Actual { get; private set; }

    // Está bien si tiene el fusible justo, sano
    public bool Correcto => Actual != null && !Actual.Quemado && Actual.amperaje == amperaje;

    XRSocketInteractor socket;
    Vector3 escalaAro;

    // Filtros: solo entran fusibles que no estén quemados. El que se quemó adentro se queda puesto
    // (negro y con la luz roja) hasta que el jugador lo saque: sin el "IsSelecting", XRI lo
    // soltaría solo apenas se quema y se caería al piso.
    public bool canProcess => isActiveAndEnabled;
    public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
        => Acepta(interactable.transform) || socket.IsSelecting(interactable);
    public bool Process(IXRHoverInteractor interactor, IXRHoverInteractable interactable) => Acepta(interactable.transform);

    static bool Acepta(Transform t)
    {
        var fusible = t != null ? t.GetComponent<FusibleLab>() : null;
        return fusible != null && !fusible.Quemado;
    }

    void Awake()
    {
        socket = GetComponent<XRSocketInteractor>();
        if (chispa != null) chispa.enabled = false;
        if (humo != null) humo.SetActive(false);
        if (aro != null) escalaAro = aro.transform.localScale;
        PintarLuz(luzApagada);
    }

    // El aro se ve solo con el portafusibles vacío, y late despacio para llamar la atención
    void Update()
    {
        if (aro == null) return;
        bool vacio = !socket.hasSelection;
        if (aro.activeSelf != vacio) aro.SetActive(vacio);
        if (vacio) aro.transform.localScale = escalaAro * (1f + 0.1f * Mathf.Sin(Time.time * 5f));
    }

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
        Actual = args.interactableObject.transform.GetComponent<FusibleLab>();
        if (Actual == null) return;

        if (Actual.amperaje == amperaje)
        {
            PintarLuz(luzVerde);
            SonidoSintetico.Tocar(SonidoSintetico.Pitido(1320f, 0.12f), transform.position, 0.7f);
        }
        else if (Actual.amperaje < amperaje)
        {
            StartCoroutine(Quemar(Actual));
        }
        else
        {
            PintarLuz(luzRoja);
            SonidoSintetico.Tocar(SonidoSintetico.Zumbido(160f, 0.35f), transform.position);
        }
        alCambiar.Invoke();
    }

    void Salio(SelectExitEventArgs args)
    {
        Actual = null;
        PintarLuz(luzApagada);
        alCambiar.Invoke();
    }

    // El chispazo: dos destellos, un golpe seco y el fusible queda negro
    IEnumerator Quemar(FusibleLab fusible)
    {
        yield return new WaitForSeconds(0.35f);
        if (fusible != Actual) yield break;   // lo sacaron antes

        SonidoSintetico.Tocar(SonidoSintetico.Golpe(), transform.position, 0.9f);
        SonidoSintetico.Tocar(SonidoSintetico.Zumbido(90f, 0.25f), transform.position, 0.6f);
        fusible.Quemar();
        PintarLuz(luzRoja);
        if (humo != null) humo.SetActive(true);
        for (int i = 0; i < 2; i++)
        {
            if (chispa != null) chispa.enabled = true;
            yield return new WaitForSeconds(0.06f);
            if (chispa != null) chispa.enabled = false;
            yield return new WaitForSeconds(0.08f);
        }
        alCambiar.Invoke();
        yield return new WaitForSeconds(1.5f);
        if (humo != null) humo.SetActive(false);
    }

    void PintarLuz(Material m)
    {
        if (luz != null && m != null) luz.sharedMaterial = m;
    }
}
