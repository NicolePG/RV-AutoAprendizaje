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

    [Tooltip("Segundos hasta que se cierra sola despues de abrirse. 0 = queda abierta")]
    public float segundosParaCerrar;

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
        Quaternion cerrada = transform.localRotation;
        Quaternion final = cerrada * Quaternion.Euler(0f, anguloApertura, 0f);
        yield return Girar(cerrada, final);

        // Se cierra sola: da tiempo a salir y vuelve a su lugar
        if (segundosParaCerrar <= 0f) yield break;
        yield return new WaitForSeconds(segundosParaCerrar);
        if (sonido != null) AudioSource.PlayClipAtPoint(sonido, transform.position);
        yield return Girar(final, cerrada);
    }

    IEnumerator Girar(Quaternion desde, Quaternion hasta)
    {
        float t = 0f;
        while (t < duracion)
        {
            t += Time.deltaTime;
            transform.localRotation = Quaternion.Slerp(desde, hasta, t / duracion);
            yield return null;
        }
        transform.localRotation = hasta;
    }
}
