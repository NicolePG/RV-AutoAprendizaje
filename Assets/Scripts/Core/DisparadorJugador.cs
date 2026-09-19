using UnityEngine;
using UnityEngine.Events;

// Avisa una sola vez cuando el jugador entra en una zona. Se usa para cerrar la puerta
// de entrada apenas el jugador pisa el Cuarto 2, así no puede volverse al cuarto anterior.
//
// En la escena: va en un objeto con un collider marcado como "Is Trigger".
[RequireComponent(typeof(Collider))]
public class DisparadorJugador : MonoBehaviour
{
    [Tooltip("Qué pasa cuando el jugador entra")]
    public UnityEvent alEntrar = new UnityEvent();

    bool disparado;

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider otro)
    {
        if (disparado) return;
        // El jugador es el XR Origin: lo que lo distingue es que lleva la cámara adentro
        if (otro.GetComponentInChildren<Camera>() == null && otro.GetComponentInParent<Camera>() == null) return;

        disparado = true;
        alEntrar.Invoke();
    }
}
