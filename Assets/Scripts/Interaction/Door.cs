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

    [Tooltip("Segundos que espera antes de cerrarse cuando el jugador termina de pasar")]
    public float esperaAlPasar = 2.5f;

    bool abierta;
    Quaternion rotacionCerrada;

    void Awake() => rotacionCerrada = transform.localRotation;

    public void Abrir()
    {
        if (abierta) return;
        abierta = true;

        if (sonido != null) AudioSource.PlayClipAtPoint(sonido, transform.position);
        StartCoroutine(GirarPuerta());
    }

    // Se conecta al disparador que está del otro lado del vano: apenas el jugador
    // termina de pasar, la puerta se cierra atrás suyo.
    public void Cerrar()
    {
        if (!abierta) return;
        abierta = false;

        StopAllCoroutines();
        StartCoroutine(CerrarDespues());
    }

    IEnumerator CerrarDespues()
    {
        // Un respiro antes de cerrarse, para no darle un portazo al jugador en la espalda
        if (esperaAlPasar > 0f) yield return new WaitForSeconds(esperaAlPasar);

        if (sonido != null) AudioSource.PlayClipAtPoint(sonido, transform.position);
        yield return Girar(transform.localRotation, rotacionCerrada);
    }

    IEnumerator GirarPuerta()
    {
        Quaternion final = rotacionCerrada * Quaternion.Euler(0f, anguloApertura, 0f);
        yield return Girar(transform.localRotation, final);

        // Por si el jugador se queda adentro: pasados los segundos se cierra igual
        if (segundosParaCerrar <= 0f) yield break;
        yield return new WaitForSeconds(segundosParaCerrar);
        if (!abierta) yield break;   // ya la cerró el disparador de la salida

        abierta = false;
        if (sonido != null) AudioSource.PlayClipAtPoint(sonido, transform.position);
        yield return Girar(transform.localRotation, rotacionCerrada);
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
