using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// El que coordina la partida: el menú de inicio, en qué cuarto va el jugador, la pausa, el
// guardado automático, el tiempo agotado y la victoria. Los demás scripts le avisan a él y él
// decide qué pasa.
//
//  - Al abrir el juego aparece el menú de inicio (MainMenu) sobre el Cuarto 1 a oscuras: el reloj
//    no corre y las manos solo tocan los botones del menú, como en la pausa. EmpezarPartida() lo
//    cierra y arranca el reloj. "Menú principal" (pausa, final y tiempo agotado) vuelve a cargar
//    la escena y lo muestra otra vez (IrAlMenu). Al cargar una partida o al volver a jugar no
//    aparece: se juega directo.
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
//    del cuarto guardado y le pone el tiempo que tenía. Si era para reiniciar (también "Volver a
//    jugar" al ganar), lo lleva SIEMPRE a la entrada del Cuarto 1, a su punto de inicio fijo: no
//    a donde estaba el jugador al abrir la escena, porque si se probó con "Llevar jugador al
//    Cuarto 4" la escena arranca ahí y volver a jugar lo dejaba otra vez en el Cuarto 4.
//    Al recargar, además, se lleva la cabeza del jugador al punto de inicio: en el simulador,
//    caminar con WASD mueve el visor simulado (no el XR Origin) y ese corrimiento sobrevive a la
//    recarga; sin esto el jugador aparecería donde estaba antes (por ejemplo, en el patio).
//    Y se reinicia el simulador, que también sobrevive y se quedaba con los controles viejos.
//
// En la escena: va en el objeto "Sistema", junto a TimerController y SaveManager.
// ConstructorSistema lo arma y le conecta las puertas.
public class GameManager : MonoBehaviour
{
    public enum Estado { EnMenu, Jugando, Pausado, Ganado, TiempoAgotado }

    public static GameManager Instancia { get; private set; }

    public const int TOTAL_CUARTOS = 4;

    // Solo por si falta algún asset RoomData: los nombres de verdad salen de "cuartos"
    static readonly string[] NombresPorDefecto = { "Recepción", "Dirección", "Sala de Computación", "Laboratorio" };

    // La capa de interacción de los botones de los menús. Con el juego en pausa, las manos
    // solo interactúan con esta capa.
    public const int CAPA_MENU = 30;
    public static InteractionLayerMask MascaraMenu => 1 << CAPA_MENU;

    [Header("Sistemas")]
    public TimerController reloj;
    public SaveManager guardado;
    public HUDReloj hud;
    public PantallaTiempoAgotado pantallaTiempoAgotado;
    public MainMenu menuInicio;

    [Tooltip("Los datos de cada cuarto (assets RoomData), en el orden del recorrido: nombre, acertijos " +
             "y tiempo sugerido. De acá los sacan el reloj y los menús")]
    public RoomData[] cuartos = new RoomData[TOTAL_CUARTOS];

    [Tooltip("Dónde aparece el jugador al cargar una partida, uno por cuarto. El del Cuarto 1 es " +
             "donde empieza el juego: ahí llevan Reiniciar y Volver a jugar")]
    public Transform[] puntosDeInicio = new Transform[TOTAL_CUARTOS];

    public Estado EstadoActual { get; private set; } = Estado.Jugando;
    public int CuartoActual { get; private set; } = 1;
    public bool Gano => EstadoActual == Estado.Ganado || (EstadoActual == Estado.Pausado && estadoAntesDePausa == Estado.Ganado);
    public bool PuedePausar => EstadoActual == Estado.Jugando || EstadoActual == Estado.Ganado;

    // Los datos (RoomData) del cuarto número "cuarto" (1 a 4), o null si no están
    public static RoomData DatosCuarto(int cuarto)
    {
        var juego = Instancia;
        if (juego == null || juego.cuartos == null || cuarto < 1 || cuarto > juego.cuartos.Length) return null;
        return juego.cuartos[cuarto - 1];
    }

    public static string NombreCuarto(int cuarto)
    {
        RoomData datos = DatosCuarto(cuarto);
        if (datos != null && !string.IsNullOrEmpty(datos.nombre)) return datos.nombre;
        return cuarto >= 1 && cuarto <= NombresPorDefecto.Length ? NombresPorDefecto[cuarto - 1] : "";
    }

    // Sobreviven a la recarga de la escena: "al empezar, cargar la partida guardada", "esta
    // escena se volvió a cargar desde el juego" (con Cargar, Reiniciar o Volver a jugar) y "al
    // empezar, mostrar el menú de inicio" (con Menú principal)
    static bool cargarAlEmpezar;
    static bool escenaRecargada;
    static bool irAlMenu;

    Estado estadoAntesDePausa;
    bool recargando;
    Pose inicioDelJuego;   // dónde está el jugador al abrir la escena (por si falta el punto del Cuarto 1)
    readonly Dictionary<XRBaseInteractor, InteractionLayerMask> capasOriginales =
        new Dictionary<XRBaseInteractor, InteractionLayerMask>();

    // En el editor, con "Enter Play Mode Options" las variables estáticas no se borran solas
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void LimpiarEstaticos()
    {
        cargarAlEmpezar = false;
        escenaRecargada = false;
        irAlMenu = false;
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
        bool menu = !cargar && (irAlMenu || (!recargada && ArrancaEnElCuarto1()));
        cargarAlEmpezar = false;
        escenaRecargada = false;
        irAlMenu = false;

        if (cargar) AplicarPartida(guardado.Ultima);
        else if (!menu && guardado != null && guardado.HayPartida) StartCoroutine(AvisarPartidaDisponible());

        // Al cargar hay que ir al cuarto guardado; al reiniciar, a la entrada del Cuarto 1
        if (cargar || recargada)
            StartCoroutine(AcomodarJugador(PuntoDeInicio(cargar ? CuartoActual : 1), recargada));
#if !UNITY_EDITOR
        // En el Quest la partida empieza siempre en el Cuarto 1, aunque la escena se haya guardado
        // con el jugador en otro cuarto. En el editor no: ahí se respeta "Llevar jugador al Cuarto N"
        else
            StartCoroutine(AcomodarJugador(PuntoDeInicio(1), false));
#endif

        if (menu) AbrirMenuInicio();
    }

    // true si el jugador está en la entrada del Cuarto 1 al abrir la escena. En el editor, si se lo
    // llevó a otro cuarto para probarlo ("Llevar jugador al Cuarto N"), no aparece el menú de
    // inicio: se juega directo ahí. En el Quest siempre empieza en el Cuarto 1.
    bool ArrancaEnElCuarto1()
    {
#if UNITY_EDITOR
        Vector3 distancia = inicioDelJuego.position - PuntoDeInicio(1).position;
        distancia.y = 0f;
        return distancia.magnitude < 1.5f;
#else
        return true;
#endif
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

    // La entrada del cuarto. Si falta el punto (una escena armada antes de que existiera el del
    // Cuarto 1), se usa donde estaba el jugador al abrir la escena
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

    // ------------------------------------------------------------------ menú de inicio

    // El juego queda quieto (reloj, sonido del cuarto y manos, como en la pausa) y aparece el menú
    void AbrirMenuInicio()
    {
        EstadoActual = Estado.EnMenu;
        Congelar(true);
        if (menuInicio != null) menuInicio.Abrir();
        else EmpezarPartida();   // una escena sin menú: se juega directo
    }

    // NUEVA PARTIDA en el menú de inicio: se cierra el menú y arranca el reloj de 15 minutos
    public void EmpezarPartida()
    {
        if (EstadoActual != Estado.EnMenu) return;
        EstadoActual = Estado.Jugando;
        Congelar(false);
    }

    // MENÚ PRINCIPAL en la pausa, en el tótem del final o en el tiempo agotado: la escena vuelve a
    // cargarse (todo queda como al principio) y aparece el menú de inicio en el Cuarto 1
    public void IrAlMenu()
    {
        cargarAlEmpezar = false;
        irAlMenu = true;
        RecargarEscena();
    }

    // SALIR en el menú de inicio: en el Quest cierra la aplicación; en el editor detiene el Play
    public static void Salir()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
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
        // Si el jugador activó caminar con el joystick, con un menú abierto no camina
        if (ModoDeMovimiento.Instancia != null) ModoDeMovimiento.Instancia.Bloquear(congelar);

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
