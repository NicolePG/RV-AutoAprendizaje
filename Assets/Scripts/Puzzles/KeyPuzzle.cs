using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// Acertijo del Cuarto 1: encajar la llave correcta en la cerradura.
//
// Va junto a un XRSocketInteractor (el "encaje" de la cerradura). Funciona también como filtro del socket:
// solo deja entrar la llave indicada, así el destornillador o la nota no encajan. Al acercar la llave,
// el socket dibuja una silueta transparente de la llave en la cerradura: muestra dónde soltarla.
// El jugador suelta la llave cerca de la cerradura (donde brilla el indicador) y pasa lo mismo que con una real:
//  1. La llave vuela sola a su lugar y queda puesta: ya no se puede sacar.
//  2. Entra en la cerradura, gira 90° y suena el clic.
//  3. La tira de luz pasa a verde, el texto cambia a "ABIERTO" y se dispara "alResolverse" (que abre la puerta).
//
// En la escena: va en el objeto Socket_Llave, junto al XRSocketInteractor y un collider "Is Trigger".
// En el Inspector se arrastran la llave, la tira de luz, el texto de estado y la luz de la cerradura.
[RequireComponent(typeof(XRSocketInteractor))]
public class KeyPuzzle : PuzzleBase, IXRSelectFilter, IXRHoverFilter
{
    [Tooltip("La única llave que abre esta cerradura")]
    public XRGrabInteractable llave;

    [Tooltip("Luz que marca dónde va la llave. Se apaga cuando la llave entra")]
    public GameObject indicador;

    [Header("Colocar y girar la llave")]
    [Tooltip("Segundos que se espera a que la llave llegue sola a la cerradura")]
    public float tiempoParaAcomodarse = 0.35f;

    [Tooltip("Cuántos metros entra la llave en la cerradura")]
    public float profundidadEntrada = 0.03f;

    [Tooltip("Segundos que tarda en entrar")]
    public float duracionEntrada = 0.3f;

    [Tooltip("Grados que gira la llave antes de abrir")]
    public float giroLlave = 90f;

    [Tooltip("Segundos que tarda en girar")]
    public float duracionGiro = 0.6f;

    [Tooltip("Sonido del clic al terminar de girar (opcional)")]
    public AudioClip sonidoGiro;

    [Header("Feedback de acierto")]
    public Renderer tiraDeLuz;
    public Material materialVerde;
    public TMP_Text textoEstado;
    public Light luzCerradura;

    XRSocketInteractor socket;
    bool girando;

    // Filtros: el socket le pregunta a este script si un objeto puede encajar (select)
    // o si puede mostrar su silueta (hover). Solo la llave.
    public bool canProcess => isActiveAndEnabled;

    public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
    {
        return (Object)interactable == llave;
    }

    public bool Process(IXRHoverInteractor interactor, IXRHoverInteractable interactable)
    {
        return (Object)interactable == llave;
    }

    void Awake()
    {
        socket = GetComponent<XRSocketInteractor>();
    }

    void OnEnable()
    {
        socket.selectFilters.Add(this);
        socket.hoverFilters.Add(this);
        socket.selectEntered.AddListener(AlEncajar);
    }

    void OnDisable()
    {
        socket.selectFilters.Remove(this);
        socket.hoverFilters.Remove(this);
        socket.selectEntered.RemoveListener(AlEncajar);
    }

    void AlEncajar(SelectEnterEventArgs args)
    {
        if (Resuelto || girando || (Object)args.interactableObject != llave) return;
        StartCoroutine(GirarLlaveYAbrir());
    }

    IEnumerator GirarLlaveYAbrir()
    {
        girando = true;
        if (indicador != null) indicador.SetActive(false);

        // Se espera a que la llave termine de volar suavemente hasta la cerradura (Attach Ease In Time de la llave)
        yield return new WaitForSeconds(tiempoParaAcomodarse);
        Transform t = llave.transform;
        Rigidbody cuerpo = llave.GetComponent<Rigidbody>();
        t.GetPositionAndRotation(out Vector3 posicion, out Quaternion rotacion);

        // La llave queda puesta: se apaga el encaje (que la suelta), se desactiva el agarre para que la mano
        // no pueda sacarla y se le quita la física para que no se caiga. Queda como hija de la cerradura.
        socket.enabled = false;
        llave.enabled = false;
        cuerpo.isKinematic = true;
        t.SetPositionAndRotation(posicion, rotacion);
        t.SetParent(transform, true);

        // 1. Entra en la cerradura: avanza por su eje (la punta de la llave es su +X local)
        Vector3 afuera = t.localPosition;
        Vector3 adentro = afuera + t.localRotation * Vector3.right * profundidadEntrada;
        yield return Animar(duracionEntrada, avance => t.localPosition = Vector3.Lerp(afuera, adentro, avance));

        // 2. Gira sobre su eje hasta abrir
        Quaternion inicio = t.localRotation;
        Quaternion fin = inicio * Quaternion.Euler(giroLlave, 0, 0);
        yield return Animar(duracionGiro, avance => t.localRotation = Quaternion.Slerp(inicio, fin, avance));
        if (sonidoGiro != null) AudioSource.PlayClipAtPoint(sonidoGiro, t.position);

        Resolver();
    }

    // Repite "paso" durante "duracion" segundos con un avance de 0 a 1 suave al empezar y al terminar
    static IEnumerator Animar(float duracion, System.Action<float> paso)
    {
        for (float tiempo = 0; tiempo < duracion; tiempo += Time.deltaTime)
        {
            paso(Mathf.SmoothStep(0f, 1f, tiempo / duracion));
            yield return null;
        }
        paso(1f);
    }

    protected override void MostrarAcierto()
    {
        Color verde = new Color(0.2f, 0.9f, 0.35f);
        if (tiraDeLuz != null && materialVerde != null) tiraDeLuz.sharedMaterial = materialVerde;
        if (textoEstado != null)
        {
            textoEstado.text = datos != null ? datos.mensajeAcierto : "ABIERTO";
            textoEstado.color = verde;
        }
        if (luzCerradura != null) luzCerradura.color = verde;
    }
}
