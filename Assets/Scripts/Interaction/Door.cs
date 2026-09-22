using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// Puerta que se abre sola, como una real, cuando se llama a Abrir() (por ejemplo, al resolver un acertijo):
//  1. El pestillo se suelta: la puerta se despega unos grados de golpe.
//  2. Una pequeña pausa.
//  3. Se abre del todo: rápido al principio y frenando al final, como empujada por un brazo cierrapuertas.
// Sirve para cualquier puerta del juego.
//
// Si hace falta, también se cierra sola: cuando el jugador termina de pasar (algo llama a Cerrar(),
// como la zona del otro lado de la puerta del Cuarto 2) o pasados "segundosParaCerrar". Si los dos
// quedan sin usar, la puerta queda abierta para siempre, que es lo que hace la del Cuarto 1.
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

    [Header("Cerrarse sola (opcional)")]
    [Tooltip("Segundos hasta que se cierra sola después de abrirse. 0 = queda abierta")]
    public float segundosParaCerrar;

    [Tooltip("Segundos que espera antes de cerrarse cuando el jugador termina de pasar")]
    public float esperaAlPasar = 2.5f;

    public bool Abierta { get; private set; }

    // true si la hoja está en su lugar, cerrada del todo (no se ve a través de la puerta).
    // Mientras se abre o se cierra da false. Lo usa GestorDeCuartos.
    public bool CerradaDelTodo => Quaternion.Angle(transform.localRotation, rotacionCerrada) < 1f;

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

    // Lo llama la zona que está del otro lado del vano: apenas el jugador termina de
    // pasar, la puerta se cierra atrás suyo (después de "esperaAlPasar", para no darle
    // un portazo en la espalda).
    public void Cerrar()
    {
        if (!Abierta) return;
        Abierta = false;

        StopAllCoroutines();
        StartCoroutine(AnimarCierre(esperaAlPasar));
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

        // Por si el jugador se queda adentro: pasados los segundos se cierra igual
        if (segundosParaCerrar <= 0f) yield break;
        yield return new WaitForSeconds(segundosParaCerrar);
        if (!Abierta) yield break;   // ya la cerró la zona del otro lado

        Abierta = false;
        yield return AnimarCierre(0f);
    }

    IEnumerator AnimarCierre(float espera)
    {
        if (espera > 0f) yield return new WaitForSeconds(espera);

        if (sonido != null) AudioSource.PlayClipAtPoint(sonido, transform.position);
        // Vuelve desde donde esté (abierta del todo o a medio abrir) hasta cerrada
        yield return Girar(AnguloActual(), 0f, 1.2f, true);
    }

    // Cuántos grados está girada ahora respecto de cerrada, entre -180 y 180
    float AnguloActual()
    {
        float angulo = (Quaternion.Inverse(rotacionCerrada) * transform.localRotation).eulerAngles.y;
        return angulo > 180f ? angulo - 360f : angulo;
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
