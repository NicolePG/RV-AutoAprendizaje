using UnityEngine;
using UnityEngine.Events;

// Base de todos los acertijos. Guarda lo que todos tienen en común:
// sus datos (PuzzleData), si ya se resolvió, el sonido de acierto y el evento "alResolverse".
//
// Cada acertijo concreto (KeyPuzzle, KeypadPuzzle...) hereda de esta clase y solo decide CUÁNDO
// está resuelto: en ese momento llama a Resolver(). Así agregar un acertijo nuevo no cambia esta clase.
public abstract class PuzzleBase : MonoBehaviour
{
    [Tooltip("Datos del acertijo (asset en ScriptableObjects/Puzzles)")]
    public PuzzleData datos;

    [Tooltip("Sonido corto al resolverlo")]
    public AudioClip sonidoAcierto;

    [Tooltip("Qué pasa al resolverlo (por ejemplo, abrir la puerta)")]
    public UnityEvent alResolverse = new UnityEvent();

    public bool Resuelto { get; private set; }

    // Lo llama cada acertijo cuando el jugador lo resuelve. Solo tiene efecto la primera vez.
    protected void Resolver()
    {
        if (Resuelto) return;
        Resuelto = true;

        if (sonidoAcierto != null) AudioSource.PlayClipAtPoint(sonidoAcierto, transform.position);
        MostrarAcierto();
        alResolverse.Invoke();
    }

    // Cada acertijo muestra su propio feedback (luces, textos). Por defecto no hace nada.
    protected virtual void MostrarAcierto() { }
}
