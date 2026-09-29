using UnityEngine;
using UnityEngine.Events;

// Tapa sujeta con tornillos (por ejemplo, la tapa de la cerradura).
// Cada tornillo avisa cuando lo quitan. Cuando ya no queda ninguno, la tapa se cae
// y deja ver lo que tapaba.
//
// En la escena: va en la tapa, que necesita un Rigidbody con "Is Kinematic" activado
// (así se queda quieta en la pared hasta que se sueltan todos los tornillos).
[RequireComponent(typeof(Rigidbody))]
public class ScrewedPanel : MonoBehaviour
{
    [Tooltip("Cuántos tornillos sujetan la tapa")]
    public int tornillosRestantes = 2;

    [Tooltip("Qué pasa cuando la tapa se suelta")]
    public UnityEvent alSoltarse = new UnityEvent(); // se crea aquí para que nunca sea null

    public void QuitarTornillo()
    {
        tornillosRestantes--;
        if (tornillosRestantes > 0) return;

        // Se activa la física y se empuja un poco hacia afuera de la pared
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.AddForce(transform.forward * -0.6f, ForceMode.VelocityChange);
        alSoltarse.Invoke();
    }
}
