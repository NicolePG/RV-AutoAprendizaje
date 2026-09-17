using System.Collections;
using UnityEngine;

// Puerta reutilizable: gira sobre su bisagra cuando el acertijo del cuarto se resuelve.
// No sabe nada de acertijos: el método Abrir() se conecta desde el evento "On Solved"
// del PuzzleBase del cuarto (en el Inspector), así sirve para los 4 cuartos.
//
// En la escena: va en el objeto que tiene que girar (la hoja de la puerta), con el
// pivote (el Transform del objeto) ya ubicado en el borde donde va la bisagra.
public class Door : MonoBehaviour
{
    [Tooltip("Cuántos grados gira al abrirse")]
    public float anguloApertura = 90f;

    [Tooltip("Cuánto tarda en abrirse, en segundos")]
    public float duracion = 1.2f;

    [Tooltip("Sonido al abrirse (opcional)")]
    public AudioClip sonido;

    bool abierta;

    public void Abrir()
    {
        if (abierta) return;
        abierta = true;

        if (sonido != null) AudioSource.PlayClipAtPoint(sonido, transform.position);
        StartCoroutine(GirarPuerta());
    }

    IEnumerator GirarPuerta()
    {
        Quaternion inicial = transform.localRotation;
        Quaternion final = inicial * Quaternion.Euler(0f, anguloApertura, 0f);
        float t = 0f;

        while (t < duracion)
        {
            t += Time.deltaTime;
            transform.localRotation = Quaternion.Slerp(inicial, final, t / duracion);
            yield return null;
        }

        transform.localRotation = final;
    }
}
