using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// Puerta que se abre sola, como una real, cuando se llama a Abrir() (por ejemplo, al resolver un acertijo):
//  1. El pestillo se suelta: la puerta se despega unos grados de golpe.
//  2. Una pequeña pausa.
//  3. Se abre del todo: rápido al principio y frenando al final, como empujada por un brazo cierrapuertas.
// Sirve para cualquier puerta del juego.
//
// En la escena: va en la bisagra de la puerta (el objeto vacío en el borde donde gira), no en la hoja.
// La puerta tiene que empezar CERRADA en el editor.
public class Door : MonoBehaviour
{
    [Tooltip("Cuántos grados gira al abrirse del todo (positivo o negativo según hacia dónde abre)")]
    public float anguloAbierto = 90f;

    [Tooltip("Grados que se despega al soltarse el pestillo")]
    public float anguloPestillo = 4f;

    [Tooltip("Segundos de pausa después de soltarse el pestillo")]
    public float pausa = 0.35f;

    [Tooltip("Segundos que tarda en abrirse del todo")]
    public float duracion = 2.2f;

    [Tooltip("Sonido al abrirse (opcional)")]
    public AudioClip sonido;

    [Tooltip("Qué pasa cuando empieza a abrirse (por ejemplo, encender la luz del pasillo; más adelante: guardar)")]
    public UnityEvent alAbrirse = new UnityEvent();

    public bool Abierta { get; private set; }

    Quaternion rotacionCerrada;

    void Awake()
    {
        rotacionCerrada = transform.localRotation;
    }

    public void Abrir()
    {
        if (Abierta) return;
        Abierta = true;
        StartCoroutine(AnimarApertura());
    }

    IEnumerator AnimarApertura()
    {
        if (sonido != null) AudioSource.PlayClipAtPoint(sonido, transform.position);
        alAbrirse.Invoke();
        float direccion = Mathf.Sign(anguloAbierto);

        // 1. Se suelta el pestillo
        yield return Girar(0f, anguloPestillo * direccion, 0.12f, false);

        // 2. Pausa
        yield return new WaitForSeconds(pausa);

        // 3. Se abre frenando al final
        yield return Girar(anguloPestillo * direccion, anguloAbierto, duracion, true);
    }

    IEnumerator Girar(float desde, float hasta, float segundos, bool frenarAlFinal)
    {
        for (float tiempo = 0; tiempo < segundos; tiempo += Time.deltaTime)
        {
            float t = tiempo / segundos;
            // Frenar al final: avanza mucho al principio y casi nada al terminar (1 - (1-t)^3)
            float avance = frenarAlFinal ? 1f - Mathf.Pow(1f - t, 3f) : t;
            transform.localRotation = rotacionCerrada * Quaternion.Euler(0, Mathf.Lerp(desde, hasta, avance), 0);
            yield return null;
        }
        transform.localRotation = rotacionCerrada * Quaternion.Euler(0, hasta, 0);
    }
}
