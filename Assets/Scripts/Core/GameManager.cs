using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// El que coordina la partida: en qué cuarto va el jugador, la pausa, el guardado automático,
// el tiempo agotado y la victoria. Los demás scripts le avisan a él y él decide qué pasa.
//
//  - Cuando se abre la puerta de salida de un cuarto, LlegarAlCuarto(n) anota que el jugador ya
//    va por el cuarto n y guarda la partida sola (punto de control), con aviso en el reloj de la esquina.
//  - Pausar/Reanudar: el reloj se detiene, el sonido del cuarto también, y las manos solo pueden
//    tocar los botones de los menús (capa de interacción "Menu"): no se puede agarrar nada ni
//    teletransportarse con el juego en pausa.
//    No se usa Time.timeScale = 0: el XR Interaction Toolkit suaviza el rayo del control con el
//    tiempo del juego, y con el tiempo en cero el rayo quedaría trabado y no se podría apuntar
//    a los botones del menú.
//  - Si el reloj llega a cero, bloquea las manos igual que la pausa y muestra la pantalla de
//    tiempo agotado.
//  - CargarPartida y Reiniciar vuelven a cargar la escena, así todos los acertijos quedan como
//    al principio. Si era para cargar, al arrancar la escena nueva lleva al jugador al principio
//    del cuarto guardado y le pone el tiempo que tenía.
//    Al recargar, además, se lleva la cabeza del jugador al punto de inicio: en el simulador,
//    caminar con WASD mueve el visor simulado (no el XR Origin) y ese corrimiento sobrevive a la
//    recarga; sin esto el jugador aparecería donde estaba antes (por ejemplo, en el patio).
//    Y se reinicia el simulador, que también sobrevive y se quedaba con los controles viejos.
//
// En la escena: va en el objeto "Sistema", junto a TimerController y SaveManager.
// ConstructorSistema lo arma y le conecta las puertas.
public class GameManager : MonoBehaviour
{
    public enum Estado { Jugando, Pausado, Ganado, TiempoAgotado }

    public static GameManager Instancia { get; private set; }

    public const int TOTAL_CUARTOS = 4;
    public static readonly string[] NombresCuartos = { "Recepción", "Dirección", "Sala de Computación", "Laboratorio" };

    // La capa de interacción de los botones de los menús. Con el juego en pausa, las manos
    // solo interactúan con esta capa.
    public const int CAPA_MENU = 30;
    public static InteractionLayerMask MascaraMenu => 1 << CAPA_MENU;

    [Header("Sistemas")]
    public TimerController reloj;
    public SaveManager guardado;
    public HUDReloj hud;
    public PantallaTiempoAgotado pantallaTiempoAgotado;

    [Tooltip("Dónde aparece el jugador al cargar una partida, uno por cuarto. El del Cuarto 1 puede " +
             "quedar vacío: es donde empieza el juego")]
    public Transform[] puntosDeInicio = new Transform[TOTAL_CUARTOS];

    public Estado EstadoActual { get; private set; } = Estado.Jugando;
    public int CuartoActual { get; private set; } = 1;
    public bool Gano => EstadoActual == Estado.Ganado || (EstadoActual == Estado.Pausado && estadoAntesDePausa == Estado.Ganado);
    public bool PuedePausar => EstadoActual == Estado.Jugando || EstadoActual == Estado.Ganado;

    public static string NombreCuarto(int cuarto) =>
        cuarto >= 1 && cuarto <= NombresCuartos.Length ? NombresCuartos[cuarto - 1] : "";

    // Sobreviven a la recarga de la escena: "al empezar, cargar la partida guardada" y "esta
    // escena se volvió a cargar desde el juego" (con Cargar, Reiniciar o Volver a jugar)
    static bool cargarAlEmpezar;
    static bool escenaRecargada;

    Estado estadoAntesDePausa;
    bool recargando;
    Pose inicioDelJuego;   // dónde está el jugador al abrir la escena: la entrada del Cuarto 1
    readonly Dictionary<XRBaseInteractor, InteractionLayerMask> capasOriginales =
        new Dictionary<XRBaseInteractor, InteractionLayerMask>();

    // En el editor, con "Enter Play Mode Options" las variables estáticas no se borran solas
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void LimpiarEstaticos()
    {
        cargarAlEmpezar = false;
        escenaRecargada = false;
        Instancia = null;
    }

    void Awake()
    {
        Instancia = this;
        // Por si la escena anterior quedó en pausa: el audio pausado es de todo el juego
        AudioListener.pause = false;

        var origen = FindAnyObjectByType<XROrigin>();
        if (origen != null) inicioDelJuego = new Pose(origen.transform.position, origen.transform.rotation);
    }

    void OnDestroy()
    {
        if (Instancia == this) Instancia = null;
    }

    void Start()
    {
        if (reloj != null)
        {
            reloj.alQuedarCincoMinutos.AddListener(() => Avisar("QUEDAN 5 MINUTOS", HUDReloj.Tono.Alerta, 0.4f));
            reloj.alUltimoMinuto.AddListener(() => Avisar("¡ÚLTIMO MINUTO!", HUDReloj.Tono.Peligro, 0.7f));
            reloj.alTerminar.AddListener(TerminarTiempo);
        }

        bool cargar = cargarAlEmpezar && guardado != null && guardado.HayPartida;
        bool recargada = escenaRecargada;
        cargarAlEmpezar = false;
        escenaRecargada = false;

        if (cargar) AplicarPartida(guardado.Ultima);
        else if (guardado != null && guardado.HayPartida) StartCoroutine(AvisarPartidaDisponible());

        // Al cargar hay que ir al cuarto guardado; al reiniciar, volver a la entrada del Cuarto 1
        if (cargar || recargada)
            StartCoroutine(AcomodarJugador(cargar ? PuntoDeInicio(CuartoActual) : inicioDelJuego, recargada));
    }

    // El Quest pierde el foco cuando el jugador abre el menú del sistema (botón de Meta): ahí se
    // pausa, así el reloj no sigue corriendo. En el editor no, porque pasaría cada vez que se hace
    // clic en otra ventana.
    void OnApplicationFocus(bool tieneFoco)
    {
#if !UNITY_EDITOR
        if (!tieneFoco && PuedePausar && PauseMenu.Instancia != null) PauseMenu.Instancia.Abrir();
#endif
    }

    // ------------------------------------------------------------------ cuartos y guardado

    // Lo llama la puerta de salida de cada cuarto al abrirse (evento alAbrirse): el jugador ya
    // pasa al cuarto "cuarto". La partida se guarda sola: si después carga, arranca ahí.
    public void LlegarAlCuarto(int cuarto)
    {
        if (EstadoActual == Estado.Ganado || EstadoActual == Estado.TiempoAgotado) return;
        if (cuarto <= CuartoActual) return;   // ya estaba anotado
        CuartoActual = Mathf.Clamp(cuarto, 1, TOTAL_CUARTOS);
        Guardar();
    }

    // Guarda el cuarto actual y el tiempo que queda. Lo usan el guardado automático y el menú.
    public bool Guardar()
    {
        if (guardado == null || reloj == null) return false;
        bool ok = guardado.Guardar(CuartoActual, reloj.TiempoRestante);
        if (ok) Avisar("PROGRESO GUARDADO", HUDReloj.Tono.Exito, 0.25f);
        else Avisar("NO SE PUDO GUARDAR", HUDReloj.Tono.Peligro, 0.5f);
        return ok;
    }

    void AplicarPartida(SaveData datos)
    {
        CuartoActual = Mathf.Clamp(datos.cuartoActual, 1, TOTAL_CUARTOS);
        guardado.Restaurar(datos);
        if (reloj != null) reloj.Fijar(datos.tiempoRestante);
        Avisar("PARTIDA CARGADA\n<size=70%>Cuarto " + CuartoActual + " · " + NombreCuarto(CuartoActual) + "</size>",
               HUDReloj.Tono.Exito, 0.3f);
    }

    // La entrada del cuarto (el Cuarto 1 no tiene punto: es donde empieza el juego)
    Pose PuntoDeInicio(int cuarto)
    {
        int i = cuarto - 1;
        if (puntosDeInicio == null || i < 0 || i >= puntosDeInicio.Length || puntosDeInicio[i] == null) return inicioDelJuego;
        return new Pose(puntosDeInicio[i].position, puntosDeInicio[i].rotation);
    }

    // Lleva al jugador al punto después de recargar. Se espera un cuadro: recién ahí el visor (o el
    // simulador) ubicó la cámara adentro del XR Origin. Y se hace antes del próximo paso de la
    // física, así ninguna zona llega a detectar al jugador donde estaba antes de recargar (por
    // ejemplo, la del patio, que daría la partida por ganada). Se repite al cuadro siguiente por
    // si el visor terminó de arrancar recién ahí.
    // El punto de cada cuarto queda adentro de la zona de su entrada, que aplica la luz y la niebla
    // del cuarto: el clima se acomoda solo.
    IEnumerator AcomodarJugador(Pose destino, bool reiniciarSimulador)
    {
        yield return null;
        if (reiniciarSimulador) ReiniciarSimulador();
        LlevarCabezaA(destino);
        yield return null;
        LlevarCabezaA(destino);
    }

    // Pone la cabeza del jugador justo encima del punto, mirando hacia donde mira el punto.
    // No alcanza con mover el XR Origin al punto: la cabeza puede estar corrida adentro de él.
    static void LlevarCabezaA(Pose destino)
    {
        var origen = FindAnyObjectByType<XROrigin>();
        if (origen == null || origen.Camera == null) return;
        Transform cabeza = origen.Camera.transform;

        origen.transform.SetPositionAndRotation(destino.position, destino.rotation);
        origen.MatchOriginUpCameraForward(Vector3.up, destino.forward);   // gira alrededor de la cabeza
        Vector3 corrimiento = destino.position - cabeza.position;
        corrimiento.y = 0f;                                               // la altura de los ojos no se toca
        origen.transform.position += corrimiento;
        Physics.SyncTransforms();
    }

    // El simulador de XR (solo en el PC) no se borra al recargar la escena y se queda con los
    // controles del XR Origin anterior, que ya no existen: apagarlo y prenderlo hace que los
    // vuelva a buscar. En el Quest no hay simulador y no hace nada.
    static void ReiniciarSimulador()
    {
        var simulador = XRInteractionSimulator.instance;
        if (simulador == null || !simulador.isActiveAndEnabled) return;
        simulador.enabled = false;
        simulador.enabled = true;
    }

    IEnumerator AvisarPartidaDisponible()
    {
        yield return new WaitForSecondsRealtime(2.5f);
        if (EstadoActual == Estado.Jugando)
            Avisar("HAY UNA PARTIDA GUARDADA\n<size=70%>Abrí el menú para cargarla</size>", HUDReloj.Tono.Info, 0.2f);
    }

    // ------------------------------------------------------------------ pausa

    public void Pausar()
    {
        if (!PuedePausar) return;
        estadoAntesDePausa = EstadoActual;
        EstadoActual = Estado.Pausado;
        Congelar(true);
    }

    public void Reanudar()
    {
        if (EstadoActual != Estado.Pausado) return;
        EstadoActual = estadoAntesDePausa;
        Congelar(false);
    }

    // Congela o descongela la partida: el reloj, el sonido del cuarto (los menús usan un
    // AudioSource que no se pausa) y las manos. Las manos (rayo, mano cercana, dedo y
    // teletransporte) pasan a la capa de los menús y después vuelven a la suya. Los encajes
    // (sockets) no se tocan: si no, soltarían la llave o los fusibles que tienen puestos.
    void Congelar(bool congelar)
    {
        if (reloj != null) reloj.Pausado = congelar;
        AudioListener.pause = congelar;

        if (congelar)
        {
            // También las apagadas: el rayo de teletransporte se prende recién al mover el joystick
            foreach (var mano in FindObjectsByType<XRBaseInteractor>(FindObjectsInactive.Include))
            {
                if (mano is XRSocketInteractor || capasOriginales.ContainsKey(mano)) continue;
                capasOriginales[mano] = mano.interactionLayers;
                mano.interactionLayers = MascaraMenu;
            }
        }
        else
        {
            foreach (var par in capasOriginales)
                if (par.Key != null) par.Key.interactionLayers = par.Value;
            capasOriginales.Clear();
        }
    }

    // ------------------------------------------------------------------ final de la partida

    void TerminarTiempo()
    {
        if (EstadoActual == Estado.Ganado || EstadoActual == Estado.TiempoAgotado) return;
        EstadoActual = Estado.TiempoAgotado;
        Congelar(true);
        Vibrar(1f, 0.8f);
        if (pantallaTiempoAgotado != null) pantallaTiempoAgotado.Mostrar();
    }

    // Lo llama la salida de emergencia del Cuarto 4 al abrirse: el reloj se detiene ahí mismo,
    // así no se le acaba el tiempo mientras camina hacia el patio
    public void SalidaAbierta()
    {
        if (reloj != null) reloj.Detener();
    }

    // Lo llama el patio de salida (PantallaVictoria) cuando el jugador sale del colegio. Puede
    // pasar con el menú abierto (en el Quest se puede caminar en pausa): ahí queda anotado para
    // cuando lo cierre.
    public void Ganar()
    {
        if (Gano || EstadoActual == Estado.TiempoAgotado) return;
        if (reloj != null) reloj.Detener();
        if (EstadoActual == Estado.Pausado) estadoAntesDePausa = Estado.Ganado;
        else EstadoActual = Estado.Ganado;
        Avisar("¡ESCAPASTE!", HUDReloj.Tono.Exito, 0.6f);
    }

    public void CargarPartida()
    {
        if (guardado == null || !guardado.HayPartida) return;
        cargarAlEmpezar = true;
        RecargarEscena();
    }

    public void Reiniciar()
    {
        cargarAlEmpezar = false;
        RecargarEscena();
    }

    void RecargarEscena()
    {
        if (recargando) return;   // por si tocan el botón dos veces
        recargando = true;
        escenaRecargada = true;
        Congelar(false);

        Scene escena = SceneManager.GetActiveScene();
#if UNITY_EDITOR
        // En el editor se puede recargar aunque la escena no esté en la lista del build
        if (escena.buildIndex < 0)
        {
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
                escena.path, new LoadSceneParameters(LoadSceneMode.Single));
            return;
        }
#endif
        SceneManager.LoadScene(escena.buildIndex);
    }

    // ------------------------------------------------------------------ avisos

    void Avisar(string texto, HUDReloj.Tono tono, float vibracion)
    {
        if (hud != null) hud.Avisar(texto, tono);
        if (vibracion > 0f) Vibrar(vibracion, 0.15f);
    }

    // Hace vibrar los dos controles
    public static void Vibrar(float fuerza, float segundos)
    {
        foreach (var mano in FindObjectsByType<XRBaseInputInteractor>())
            mano.SendHapticImpulse(fuerza, segundos);
    }
}
