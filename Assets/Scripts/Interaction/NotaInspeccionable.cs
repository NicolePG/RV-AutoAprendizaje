using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Hoja que se lee de cerca: al tocarla viene hasta enfrente de la vista del jugador,
// más grande y derecha para leerla cómoda. Mientras está enfocada acompaña la cabeza.
// Al tocarla de nuevo vuelve a su lugar en el escritorio.
//
// En la escena: va en la hoja (con el texto como hijo, apoyado sobre su cara de arriba),
// junto a un XRSimpleInteractable. La cámara del jugador tiene que tener el tag MainCamera.
[RequireComponent(typeof(XRSimpleInteractable))]
public class NotaInspeccionable : MonoBehaviour
{
    [Tooltip("A cuántos metros de la vista queda la hoja al enfocarla")]
    public float distancia = 0.45f;

    [Tooltip("Cuánto se baja respecto de la línea de la vista, en metros")]
    public float bajar = 0.06f;

    [Tooltip("Cuánto se agranda al enfocarla")]
    public float escalaEnfocada = 1.3f;

    [Tooltip("Qué tan rápido viaja entre el escritorio y la vista")]
    public float velocidad = 8f;

    public bool Enfocada { get; private set; }

    XRSimpleInteractable interactable;
    Transform camara;
    Vector3 posicionOriginal;
    Quaternion rotacionOriginal;
    Vector3 escalaOriginal;

    void Awake()
    {
        interactable = GetComponent<XRSimpleInteractable>();
        posicionOriginal = transform.position;
        rotacionOriginal = transform.rotation;
        escalaOriginal = transform.localScale;
    }

    void OnEnable() => interactable.selectEntered.AddListener(Alternar);
    void OnDisable() => interactable.selectEntered.RemoveListener(Alternar);

    void Alternar(SelectEnterEventArgs args)
    {
        if (camara == null && Camera.main != null) camara = Camera.main.transform;
        Enfocada = !Enfocada && camara != null;
    }

    void Update()
    {
        Vector3 posicion = posicionOriginal;
        Quaternion rotacion = rotacionOriginal;
        Vector3 escala = escalaOriginal;

        if (Enfocada)
        {
            posicion = camara.position + camara.forward * distancia - camara.up * bajar;
            // La cara con el texto mira a la cámara y la parte de arriba del texto
            // queda para arriba de la vista
            rotacion = Quaternion.LookRotation(-camara.up, -camara.forward);
            escala = escalaOriginal * escalaEnfocada;
        }

        float t = 1f - Mathf.Exp(-velocidad * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, posicion, t);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotacion, t);
        transform.localScale = Vector3.Lerp(transform.localScale, escala, t);
    }
}
