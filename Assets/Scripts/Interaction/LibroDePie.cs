using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Libro apoyado que, al tocarlo, se levanta y se pone de frente al jugador para que
// pueda leer la tapa. Al tocarlo de nuevo vuelve a quedar acostado como estaba.
//
// En la escena: va en el libro (con el texto sobre su cara de arriba), junto a un
// XRSimpleInteractable. La cámara del jugador tiene que tener el tag MainCamera.
[RequireComponent(typeof(XRSimpleInteractable))]
public class LibroDePie : MonoBehaviour
{
    [Tooltip("Cuánto se levanta al pararse, en metros")]
    public float alturaExtra = 0.14f;

    [Tooltip("Cuánto se acerca al jugador al pararse, en metros")]
    public float acercar = 0.06f;

    [Tooltip("Qué tan rápido se para y se acuesta")]
    public float velocidad = 6f;

    public bool Parado { get; private set; }

    XRSimpleInteractable interactable;
    Transform camara;
    Vector3 posicionOriginal;
    Quaternion rotacionOriginal;

    void Awake()
    {
        interactable = GetComponent<XRSimpleInteractable>();
        posicionOriginal = transform.position;
        rotacionOriginal = transform.rotation;
    }

    void OnEnable() => interactable.selectEntered.AddListener(Alternar);
    void OnDisable() => interactable.selectEntered.RemoveListener(Alternar);

    void Alternar(SelectEnterEventArgs args)
    {
        if (camara == null && Camera.main != null) camara = Camera.main.transform;
        Parado = !Parado && camara != null;
    }

    void Update()
    {
        Vector3 posicion = posicionOriginal;
        Quaternion rotacion = rotacionOriginal;

        if (Parado)
        {
            Vector3 haciaJugador = camara.position - posicionOriginal;
            haciaJugador.y = 0f;
            if (haciaJugador.sqrMagnitude > 0.0001f)
            {
                haciaJugador.Normalize();
                posicion = posicionOriginal + Vector3.up * alturaExtra + haciaJugador * acercar;
                // El texto está impreso sobre la cara de arriba de la tapa, o sea que mira
                // hacia el +Y del libro y su renglón va hacia el +Z. Para leerlo parado hay
                // que dejar el +Y apuntando al jugador y el +Z apuntando al cielo: eso es
                // LookRotation(arriba, haciaJugador). Con Vector3.down el texto salía dado
                // vuelta y no se entendía nada.
                rotacion = Quaternion.LookRotation(Vector3.up, haciaJugador);
            }
        }

        float t = 1f - Mathf.Exp(-velocidad * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, posicion, t);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotacion, t);
    }
}
