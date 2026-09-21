using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// Herramientas del juego (la llave y el destornillador) cuando se prueba en el PC con el simulador:
//  1. Se agarran con UN clic y se quedan en la mano sin mantener apretado; otro clic las suelta.
//     Todo lo demás (cajones, palanca, botones, notas) se agarra como siempre: mantener apretado y soltar.
//  2. Mientras están en la mano quedan derechas y mirando al frente, hacia donde mira el jugador.
//     Así la llave entra recta en la cerradura y el destornillador apunta recto al tornillo.
//
// En el Quest no hace nada: ahí la herramienta sigue la mano real y se suelta al soltar el grip.
//
// Cómo funciona el clic: cada mano tiene un modo de agarre ("Select Action Trigger"). Al tomar la herramienta,
// esa mano pasa al modo "Toggle" (un clic agarra, otro suelta). Al soltarla, la mano vuelve a su modo normal,
// pero recién cuando se suelta el botón: si no, el mismo clic que la soltó la volvería a agarrar.
//
// En la escena: va en la herramienta, junto a su XRGrabInteractable (con su Attach Transform: el punto de
// agarre, cuyo eje azul apunta hacia adelante y el verde hacia arriba). Cuarto1Builder lo agrega solo.
[RequireComponent(typeof(XRGrabInteractable))]
public class HerramientaEnMano : MonoBehaviour
{
    [Tooltip("Qué tan rápido se endereza al agarrarla y al girar la vista (más alto = más rápido)")]
    public float velocidadEnderezar = 15f;

    XRGrabInteractable agarre;
    XRBaseInputInteractor mano;              // mano del PC que la tiene agarrada (null si nadie)
    XRBaseInputInteractor manoPorRestaurar;  // mano que la soltó y espera que se suelte el botón
    XRBaseInputInteractor.InputTriggerType modoOriginal;

    void Awake()
    {
        agarre = GetComponent<XRGrabInteractable>();
    }

    void OnEnable()
    {
        agarre.selectEntered.AddListener(AlAgarrar);
        agarre.selectExited.AddListener(AlSoltar);
    }

    void OnDisable()
    {
        agarre.selectEntered.RemoveListener(AlAgarrar);
        agarre.selectExited.RemoveListener(AlSoltar);
        if (mano != null) { manoPorRestaurar = mano; mano = null; agarre.trackRotation = true; }
        RestaurarMano();
    }

    void AlAgarrar(SelectEnterEventArgs args)
    {
        // Solo las manos: la cerradura (socket) también "agarra" la llave, pero no es una mano
        if (!(args.interactorObject is XRBaseInputInteractor nuevaMano)) return;
        // Sin simulador es el Quest real: agarre normal
        if (FindAnyObjectByType<XRInteractionSimulator>() == null) return;

        // Si esa mano todavía estaba en modo clic (acaba de soltar algo), se conserva su modo original
        if (nuevaMano == manoPorRestaurar) manoPorRestaurar = null;
        else modoOriginal = nuevaMano.selectActionTrigger;

        mano = nuevaMano;
        mano.selectActionTrigger = XRBaseInputInteractor.InputTriggerType.Toggle;
        agarre.trackRotation = false; // la rotación la pone este script (derecha y al frente)
    }

    void AlSoltar(SelectExitEventArgs args)
    {
        if ((Object)args.interactorObject != mano) return;
        agarre.trackRotation = true;
        manoPorRestaurar = mano;
        mano = null;
    }

    void Update()
    {
        // La mano vuelve a su modo normal recién cuando se deja de apretar el botón
        if (manoPorRestaurar != null && !manoPorRestaurar.selectInput.ReadIsPerformed())
            RestaurarMano();

        if (mano != null) MirarAlFrente();
    }

    void RestaurarMano()
    {
        if (manoPorRestaurar == null) return;
        manoPorRestaurar.selectActionTrigger = modoOriginal;
        manoPorRestaurar = null;
    }

    // Gira la herramienta para que su punto de agarre mire hacia donde mira el jugador (solo en horizontal)
    // con el "arriba" hacia arriba. La posición la sigue poniendo XRGrabInteractable en la mano.
    void MirarAlFrente()
    {
        Camera camara = Camera.main;
        Transform punto = agarre.attachTransform;
        if (camara == null || punto == null) return;

        Vector3 frente = Vector3.ProjectOnPlane(camara.transform.forward, Vector3.up);
        if (frente.sqrMagnitude < 0.001f) frente = Vector3.ProjectOnPlane(camara.transform.up, Vector3.up); // mirando justo abajo
        Quaternion puntoDeseado = Quaternion.LookRotation(frente, Vector3.up);

        // Rotación del punto de agarre dentro de la herramienta: se descuenta para que sea el punto el que mire al frente
        Quaternion puntoEnHerramienta = Quaternion.Inverse(transform.rotation) * punto.rotation;
        Quaternion objetivo = puntoDeseado * Quaternion.Inverse(puntoEnHerramienta);
        transform.rotation = Quaternion.Slerp(transform.rotation, objetivo, Mathf.Clamp01(velocidadEnderezar * Time.deltaTime));
    }
}
