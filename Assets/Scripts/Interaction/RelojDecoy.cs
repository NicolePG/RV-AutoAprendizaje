using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// Reloj falso del Cuarto 2: al tocarlo dispara el susto (sección 6 del documento).
// Se pone SOLO en los relojes B, C, D y E — el reloj real (A) no lleva este script.
// El susto se dispara una sola vez por partida (límite de la sección 6).
//
// En la escena: va en cada reloj falso, junto a un XRSimpleInteractable.
[RequireComponent(typeof(XRSimpleInteractable))]
public class RelojDecoy : MonoBehaviour
{
    [Tooltip("Renderer del vidrio del reloj, se cambia a resquebrajado durante el susto")]
    public Renderer vidrio;
    public Material materialResquebrajado;

    [Tooltip("Luz que destella durante el susto (opcional)")]
    public Light luzSusto;

    [Tooltip("Sonido grave del susto")]
    public AudioClip sonidoSusto;

    [Tooltip("Cuánto dura el destello, en segundos (menos de 1 segundo)")]
    public float duracionDestello = 0.8f;

    XRSimpleInteractable interactable;
    bool yaDisparado;

    void Awake()
    {
        interactable = GetComponent<XRSimpleInteractable>();
    }

    void OnEnable() => interactable.selectEntered.AddListener(AlTocar);
    void OnDisable() => interactable.selectEntered.RemoveListener(AlTocar);

    void AlTocar(SelectEnterEventArgs args)
    {
        if (yaDisparado) return;
        yaDisparado = true;

        args.interactorObject.transform.GetComponent<XRBaseInputInteractor>()?.SendHapticImpulse(1f, 0.3f);
        if (sonidoSusto != null) AudioSource.PlayClipAtPoint(sonidoSusto, transform.position);
        if (vidrio != null && materialResquebrajado != null) vidrio.sharedMaterial = materialResquebrajado;

        StartCoroutine(Destello());
    }

    IEnumerator Destello()
    {
        if (luzSusto != null) luzSusto.enabled = true;
        yield return new WaitForSeconds(duracionDestello);
        if (luzSusto != null) luzSusto.enabled = false;
    }
}
