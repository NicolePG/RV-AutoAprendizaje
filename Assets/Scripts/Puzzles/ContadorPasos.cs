using UnityEngine;
using UnityEngine.Events;

// Cuenta cuántas veces pasó algo y avisa una sola vez, cuando se llegó al total.
//
// En el Cuarto 2 lo usan los cuatro relojes buenos: cada uno que queda en hora llama
// a Contar(), y recién cuando están los cuatro el televisor muestra el código de la
// puerta. Sin esto el televisor mostraría el código con el primer reloj.
public class ContadorPasos : MonoBehaviour
{
    [Tooltip("Cuántas veces hay que llamar a Contar() para que avise")]
    public int total = 4;

    [Tooltip("Qué pasa cuando se completaron todos")]
    public UnityEvent alCompletar = new UnityEvent();

    public int Hechos { get; private set; }

    public void Contar()
    {
        if (Hechos >= total) return;
        Hechos++;
        if (Hechos >= total) alCompletar.Invoke();
    }
}
