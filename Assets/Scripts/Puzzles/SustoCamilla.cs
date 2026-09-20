using System.Collections;
using UnityEngine;

// El susto del laboratorio, el más fuerte de los cuatro cuartos.
//
// Salta mientras el jugador está girando la segunda llave de gas: tiene las dos manos
// ocupadas, sosteniendo, y la camilla le queda al costado. Ahí la sábana se cae de
// golpe, el cuerpo se incorpora en la camilla y suena el grito.
//
// Pasa una sola vez por partida.
//
// En la escena: va en el objeto "Camilla". En "torso" va el pivote que se incorpora
// (el que tiene adentro el tronco, la cabeza y los brazos), en "sabana" la sábana que
// tapa y en "sabanaCaida" la que ya está en el piso.
public class SustoCamilla : MonoBehaviour
{
    [Tooltip("El pivote del tronco: gira sobre la cintura para incorporarse")]
    public Transform torso;

    [Tooltip("La sábana tapando el cuerpo")]
    public GameObject sabana;

    [Tooltip("La sábana caída al costado de la camilla: arranca apagada")]
    public GameObject sabanaCaida;

    [Tooltip("Destello que acompaña el susto")]
    public Light destello;

    [Tooltip("El grito")]
    public AudioSource grito;

    [Tooltip("Cuántos grados se incorpora")]
    public float angulo = 62f;

    [Tooltip("Cuánto tarda en incorporarse: cuanto más corto, más de golpe")]
    public float duracion = 0.28f;

    public bool Disparado { get; private set; }

    void Awake()
    {
        if (destello != null) destello.enabled = false;
        if (sabanaCaida != null) sabanaCaida.SetActive(false);
    }

    // Lo llama la segunda llave de gas cuando va por la mitad del giro
    public void Disparar()
    {
        if (Disparado) return;
        Disparado = true;
        StartCoroutine(Asustar());
    }

    IEnumerator Asustar()
    {
        if (sabana != null) sabana.SetActive(false);
        if (sabanaCaida != null) sabanaCaida.SetActive(true);
        if (grito != null) grito.Play();
        if (destello != null) destello.enabled = true;

        // Se incorpora de golpe
        Quaternion acostado = torso != null ? torso.localRotation : Quaternion.identity;
        Quaternion sentado = acostado * Quaternion.Euler(angulo, 0f, 0f);

        float t = 0f;
        while (t < duracion)
        {
            t += Time.deltaTime;
            if (torso != null) torso.localRotation = Quaternion.Slerp(acostado, sentado, t / duracion);
            yield return null;
        }
        if (torso != null) torso.localRotation = sentado;

        yield return new WaitForSeconds(0.15f);
        if (destello != null) destello.enabled = false;

        // Y se queda sentado, mirando al jugador para el resto de la partida
    }
}
