using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Almohadón del sofá que se corre de costado al hacerle un clic.
//
// Antes eran agarrables (XRGrabInteractable) y al tocarlos volaban hacia el jugador,
// que es justo lo que no se quería: el almohadón tiene que deslizarse sobre el asiento,
// como cuando uno lo corre con la mano para ver qué hay debajo.
//
// En la escena: va en el almohadón, junto a un XR Simple Interactable. En "direccion"
// va hacia dónde se corre (en ejes del padre: 0,0,1 lo corre a lo largo del sofá).
[RequireComponent(typeof(XRSimpleInteractable))]
public class CojinDeslizante : MonoBehaviour
{
    [Tooltip("Hacia dónde se corre, en los ejes del objeto que lo contiene")]
    public Vector3 direccion = Vector3.forward;

    [Tooltip("Cuántos metros se corre")]
    public float distancia = 0.5f;

    [Tooltip("Qué tan rápido se desliza")]
    public float velocidad = 4f;

    Vector3 destino;
    bool corrido;
    XRSimpleInteractable interactable;

    void Awake() => interactable = GetComponent<XRSimpleInteractable>();

    void OnEnable() => interactable.selectEntered.AddListener(Tocar);
    void OnDisable() => interactable.selectEntered.RemoveListener(Tocar);

    void Tocar(SelectEnterEventArgs args) => Correr();

    // Se corre una sola vez: ya destapó lo que tapaba y no vuelve solo a su lugar
    public void Correr()
    {
        if (corrido) return;
        corrido = true;
        destino = transform.localPosition + direccion.normalized * distancia;
    }

    void Update()
    {
        if (!corrido) return;
        transform.localPosition = Vector3.Lerp(transform.localPosition, destino,
                                               1f - Mathf.Exp(-velocidad * Time.deltaTime));
    }
}
