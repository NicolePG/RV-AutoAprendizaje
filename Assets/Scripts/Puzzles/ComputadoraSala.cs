using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Una computadora de la sala del Cuarto 3 (acertijo 3).
//
// Se prende y se apaga tocando la torre. Qué muestra depende de cómo esté la sala:
//  - Sin energía (antes de prender las luces): no hace nada.
//  - Con energía, al prenderla arranca ("Iniciando...") y después, según su tipo:
//     Clave: muestra su nombre y su IP. Si SU cable de red está en SU toma (acertijo 2),
//            muestra además su fragmento de la clave de la consola; si no, "SIN CONEXIÓN".
//            No dice en qué lugar va el fragmento: la consola pide ordenarlos por IP.
//     PantallaAzul y SinSenal: las computadoras rotas del GDD (C1 y C4), son señuelos.
// El carácter sale de la clave guardada en el PuzzleData de la consola: así la clave se
// cambia en un solo lugar (el asset) y las pantallas la siguen solas. "orden" es el lugar
// de esta computadora al ordenar las tres por IP (1 = la IP más baja).
//
// También muestra la cara del susto cuando se lo pide SustoSillas, aunque esté apagada.
//
// En la escena: va en el grupo "Computadora_C1", etc. ConstructorCuarto3 lo arma solo.
public class ComputadoraSala : MonoBehaviour
{
    public enum Tipo { Clave, PantallaAzul, SinSenal }

    public Tipo tipo;

    [Tooltip("Nombre de la computadora (C1, C2...)")]
    public string nombre = "C1";

    [Tooltip("Su dirección IP (la misma que figura en el mapa de la red)")]
    public string ip = "192.168.0.1";

    [Tooltip("Solo para las de tipo Clave: de dónde sale la clave (el PuzzleData de la consola)")]
    public PuzzleData clave;

    [Tooltip("Solo para las de tipo Clave: qué carácter de la clave muestra (1 = el primero). " +
             "Es su lugar al ordenar las computadoras por IP, de menor a mayor.")]
    public int orden = 1;

    [Tooltip("La torre: tocarla prende o apaga la computadora")]
    public XRSimpleInteractable torre;

    public Renderer pantalla;
    public TMP_Text texto;

    [Tooltip("Lucecita de encendido de la torre")]
    public Renderer luzEncendido;

    [Tooltip("La cara del susto (se muestra un instante)")]
    public GameObject cara;

    public Material pantallaApagada, pantallaArranque, pantallaSistema, pantallaAzul, pantallaCara;
    public Material luzPrendida, luzApagada;

    [Tooltip("Cuando empieza a mostrar su parte de la clave")]
    public UnityEvent alMostrarClave = new UnityEvent();

    [Tooltip("Cuando se prende")]
    public UnityEvent alPrender = new UnityEvent();

    public bool Encendida { get; private set; }
    public bool MuestraClave { get; private set; }

    bool energia, red, arrancando;

    void Awake()
    {
        if (torre != null) torre.selectEntered.AddListener(_ => Alternar());
        MostrarApagada();
    }

    // Llega cuando se prenden las luces de la sala
    public void DarEnergia() => energia = true;

    // Lo llama RedSala: true cuando SU ficha está en SU toma. Si ya estaba prendida, se actualiza sola.
    public void CambiarRed(bool conRed)
    {
        if (red == conRed) return;
        red = conRed;
        if (Encendida && !arrancando) MostrarSistema();
    }

    public void Alternar()
    {
        if (!energia)
        {
            // Sin energía la torre no responde: solo el ruido seco del botón
            SonidoSintetico.Tocar(SonidoSintetico.Pitido(300f, 0.04f), transform.position, 0.5f);
            return;
        }
        if (arrancando) return;
        if (Encendida) Apagar();
        else StartCoroutine(Arrancar());
    }

    IEnumerator Arrancar()
    {
        arrancando = true;
        Encendida = true;
        if (luzEncendido != null) luzEncendido.sharedMaterial = luzPrendida;
        alPrender.Invoke();
        SonidoSintetico.Tocar(SonidoSintetico.Subida(220f, 880f, 0.6f), transform.position, 0.6f);

        Pantalla(pantallaArranque, "<size=60%>" + nombre + "  ·  SALA DE COMPUTACIÓN</size>\n\nIniciando...", new Color(0.65f, 0.75f, 0.85f));
        yield return new WaitForSeconds(1.4f);
        arrancando = false;
        if (Encendida) MostrarSistema();
    }

    void MostrarSistema()
    {
        switch (tipo)
        {
            case Tipo.Clave:
                if (!red)
                {
                    MuestraClave = false;
                    Pantalla(pantallaSistema, Encabezado("SIN RED") + "\n\nSIN CONEXIÓN A LA RED\n<size=60%>Conecte el cable a su toma\n(ver el mapa de la red)</size>",
                             new Color(1f, 0.72f, 0.25f));
                    return;
                }
                Pantalla(pantallaSistema, Encabezado("CONECTADO") + "\n\n<size=60%>FRAGMENTO DE LA CLAVE</size>\n<size=230%>" + Caracter + "</size>",
                         new Color(0.4f, 1f, 0.6f));
                SonidoSintetico.Tocar(SonidoSintetico.Pitido(1320f, 0.12f), transform.position, 0.6f);
                if (!MuestraClave)
                {
                    MuestraClave = true;
                    alMostrarClave.Invoke();
                }
                break;

            case Tipo.PantallaAzul:
                Pantalla(pantallaAzul, ":(\n<size=60%>El equipo encontró un problema y se detuvo.\n\nERROR: DISCO NO ENCONTRADO</size>", Color.white);
                SonidoSintetico.Tocar(SonidoSintetico.Zumbido(160f, 0.35f), transform.position, 0.6f);
                break;

            case Tipo.SinSenal:
                Pantalla(pantallaApagada, "SIN SEÑAL", new Color(0.55f, 0.55f, 0.6f));
                break;
        }
    }

    void Apagar()
    {
        Encendida = false;
        StopAllCoroutines();
        arrancando = false;
        MostrarApagada();
        SonidoSintetico.Tocar(SonidoSintetico.Pitido(500f, 0.06f), transform.position, 0.5f);
    }

    void MostrarApagada()
    {
        if (luzEncendido != null) luzEncendido.sharedMaterial = luzApagada;
        Pantalla(pantallaApagada, "", Color.white);
        if (cara != null) cara.SetActive(false);
    }

    // La cara del susto: aparece y titila un instante, aunque la computadora esté apagada
    public void MostrarCara(float segundos) => StartCoroutine(Cara(segundos));

    IEnumerator Cara(float segundos)
    {
        Material antes = pantalla != null ? pantalla.sharedMaterial : null;
        bool textoAntes = texto != null && texto.enabled;
        if (texto != null) texto.enabled = false;

        for (float t = 0f; t < segundos; t += 0.07f)
        {
            bool visible = Random.value > 0.25f;   // titila, como una pantalla que falla
            if (cara != null) cara.SetActive(visible);
            if (pantalla != null) pantalla.sharedMaterial = visible ? pantallaCara : pantallaApagada;
            yield return new WaitForSeconds(0.07f);
        }

        if (cara != null) cara.SetActive(false);
        if (pantalla != null) pantalla.sharedMaterial = antes;
        if (texto != null) texto.enabled = textoAntes;
    }

    void Pantalla(Material fondo, string mensaje, Color color)
    {
        if (pantalla != null && fondo != null) pantalla.sharedMaterial = fondo;
        if (texto == null) return;
        texto.text = mensaje;
        texto.color = color;
    }

    // Primera línea de la pantalla: nombre, IP y estado de la red
    string Encabezado(string estado) => "<size=55%>" + nombre + "  ·  IP " + ip + "  ·  " + estado + "</size>";

    string Caracter
    {
        get
        {
            if (clave == null || string.IsNullOrEmpty(clave.solucion) || orden < 1 || orden > clave.solucion.Length) return "?";
            return clave.solucion.Substring(orden - 1, 1).ToUpper();
        }
    }
}
