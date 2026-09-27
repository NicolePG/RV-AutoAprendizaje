using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// El final del juego: el pasillo de salida del colegio, detrás de la puerta de emergencia del
// Cuarto 4, con la puerta principal de vidrio al fondo.
//
// Cuando el jugador entra al pasillo (la zona apenas pasando la puerta llama a Ganar()):
//  - se pone el clima del pasillo: de día, con la luz que entra por la puerta principal;
//  - se prende la pantalla colgada del techo con "¡GANASTE!" y el tiempo que tardó, y el tótem
//    con el botón para volver a jugar;
//  - suena una fanfarria corta y los dos controles vibran;
//  - avisa "alGanar": las luces del techo se prenden en cascada hacia la salida y, al terminar,
//    se abre sola la puerta principal (PuertaCorrediza);
//  - GameManager da la partida por ganada (el reloj de la esquina se pone verde).
// El botón verde "VOLVER A JUGAR" llama a VolverAJugar(): carga la escena otra vez, siempre desde
// el Cuarto 1. El azul "MENÚ PRINCIPAL" llama a IrAlMenu(): vuelve al menú de inicio.
//
// En la escena: va en el pasillo de salida. ConstructorCuarto4 lo arma y lo conecta
// (ConstructorCuarto4Salida.cs).
public class PantallaVictoria : MonoBehaviour
{
    [Tooltip("El clima del pasillo (de día)")]
    public ClimaCuarto clima;

    [Tooltip("Lo que aparece al ganar: los textos de las pantallas y el botón. Empiezan ocultos")]
    public GameObject[] mostrarAlGanar;

    [Tooltip("La línea de la pantalla con el tiempo que tardó")]
    public TMP_Text textoTiempo;

    [Tooltip("Qué más pasa al ganar (por ejemplo, prender las luces del pasillo en cascada)")]
    public UnityEvent alGanar = new UnityEvent();

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

        var juego = GameManager.Instancia;
        if (juego != null)
        {
            juego.Ganar();
            if (textoTiempo != null && juego.reloj != null)
                textoTiempo.text = "Tu tiempo: <b>" + TimerController.Formato(juego.reloj.TiempoUsado) + "</b>" +
                                   "     ·     Te sobraron " + TimerController.Formato(juego.reloj.TiempoRestante);
        }

        if (clima != null) clima.Aplicar();
        foreach (GameObject objeto in mostrarAlGanar)
            if (objeto != null) objeto.SetActive(true);

        foreach (var control in FindObjectsByType<XRBaseInputInteractor>())
            control.SendHapticImpulse(0.6f, 0.4f);
        StartCoroutine(Fanfarria());
        alGanar.Invoke();
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

    // Empieza de nuevo: la escena vuelve a cargarse y el jugador aparece en la entrada del Cuarto 1
    public void VolverAJugar()
    {
        if (GameManager.Instancia != null) GameManager.Instancia.Reiniciar();
        else SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // El botón azul del tótem: vuelve al menú de inicio
    public void IrAlMenu()
    {
        if (GameManager.Instancia != null) GameManager.Instancia.IrAlMenu();
        else SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
