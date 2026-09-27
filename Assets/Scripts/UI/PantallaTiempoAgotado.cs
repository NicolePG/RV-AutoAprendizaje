using System.Collections;
using TMPro;
using UnityEngine;

// La pantalla de "TIEMPO AGOTADO": la muestra GameManager cuando el reloj llega a cero.
// El cuarto se oscurece, las manos ya no pueden tocar nada del cuarto y solo se puede elegir:
//  - CARGAR ÚLTIMA PARTIDA: vuelve al principio del cuarto guardado, con el tiempo que tenía.
//    Si no hay partida guardada queda apagado.
//  - EMPEZAR DE NUEVO: desde el Cuarto 1, con los 15 minutos.
// Dice también hasta dónde llegó el jugador (cuarto y acertijos resueltos).
//
// En la escena: va en "Sistema/Pantalla_Tiempo_Agotado", junto a su PanelFlotante.
// ConstructorSistema lo arma.
public class PantallaTiempoAgotado : MonoBehaviour
{
    public PanelFlotante panel;
    public TMP_Text textoProgreso;
    public BotonMenu botonCargar;
    public BotonMenu botonReiniciar;

    [Tooltip("Sonido de los menús: un AudioSource 2D que no se pausa con el juego")]
    public AudioSource audioMenu;

    void Awake()
    {
        botonCargar.alPresionar.AddListener(() => GameManager.Instancia.CargarPartida());
        botonReiniciar.alPresionar.AddListener(() => GameManager.Instancia.Reiniciar());
        if (audioMenu != null) audioMenu.ignoreListenerPause = true;
    }

    public void Mostrar()
    {
        var juego = GameManager.Instancia;
        var guardado = SaveManager.Instancia;
        int cuarto = juego != null ? juego.CuartoActual : 1;

        string progreso = "Llegaste al <b>Cuarto " + cuarto + " de " + GameManager.TOTAL_CUARTOS + "</b> (" + GameManager.NombreCuarto(cuarto) + ")";
        if (guardado != null)
            progreso += "  ·  " + guardado.AcertijosResueltos + " de " + guardado.TotalAcertijos + " acertijos resueltos";
        textoProgreso.text = progreso;

        bool hay = guardado != null && guardado.HayPartida;
        botonCargar.Habilitar(hay);
        botonCargar.Textos(null, hay ? SaveManager.Resumen(guardado.Ultima) + " restantes" : "No hay partida guardada");

        panel.Mostrar();
        StartCoroutine(Sonido());
    }

    // Tres notas que bajan: "se terminó"
    IEnumerator Sonido()
    {
        if (audioMenu == null) yield break;
        float[] notas = { 392f, 311f, 233f };
        foreach (float hz in notas)
        {
            audioMenu.PlayOneShot(SonidoSintetico.Pitido(hz, 0.32f), 0.7f);
            yield return new WaitForSecondsRealtime(0.28f);
        }
    }
}
