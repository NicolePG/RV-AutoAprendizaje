using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// El final del juego: el patio de salida del colegio, detrás de la puerta de emergencia del
// Cuarto 4.
//
// Cuando el jugador sale al patio (la zona de la entrada del patio llama a Ganar()):
//  - se hace de día: la luz ambiental y la niebla pasan a las del patio (su ClimaCuarto);
//  - aparece el cartel "¡LOGRASTE SALIR DEL COLEGIO!" y el botón para volver a jugar;
//  - suena una fanfarria corta y los dos controles vibran.
// El botón "VOLVER A JUGAR" llama a VolverAJugar(): carga la escena otra vez, desde el Cuarto 1.
//
// En la escena: va en el patio. ConstructorCuarto4 lo arma y lo conecta.
public class PantallaVictoria : MonoBehaviour
{
    [Tooltip("El clima del patio (de día)")]
    public ClimaCuarto clima;

    [Tooltip("Lo que aparece al ganar: el cartel y el botón. Empiezan ocultos")]
    public GameObject[] mostrarAlGanar;

    public bool Gano { get; private set; }

    void Awake()
    {
        foreach (GameObject objeto in mostrarAlGanar)
            if (objeto != null) objeto.SetActive(false);
    }

    public void Ganar()
    {
        if (Gano) return;
        Gano = true;

        if (clima != null) clima.Aplicar();
        foreach (GameObject objeto in mostrarAlGanar)
            if (objeto != null) objeto.SetActive(true);

        foreach (var control in FindObjectsByType<XRBaseInputInteractor>())
            control.SendHapticImpulse(0.6f, 0.4f);
        StartCoroutine(Fanfarria());
    }

    // Cuatro notas que suben (do, mi, sol y do agudo), como un "¡lo lograste!"
    IEnumerator Fanfarria()
    {
        float[] notas = { 523f, 659f, 784f, 1047f };
        foreach (float hz in notas)
        {
            SonidoSintetico.Tocar(SonidoSintetico.Pitido(hz, 0.22f), transform.position, 0.7f);
            yield return new WaitForSeconds(0.16f);
        }
    }

    // Empieza de nuevo: la escena vuelve a cargarse como al darle Play
    public void VolverAJugar() => SceneManager.LoadScene(SceneManager.GetActiveScene().name);
}
