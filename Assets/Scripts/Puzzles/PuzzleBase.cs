using UnityEngine;
using UnityEngine.Events;

// Base común para todos los acertijos del juego (teclado, llave, fusibles, palancas).
// Cada acertijo concreto (por ejemplo KeypadPuzzle) hereda de esta clase y decide
// CUÁNDO llamar a Resolver(); esta clase solo se encarga de qué pasa una vez resuelto:
// avisa una sola vez (por si se llama de nuevo por error) y dispara el evento OnSolved,
// del que escucha la puerta del cuarto (Door.cs).
public abstract class PuzzleBase : MonoBehaviour
{
    [Tooltip("El asset de datos de este acertijo (pista, solución, mensajes)")]
    public PuzzleData datos;

    [Tooltip("Se dispara una sola vez, cuando el acertijo se resuelve")]
    public UnityEvent OnSolved = new UnityEvent();

    public bool Resuelto { get; private set; }

    protected void Resolver()
    {
        if (Resuelto) return;
        Resuelto = true;

        if (datos != null) Debug.Log(datos.mensajeAcierto);
        OnSolved.Invoke();
    }
}
