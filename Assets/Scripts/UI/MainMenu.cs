using System.Collections;
using TMPro;
using UnityEngine;

// El menú de inicio del juego. Aparece al abrir el juego, flotando delante del jugador en la
// recepción a oscuras (Cuarto 1): un panel de emergencia de acero, con franjas de peligro, el
// título, la premisa y la última partida guardada (los efectos están en EfectosMenuInicio).
// Lo abre GameManager: mientras está abierto el reloj no corre y las manos solo tocan los botones.
//
// Tiene tres páginas en el mismo panel:
//  - PRINCIPAL:
//     · NUEVA PARTIDA: cierra el menú y empieza el juego en el Cuarto 1, con 15 minutos.
//     · CONTINUAR: carga la última partida guardada (apagado si no hay ninguna).
//     · OPCIONES: abre la página de opciones.
//     · CÓMO JUGAR: abre la página con los controles.
//     · SALIR: cierra el juego. Pide confirmación: hay que tocarlo otra vez antes de 3 segundos.
//  - OPCIONES: el volumen general, con "-" y "+" (de a 10 %), y "Caminar con joystick" (sí/no,
//    ver ModoDeMovimiento). Los dos se guardan en el visor (PlayerPrefs) y se aplican cada vez que
//    arranca el juego.
//  - CÓMO JUGAR: los controles del visor y del PC. No da pistas de los acertijos: el GDD dice
//    que las pistas están en los cuartos.
//
// En la escena: va en "Sistema/Menu_Inicio", junto a su PanelFlotante. ConstructorSistema lo arma.
public class MainMenu : MonoBehaviour
{
    public static MainMenu Instancia { get; private set; }

    const string CLAVE_VOLUMEN = "volumen";
    const float SEGUNDOS_CONFIRMAR = 3f;

    public PanelFlotante panel;

    [Header("Páginas")]
    public GameObject paginaPrincipal;
    public GameObject paginaOpciones;
    public GameObject paginaControles;

    [Header("Página principal")]
    public BotonMenu botonNuevaPartida;
    public BotonMenu botonContinuar;
    public BotonMenu botonOpciones;
    public BotonMenu botonControles;
    public BotonMenu botonSalir;
    [Tooltip("La tarjeta de abajo: qué partida hay guardada")]
    public TMP_Text textoPartida;

    [Header("Opciones")]
    public BotonMenu botonMenosVolumen;
    public BotonMenu botonMasVolumen;
    [Tooltip("Las rayitas de la barra de volumen, de izquierda a derecha")]
    public Renderer[] barraVolumen;
    public TMP_Text textoVolumen;
    public Color colorBarraLlena = new Color(0.86f, 0.1f, 0.12f);
    public Color colorBarraVacia = new Color(0.16f, 0.13f, 0.14f);
    [Tooltip("Prende y apaga caminar con el joystick izquierdo (ModoDeMovimiento)")]
    public BotonMenu botonCaminar;
    public BotonMenu botonVolverOpciones;

    [Header("Cómo jugar")]
    public BotonMenu botonVolverControles;

    [Tooltip("Sonido de los menús: un AudioSource 2D que no se pausa con el juego")]
    public AudioSource audioMenu;

    MaterialPropertyBlock bloque;
    float confirmarSalirHasta = -1f;

    void Awake()
    {
        Instancia = this;
        bloque = new MaterialPropertyBlock();
        AplicarVolumenGuardado();

        botonNuevaPartida.alPresionar.AddListener(NuevaPartida);
        botonContinuar.alPresionar.AddListener(() => GameManager.Instancia.CargarPartida());
        botonOpciones.alPresionar.AddListener(() => MostrarPagina(paginaOpciones));
        botonControles.alPresionar.AddListener(() => MostrarPagina(paginaControles));
        botonSalir.alPresionar.AddListener(Salir);
        botonMenosVolumen.alPresionar.AddListener(() => CambiarVolumen(-0.1f));
        botonMasVolumen.alPresionar.AddListener(() => CambiarVolumen(0.1f));
        botonCaminar.alPresionar.AddListener(AlternarCaminar);
        botonVolverOpciones.alPresionar.AddListener(() => MostrarPagina(paginaPrincipal));
        botonVolverControles.alPresionar.AddListener(() => MostrarPagina(paginaPrincipal));
    }

    void OnDestroy()
    {
        if (Instancia == this) Instancia = null;
    }

    // El volumen que eligió el jugador la última vez (100 % si nunca lo cambió)
    public static void AplicarVolumenGuardado() => AudioListener.volume = PlayerPrefs.GetFloat(CLAVE_VOLUMEN, 1f);

    // Lo llama GameManager al empezar. Espera dos cuadros: recién ahí el visor ubicó la cabeza, y
    // el panel aparece de frente al jugador y no donde estaba la cámara al cargar la escena.
    public void Abrir() => StartCoroutine(AbrirCuandoHayaVista());

    IEnumerator AbrirCuandoHayaVista()
    {
        yield return null;
        yield return null;
        MostrarPagina(paginaPrincipal);
        panel.Mostrar();
        Sonar(SonidoSintetico.Subida(520f, 880f, 0.12f), 0.45f);
    }

    void NuevaPartida()
    {
        panel.Ocultar();
        Sonar(SonidoSintetico.Golpe(), 0.8f);   // el golpe del apagón: empieza el juego
        GameManager.Instancia.EmpezarPartida();
    }

    void Salir()
    {
        if (Time.unscaledTime < confirmarSalirHasta)
        {
            GameManager.Salir();
            return;
        }
        confirmarSalirHasta = Time.unscaledTime + SEGUNDOS_CONFIRMAR;
        botonSalir.Textos("¿SEGURO? TOCÁ DE NUEVO", "Se cierra el juego");
        botonSalir.Colorear(HUDReloj.Rojo);
    }

    void Update()
    {
        // Pasaron los 3 segundos sin confirmar: el botón SALIR vuelve a ser el de siempre
        if (confirmarSalirHasta > 0f && Time.unscaledTime >= confirmarSalirHasta) CancelarSalir();
    }

    void CancelarSalir()
    {
        confirmarSalirHasta = -1f;
        botonSalir.Textos(null, null);
        botonSalir.Colorear(null);
    }

    void MostrarPagina(GameObject pagina)
    {
        CancelarSalir();
        paginaPrincipal.SetActive(pagina == paginaPrincipal);
        paginaOpciones.SetActive(pagina == paginaOpciones);
        paginaControles.SetActive(pagina == paginaControles);
        Refrescar();
    }

    // Pone al día la partida guardada y el volumen
    void Refrescar()
    {
        var guardado = SaveManager.Instancia;
        bool hay = guardado != null && guardado.HayPartida;
        botonContinuar.Habilitar(hay);
        botonContinuar.Textos(null, hay ? SaveManager.Resumen(guardado.Ultima) + " restantes" : "No hay partida guardada");
        textoPartida.text = hay
            ? "Cuarto " + guardado.Ultima.cuartoActual + " · " + GameManager.NombreCuarto(guardado.Ultima.cuartoActual) +
              "  ·  " + TimerController.Formato(guardado.Ultima.tiempoRestante) + " restantes\n<size=80%>Guardada el " +
              guardado.Ultima.fecha + "</size>"
            : "Todavía no hay una partida guardada\n<size=80%>Se guarda sola cada vez que se abre una puerta</size>";

        int pasos = Mathf.RoundToInt(AudioListener.volume * 10f);
        textoVolumen.text = (pasos * 10) + "%";
        for (int i = 0; i < barraVolumen.Length; i++)
        {
            barraVolumen[i].GetPropertyBlock(bloque);
            bloque.SetColor("_BaseColor", i < pasos ? colorBarraLlena : colorBarraVacia);
            barraVolumen[i].SetPropertyBlock(bloque);
        }
        botonMenosVolumen.Habilitar(pasos > 0);
        botonMasVolumen.Habilitar(pasos < 10);

        bool caminar = ModoDeMovimiento.CaminarConJoystick;
        botonCaminar.Habilitar(ModoDeMovimiento.Instancia != null);
        botonCaminar.Textos(caminar ? "CAMINAR CON JOYSTICK:  SÍ" : "CAMINAR CON JOYSTICK:  NO",
                            caminar ? "El izquierdo camina (los bordes se oscurecen). El derecho teletransporta"
                                    : "Solo teletransporte (no marea). Tocá para caminar con el izquierdo");
    }

    void AlternarCaminar()
    {
        if (ModoDeMovimiento.Instancia != null) ModoDeMovimiento.Instancia.Alternar();
        Refrescar();
    }

    void CambiarVolumen(float cambio)
    {
        float volumen = Mathf.Clamp01(Mathf.Round((AudioListener.volume + cambio) * 10f) / 10f);
        AudioListener.volume = volumen;
        PlayerPrefs.SetFloat(CLAVE_VOLUMEN, volumen);
        PlayerPrefs.Save();
        Refrescar();
    }

    void Sonar(AudioClip clip, float volumen)
    {
        if (audioMenu == null) return;
        audioMenu.ignoreListenerPause = true;
        audioMenu.PlayOneShot(clip, volumen);
    }
}
