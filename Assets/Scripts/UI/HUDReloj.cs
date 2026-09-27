using TMPro;
using UnityEngine;

// El reloj de la partida en pantalla: una tarjeta chica fija en la esquina de arriba a la
// izquierda de la vista. Muestra el tiempo que queda, el cuarto (con cuatro rayitas: verde los
// que ya pasó, ámbar el actual, gris los que faltan), una barra que se vacía con el tiempo y,
// debajo, los avisos cortos ("PROGRESO GUARDADO", "QUEDAN 5 MINUTOS"...).
//
// Va colgado de la cámara (la cabeza), así acompaña la vista sin temblar. Es chico, está en la
// esquina y tiene el fondo semitransparente para no tapar lo que el jugador mira. Sus colores no
// dependen de la luz del cuarto (shader EscapeRoom/Interfaz): se lee igual en los cuatro cuartos.
// Se esconde mientras hay un menú abierto (el menú ya muestra el tiempo).
// Color del tiempo: normal (blanco), menos de 5 minutos (ámbar), último minuto (rojo, late),
// ganó (verde). En los últimos 10 segundos hace tic cada segundo.
//
// En la escena: va en "Sistema/HUD_Reloj". ConstructorSistema arma las piezas.
public class HUDReloj : MonoBehaviour
{
    public enum Tono { Info, Exito, Alerta, Peligro }

    public static readonly Color Blanco = new Color(0.94f, 0.96f, 0.98f);
    public static readonly Color Ambar = new Color(0.96f, 0.65f, 0.14f);
    public static readonly Color Rojo = new Color(0.94f, 0.3f, 0.32f);
    public static readonly Color Verde = new Color(0.2f, 0.78f, 0.47f);
    public static readonly Color Celeste = new Color(0.36f, 0.7f, 0.96f);
    public static readonly Color Gris = new Color(0.3f, 0.35f, 0.43f);

    public TimerController reloj;

    [Header("Piezas")]
    [Tooltip("La tarjeta del reloj: se esconde mientras hay un menú abierto")]
    public GameObject tarjeta;
    public TMP_Text textoTiempo;
    public TMP_Text textoCuarto;
    [Tooltip("La franja de color del costado izquierdo")]
    public Renderer acento;
    [Tooltip("Una rayita por cuarto, en orden")]
    public Renderer[] fichasCuartos;
    [Tooltip("El relleno de la barra de tiempo: se achica en X desde su borde izquierdo")]
    public Transform barra;
    public Renderer rellenoBarra;

    [Header("Aviso")]
    public GameObject aviso;
    public TMP_Text textoAviso;
    public Renderer franjaAviso;
    public float segundosAviso = 2.8f;

    [Header("Posición en la vista")]
    [Tooltip("Dónde queda respecto de los ojos, en metros: X negativo = izquierda, Y = arriba, Z = adelante")]
    public Vector3 posicionEnLaVista = new Vector3(-0.3f, 0.2f, 0.85f);

    Transform camara;
    MaterialPropertyBlock bloque;
    float avisoHasta = -1f;
    float avisoAvance;
    int ultimoSegundo = -1;
    int ultimoCuarto = -1;
    bool ultimoGano;
    Color ultimoColor;

    void Awake()
    {
        bloque = new MaterialPropertyBlock();
        if (aviso != null) aviso.SetActive(false);
    }

    void LateUpdate()
    {
        if (camara == null) Enganchar();
        if (camara == null) return;

        // Con un menú abierto no se ve (el menú ya tiene el tiempo y quedaría encima de él)
        var juego = GameManager.Instancia;
        bool hayMenu = juego != null &&
                       (juego.EstadoActual == GameManager.Estado.EnMenu || juego.EstadoActual == GameManager.Estado.Pausado ||
                        juego.EstadoActual == GameManager.Estado.TiempoAgotado);
        if (tarjeta != null && tarjeta.activeSelf == hayMenu) tarjeta.SetActive(!hayMenu);
        if (aviso != null && hayMenu && aviso.activeSelf) aviso.SetActive(false);
        if (hayMenu) return;

        MostrarTiempo();
        MostrarCuarto();
        AnimarAviso();
    }

    // Se cuelga de la cámara, en la esquina, de frente a los ojos
    void Enganchar()
    {
        if (Camera.main == null) return;
        camara = Camera.main.transform;
        transform.SetParent(camara, false);
        transform.localPosition = posicionEnLaVista;
        // Su +Z sigue la línea de la mirada hacia él: así su frente (-Z) queda de cara a los ojos
        transform.localRotation = Quaternion.LookRotation(posicionEnLaVista.normalized, Vector3.up);
    }

    void MostrarTiempo()
    {
        if (reloj == null || textoTiempo == null) return;
        float restante = reloj.TiempoRestante;

        // El texto se rearma solo cuando cambia el segundo (TextMeshPro es caro de actualizar)
        int segundo = Mathf.CeilToInt(restante);
        if (segundo != ultimoSegundo)
        {
            ultimoSegundo = segundo;
            textoTiempo.text = TimerController.Formato(restante);
            // Tic en los últimos 10 segundos (más agudo en los últimos 3)
            if (reloj.Corriendo && !reloj.Pausado && segundo > 0 && segundo <= 10)
                SonidoSintetico.Tocar(SonidoSintetico.Pitido(segundo <= 3 ? 1500f : 1100f, 0.05f), camara.position, 0.45f);
        }

        bool gano = GameManager.Instancia != null && GameManager.Instancia.Gano;
        Color color = gano ? Verde : restante <= 60f ? Rojo : restante <= 300f ? Ambar : Blanco;
        if (color != ultimoColor)
        {
            ultimoColor = color;
            textoTiempo.color = color;
            Color franja = color == Blanco ? Celeste : color;
            Pintar(acento, franja);
            Pintar(rellenoBarra, franja);
        }

        // En el último minuto el número late
        bool late = reloj.Corriendo && restante <= 60f;
        float escala = late ? 1f + 0.06f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * Mathf.PI)) : 1f;
        textoTiempo.transform.localScale = Vector3.one * escala;

        if (barra != null)
            barra.localScale = new Vector3(Mathf.Clamp01(restante / reloj.duracion), 1f, 1f);
    }

    void MostrarCuarto()
    {
        var juego = GameManager.Instancia;
        if (juego == null) return;
        int cuarto = juego.CuartoActual;
        bool gano = juego.Gano;
        if (cuarto == ultimoCuarto && gano == ultimoGano) return;
        ultimoCuarto = cuarto;
        ultimoGano = gano;

        if (textoCuarto != null) textoCuarto.text = cuarto + "/" + GameManager.TOTAL_CUARTOS;
        if (fichasCuartos == null) return;
        for (int i = 0; i < fichasCuartos.Length; i++)
        {
            int n = i + 1;
            Pintar(fichasCuartos[i], n < cuarto || gano ? Verde : n == cuarto ? Ambar : Gris);
        }
    }

    // Muestra un aviso corto debajo del reloj. Si llega otro mientras se ve, lo reemplaza.
    public void Avisar(string texto, Tono tono)
    {
        if (aviso == null || textoAviso == null) return;
        textoAviso.text = texto;
        Pintar(franjaAviso, ColorDe(tono));
        aviso.SetActive(true);
        avisoHasta = Time.unscaledTime + segundosAviso;
    }

    void AnimarAviso()
    {
        if (aviso == null || !aviso.activeSelf) return;
        bool mostrando = Time.unscaledTime < avisoHasta;
        avisoAvance = Mathf.MoveTowards(avisoAvance, mostrando ? 1f : 0f, Time.unscaledDeltaTime / 0.18f);
        float suave = avisoAvance * avisoAvance * (3f - 2f * avisoAvance);
        aviso.transform.localScale = new Vector3(1f, Mathf.Lerp(0.2f, 1f, suave), 1f);
        if (!mostrando && avisoAvance <= 0f) aviso.SetActive(false);
    }

    public static Color ColorDe(Tono tono)
    {
        switch (tono)
        {
            case Tono.Exito: return Verde;
            case Tono.Alerta: return Ambar;
            case Tono.Peligro: return Rojo;
            default: return Celeste;
        }
    }

    // Cambia el color de una pieza sin tocar su material (que comparten todas las piezas)
    void Pintar(Renderer r, Color color)
    {
        if (r == null) return;
        r.GetPropertyBlock(bloque);
        bloque.SetColor("_BaseColor", color);
        r.SetPropertyBlock(bloque);
    }
}
