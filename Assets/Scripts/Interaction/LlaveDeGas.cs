using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Volante de la línea de gas del laboratorio. No se abre de un toque: hay que apuntarle
// y mantener apretado el gatillo mientras el volante da vueltas. Si se suelta antes de
// tiempo, la presión lo devuelve para atrás y hay que empezar de nuevo (despacio, así
// un resbalón no arruina todo el intento).
//
// En la escena: va en el objeto de la llave, junto a un XR Simple Interactable. En
// "volante" va la parte que gira y en "aguja" la del manómetro, si la tiene.
[RequireComponent(typeof(XRSimpleInteractable))]
public class LlaveDeGas : MonoBehaviour
{
    [Tooltip("La parte que gira al abrir")]
    public Transform volante;

    [Tooltip("La aguja del manómetro: se mueve junto con la apertura")]
    public Transform aguja;

    [Tooltip("Cuántos segundos hay que sostenerla para abrirla del todo")]
    public float segundosParaAbrir = 3f;

    [Tooltip("Cuántas vueltas da el volante de cerrado a abierto")]
    public float vueltas = 2f;

    [Tooltip("Cuánto gira la aguja del manómetro, en grados")]
    public float recorridoAguja = 120f;

    [Tooltip("Se prende mientras el jugador la está girando")]
    public GameObject luzGirando;

    [Tooltip("Sonido del gas saliendo mientras se abre")]
    public AudioSource sonido;

    [Tooltip("Qué pasa cuando la llave queda abierta del todo")]
    public UnityEvent alAbrir = new UnityEvent();

    public bool Abierta { get; private set; }
    public float Progreso => progreso;

    XRSimpleInteractable interactable;
    float progreso;
    bool sosteniendo;

    void Awake() => interactable = GetComponent<XRSimpleInteractable>();

    void OnEnable()
    {
        interactable.selectEntered.AddListener(Empezar);
        interactable.selectExited.AddListener(Soltar);
    }

    void OnDisable()
    {
        interactable.selectEntered.RemoveListener(Empezar);
        interactable.selectExited.RemoveListener(Soltar);
    }

    void Empezar(SelectEnterEventArgs args)
    {
        if (Abierta) return;
        sosteniendo = true;
        if (sonido != null && !sonido.isPlaying) sonido.Play();
    }

    void Soltar(SelectExitEventArgs args)
    {
        sosteniendo = false;
        if (sonido != null) sonido.Stop();
    }

    void Update()
    {
        if (Abierta) return;

        if (sosteniendo) progreso += Time.deltaTime / Mathf.Max(0.1f, segundosParaAbrir);
        // Vuelve para atrás mucho más lento de lo que avanza, así soltar sin querer
        // no obliga a empezar todo de nuevo
        else progreso -= Time.deltaTime / Mathf.Max(0.1f, segundosParaAbrir * 4f);

        progreso = Mathf.Clamp01(progreso);

        if (volante != null)
            volante.localRotation = Quaternion.Euler(0f, 0f, 90f) *
                                    Quaternion.Euler(0f, progreso * 360f * vueltas, 0f);

        if (aguja != null)
            aguja.localRotation = Quaternion.Euler(0f, 0f, -progreso * recorridoAguja);

        if (luzGirando != null) luzGirando.SetActive(sosteniendo && progreso < 1f);

        if (progreso < 1f) return;

        Abierta = true;
        sosteniendo = false;
        if (sonido != null) sonido.Stop();
        if (luzGirando != null) luzGirando.SetActive(false);
        interactable.enabled = false;
        alAbrir.Invoke();
    }
}
