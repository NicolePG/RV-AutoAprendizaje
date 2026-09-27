using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// El menú de pausa. Se abre y se cierra con el botón de menú del control izquierdo (en el Quest,
// el de las tres rayitas; en el simulador, la tecla M) o con Esc en el teclado.
// Mientras está abierto el juego queda en pausa (GameManager.Pausar): el reloj no corre, el sonido
// del cuarto se detiene y con las manos solo se pueden tocar los botones del menú.
//
// A la izquierda muestra la partida: el tiempo que queda, el cuarto (con una ficha por cuarto:
// verde los pasados, ámbar el actual) y cuántos acertijos lleva. A la derecha, los botones:
//  - REANUDAR: cierra el menú y sigue el juego.
//  - GUARDAR PARTIDA: guarda ahora el cuarto actual y el tiempo que queda.
//  - CARGAR PARTIDA: vuelve a la partida guardada (al principio de ese cuarto).
//  - REINICIAR JUEGO: empieza de cero desde el Cuarto 1.
//  - MENÚ PRINCIPAL: vuelve al menú de inicio (la partida se deja; lo guardado sigue guardado).
// Cargar, reiniciar y menú principal piden confirmación (se pierde lo que no se guardó): el botón se pone rojo y
// pregunta "¿SEGURO?"; hay que tocarlo otra vez antes de 3 segundos.
//
// En la escena: va en "Sistema/Menu_Pausa", junto a su PanelFlotante. ConstructorSistema lo arma.
public class PauseMenu : MonoBehaviour
{
    public static PauseMenu Instancia { get; private set; }

    public PanelFlotante panel;

    [Header("La partida")]
    public TMP_Text textoTiempo;
    public TMP_Text textoCuarto;
    public TMP_Text textoNombreCuarto;
    public TMP_Text textoAcertijos;
    [Tooltip("Una ficha por cuarto, en orden")]
    public Renderer[] fichasCuartos;
    [Tooltip("La línea de abajo: qué partida hay guardada o qué pasó al tocar un botón")]
    public TMP_Text textoEstado;

    [Header("Botones")]
    public BotonMenu botonReanudar;
    public BotonMenu botonGuardar;
    public BotonMenu botonCargar;
    public BotonMenu botonReiniciar;
    public BotonMenu botonMenuPrincipal;

    [Tooltip("Sonido de los menús: un AudioSource 2D que no se pausa con el juego")]
    public AudioSource audioMenu;

    const float SEGUNDOS_CONFIRMAR = 3f;
    static readonly Color Gris = new Color(0.23f, 0.28f, 0.36f);
    static readonly Color Apagado = new Color(0.58f, 0.64f, 0.72f);

    InputAction accion;
    MaterialPropertyBlock bloque;
    BotonMenu esperandoConfirmacion;
    float confirmarHasta;

    void Awake()
    {
        Instancia = this;
        bloque = new MaterialPropertyBlock();

        accion = new InputAction("Pausa", InputActionType.Button);
        accion.AddBinding("<XRController>{LeftHand}/{MenuButton}");
        accion.AddBinding("<XRController>{RightHand}/{MenuButton}");   // en el Quest no existe: sirve para el simulador
        accion.AddBinding("<Keyboard>/escape");
        accion.performed += _ => Alternar();

        botonReanudar.alPresionar.AddListener(Cerrar);
        botonGuardar.alPresionar.AddListener(Guardar);
        botonCargar.alPresionar.AddListener(() =>
            Confirmar(botonCargar, "Vas a volver al principio del cuarto guardado", () => GameManager.Instancia.CargarPartida()));
        botonReiniciar.alPresionar.AddListener(() =>
            Confirmar(botonReiniciar, "Se pierde todo lo que no guardaste", () => GameManager.Instancia.Reiniciar()));
        botonMenuPrincipal.alPresionar.AddListener(() =>
            Confirmar(botonMenuPrincipal, "Se pierde lo que no guardaste", () => GameManager.Instancia.IrAlMenu()));
    }

    void OnEnable() => accion.Enable();
    void OnDisable() => accion.Disable();

    void OnDestroy()
    {
        accion.Dispose();
        if (Instancia == this) Instancia = null;
    }

    void Alternar()
    {
        if (panel.Visible) Cerrar();
        else Abrir();
    }

    public void Abrir()
    {
        var juego = GameManager.Instancia;
        if (juego == null || panel.Visible || !juego.PuedePausar) return;
        juego.Pausar();
        CancelarConfirmacion();
        Refrescar();
        Estado(TextoPartidaGuardada(), Apagado);
        panel.Mostrar();
        Sonar(SonidoSintetico.Subida(520f, 880f, 0.09f));
    }

    public void Cerrar()
    {
        if (!panel.Visible) return;
        CancelarConfirmacion();
        panel.Ocultar();
        if (GameManager.Instancia != null) GameManager.Instancia.Reanudar();
        Sonar(SonidoSintetico.Subida(880f, 520f, 0.09f));
    }

    void Sonar(AudioClip clip)
    {
        if (audioMenu == null) return;
        audioMenu.ignoreListenerPause = true;
        audioMenu.PlayOneShot(clip, 0.45f);
    }

    void Update()
    {
        if (esperandoConfirmacion != null && Time.unscaledTime >= confirmarHasta) CancelarConfirmacion();
    }

    void Guardar()
    {
        CancelarConfirmacion();
        bool ok = GameManager.Instancia != null && GameManager.Instancia.Guardar();
        Refrescar();
        if (ok) Estado("Partida guardada:  " + SaveManager.Resumen(SaveManager.Instancia.Ultima) + " restantes", HUDReloj.Verde);
        else Estado("No se pudo guardar la partida", HUDReloj.Rojo);
    }

    // Primer toque: el botón pregunta. Segundo toque (antes de 3 segundos): lo hace.
    void Confirmar(BotonMenu boton, string aviso, System.Action hacer)
    {
        if (esperandoConfirmacion == boton && Time.unscaledTime < confirmarHasta)
        {
            CancelarConfirmacion();
            hacer();
            return;
        }
        CancelarConfirmacion();
        esperandoConfirmacion = boton;
        confirmarHasta = Time.unscaledTime + SEGUNDOS_CONFIRMAR;
        boton.Textos("¿SEGURO? TOCÁ DE NUEVO", aviso);
        boton.Colorear(HUDReloj.Rojo);
    }

    void CancelarConfirmacion()
    {
        if (esperandoConfirmacion == null) return;
        esperandoConfirmacion.Colorear(null);
        esperandoConfirmacion = null;
        Refrescar();   // devuelve los textos de los botones
    }

    // Pone al día todo lo que muestra el menú
    void Refrescar()
    {
        var juego = GameManager.Instancia;
        var guardado = SaveManager.Instancia;
        if (juego == null) return;

        float restante = juego.reloj != null ? juego.reloj.TiempoRestante : 0f;
        textoTiempo.text = TimerController.Formato(restante);
        textoTiempo.color = juego.Gano ? HUDReloj.Verde : restante <= 60f ? HUDReloj.Rojo
                          : restante <= 300f ? HUDReloj.Ambar : HUDReloj.Blanco;

        int cuarto = juego.CuartoActual;
        textoCuarto.text = "CUARTO " + cuarto + " DE " + GameManager.TOTAL_CUARTOS;
        textoNombreCuarto.text = GameManager.NombreCuarto(cuarto);
        for (int i = 0; i < fichasCuartos.Length; i++)
        {
            int n = i + 1;
            Color color = n < cuarto || juego.Gano ? HUDReloj.Verde : n == cuarto ? HUDReloj.Ambar : Gris;
            fichasCuartos[i].GetPropertyBlock(bloque);
            bloque.SetColor("_BaseColor", color);
            fichasCuartos[i].SetPropertyBlock(bloque);
        }

        if (guardado != null)
        {
            textoAcertijos.text = guardado.AcertijosResueltos + " de " + guardado.TotalAcertijos + " acertijos resueltos";

            botonCargar.Habilitar(guardado.HayPartida);
            botonCargar.Textos(null, guardado.HayPartida
                ? SaveManager.Resumen(guardado.Ultima) + " restantes"
                : "No hay partida guardada");
        }

        // Ya ganó: no tiene sentido guardar (el reloj está detenido)
        botonGuardar.Habilitar(!juego.Gano);
        botonGuardar.Textos(null, juego.Gano ? "Ya saliste del colegio" : "Cuarto " + cuarto + " · " + TimerController.Formato(restante));
        botonReiniciar.Textos(null, null);
        botonMenuPrincipal.Textos(null, null);
    }

    string TextoPartidaGuardada()
    {
        var guardado = SaveManager.Instancia;
        if (guardado == null || !guardado.HayPartida) return "Todavía no hay una partida guardada";
        return "Última partida guardada:  " + SaveManager.Resumen(guardado.Ultima) + " restantes  ·  " + guardado.Ultima.fecha;
    }

    void Estado(string texto, Color color)
    {
        if (textoEstado == null) return;
        textoEstado.text = texto;
        textoEstado.color = color;
    }
}
