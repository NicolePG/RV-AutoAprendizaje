using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Palanca que se sube con la mano. Mientras el jugador la agarra, subir la mano sube la palanca
// y bajarla la baja, entre anguloAbajo y anguloArriba. Al llegar arriba queda trabada, suena
// y se dispara "alActivar" (por ejemplo, devolver la energía al cuarto).
// Si se suelta antes de llegar arriba, vuelve sola hacia abajo.
//
// Se usa la altura de la mano (no su posición exacta) para que funcione igual agarrándola de cerca
// o apuntándola con el rayo desde lejos.
//
// En la escena: va en el pivote de la palanca (el punto donde gira), junto a un XRSimpleInteractable y
// un collider. La manija apunta hacia el +Z local del pivote y gira sobre su eje X.
// En el editor la palanca debe quedar ABAJO: esa posición se toma como punto de partida.
[RequireComponent(typeof(XRSimpleInteractable))]
public class Lever : MonoBehaviour
{
    [Tooltip("Ángulo de la palanca abajo (negativo = manija hacia abajo)")]
    public float anguloAbajo = -45f;

    [Tooltip("Ángulo de la palanca arriba, donde se activa")]
    public float anguloArriba = 45f;

    [Tooltip("Cuántos grados gira por cada metro que sube la mano")]
    public float gradosPorMetro = 450f;

    [Tooltip("Grados por segundo con los que vuelve abajo si la sueltan antes de activarla")]
    public float velocidadResorte = 180f;

    [Tooltip("Sonido al activarse (opcional)")]
    public AudioClip sonido;

    [Tooltip("Qué pasa cuando la palanca llega arriba")]
    public UnityEvent alActivar = new UnityEvent();

    XRSimpleInteractable interactable;
    Quaternion rotacionCero;   // rotación con la manija derecha hacia afuera (ángulo 0)
    float angulo;
    float anguloAlAgarrar;
    float alturaManoAlAgarrar;
    bool activada;

    void Awake()
    {
        interactable = GetComponent<XRSimpleInteractable>();
        // La palanca empieza abajo: se descuenta ese giro para saber dónde está el ángulo 0
        rotacionCero = transform.localRotation * Quaternion.Inverse(Giro(anguloAbajo));
        angulo = anguloAbajo;
    }

    void OnEnable() => interactable.selectEntered.AddListener(AlAgarrar);
    void OnDisable() => interactable.selectEntered.RemoveListener(AlAgarrar);

    void AlAgarrar(SelectEnterEventArgs args)
    {
        anguloAlAgarrar = angulo;
        alturaManoAlAgarrar = args.interactorObject.GetAttachTransform(interactable).position.y;
    }

    void Update()
    {
        if (activada) return;

        // Si la sueltan antes de llegar arriba, un resorte la devuelve abajo (como un interruptor real)
        if (!interactable.isSelected)
        {
            angulo = Mathf.MoveTowards(angulo, anguloAbajo, velocidadResorte * Time.deltaTime);
            transform.localRotation = rotacionCero * Giro(angulo);
            return;
        }

        // Cuánto subió la mano desde que agarró la palanca
        float alturaMano = interactable.interactorsSelecting[0].GetAttachTransform(interactable).position.y;
        angulo = Mathf.Clamp(anguloAlAgarrar + (alturaMano - alturaManoAlAgarrar) * gradosPorMetro, anguloAbajo, anguloArriba);
        transform.localRotation = rotacionCero * Giro(angulo);

        if (angulo >= anguloArriba - 2f) Activar();
    }

    void Activar()
    {
        activada = true;
        transform.localRotation = rotacionCero * Giro(anguloArriba);
        if (sonido != null) AudioSource.PlayClipAtPoint(sonido, transform.position);
        alActivar.Invoke();
    }

    // Girar sobre X en negativo levanta la manija (que apunta a +Z)
    static Quaternion Giro(float grados) => Quaternion.Euler(-grados, 0, 0);
}
