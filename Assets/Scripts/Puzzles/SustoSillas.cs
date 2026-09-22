using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// El susto del Cuarto 3 (GDD, sección 6), en su versión segura: nunca salta por chocar con
// el cuerpo, porque en una feria el jugador podría tropezar de verdad.
//
// Salta la primera vez que el jugador toca una de las tres sillas (S1, S2 o S3) con la mano, o
// se teletransporta al lado de una (una zona invisible alrededor de cada silla). Entonces:
//  - la silla se arrastra sola, alejándose del jugador, con un chirrido metálico;
//  - una computadora rota (C1) se prende un instante mostrando una cara;
//  - los controles vibran fuerte.
// Pasa una sola vez por partida.
//
// En la escena: va en el grupo de las sillas. Cada silla tiene un XR Simple Interactable (para
// la mano) y una zona con DisparadorJugador que llama a DispararPorZona() con su número.
// ConstructorCuarto3 lo arma solo.
public class SustoSillas : MonoBehaviour
{
    [Tooltip("Las sillas del susto, en orden (S1, S2, S3)")]
    public Transform[] sillas;

    [Tooltip("La computadora que muestra la cara")]
    public ComputadoraSala pantalla;

    [Tooltip("Qué tan cerca tiene que estar la mano para que cuente como tocar la silla (metros)")]
    public float alcanceMano = 0.9f;

    public float distanciaArrastre = 0.6f;
    public float giroArrastre = 35f;
    public float duracionArrastre = 0.45f;

    bool usado;

    void Awake()
    {
        for (int i = 0; i < sillas.Length; i++)
        {
            var interactable = sillas[i].GetComponent<XRSimpleInteractable>();
            if (interactable == null) continue;
            int numero = i;
            interactable.hoverEntered.AddListener(args => TocadaConLaMano(numero, args.interactorObject));
        }
    }

    // La mano (o el rayo) pasa por encima: solo cuenta si la mano está cerca de la silla
    void TocadaConLaMano(int silla, IXRInteractor mano)
    {
        if (mano == null || Vector3.Distance(mano.transform.position, sillas[silla].position) > alcanceMano) return;
        Disparar(silla, mano as XRBaseInputInteractor);
    }

    // Lo llama la zona de cada silla cuando el jugador llega al lado (caminando o teletransportándose)
    public void DispararPorZona(int silla) => Disparar(silla, null);

    void Disparar(int silla, XRBaseInputInteractor mano)
    {
        if (usado || silla < 0 || silla >= sillas.Length) return;
        usado = true;

        StartCoroutine(Arrastrar(sillas[silla]));
        if (pantalla != null) pantalla.MostrarCara(0.9f);

        // Vibración fuerte: en la mano que la tocó, o en las dos si fue por la zona
        if (mano != null) mano.SendHapticImpulse(1f, 0.45f);
        else
            foreach (var control in FindObjectsByType<XRBaseInputInteractor>())
                control.SendHapticImpulse(0.9f, 0.45f);
    }

    IEnumerator Arrastrar(Transform silla)
    {
        SonidoSintetico.Tocar(SonidoSintetico.Chirrido(0.6f), silla.position);

        // Se aleja del jugador, en horizontal
        Vector3 desde = silla.position;
        Vector3 direccion = Camera.main != null ? desde - Camera.main.transform.position : silla.forward;
        direccion.y = 0f;
        if (direccion.sqrMagnitude < 0.001f) direccion = silla.forward;
        Vector3 hasta = desde + direccion.normalized * distanciaArrastre;
        Quaternion giroDesde = silla.rotation;
        Quaternion giroHasta = giroDesde * Quaternion.Euler(0f, giroArrastre, 0f);

        for (float t = 0f; t < duracionArrastre; t += Time.deltaTime)
        {
            float avance = 1f - Mathf.Pow(1f - t / duracionArrastre, 2f);   // arranca de golpe y frena
            silla.SetPositionAndRotation(Vector3.Lerp(desde, hasta, avance), Quaternion.Slerp(giroDesde, giroHasta, avance));
            yield return null;
        }
        silla.SetPositionAndRotation(hasta, giroHasta);
    }
}
