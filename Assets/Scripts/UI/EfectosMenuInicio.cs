using System.Collections;
using TMPro;
using UnityEngine;

// Los efectos del menú de inicio, una pantalla de terror en medio del apagón:
//  - El título rojo ("BACKROOM") parpadea de vez en cuando como un cartel que se queda sin luz,
//    con un chasquido eléctrico.
//  - La imagen de fondo tiene fallas de señal: cada tanto salta de costado y se tiñe de rojo un
//    instante, y una línea de interferencia baja por la pantalla.
//  - El LED de alerta titila una vez por segundo.
//  - La baliza roja y una luz roja alrededor del jugador laten como una alarma, y con cada latido
//    se oyen dos golpes graves, como un corazón. Debajo, un zumbido grave continuo.
// Todo pasa solo mientras el menú está abierto, y con el tiempo real (el juego está congelado).
//
// En la escena: va en "Sistema/Menu_Inicio", junto a MainMenu. ConstructorSistema lo arma.
public class EfectosMenuInicio : MonoBehaviour
{
    public PanelFlotante panel;

    [Tooltip("El texto que parpadea (el título rojo)")]
    public TMP_Text tituloParpadeante;

    [Tooltip("La imagen de fondo: salta y se tiñe en las fallas de señal")]
    public Renderer fondo;

    [Tooltip("La línea de interferencia que baja por la pantalla")]
    public Transform lineaInterferencia;
    public float altoPantalla = 0.7f;

    [Tooltip("El LED de alerta de la línea de arriba")]
    public Renderer ledAlerta;

    [Tooltip("La baliza roja de arriba del panel")]
    public Renderer baliza;

    [Tooltip("La luz roja que tiñe el cuarto")]
    public Light luzAlarma;
    public float intensidadLuz = 1.4f;

    [Tooltip("El zumbido de fondo (opcional, en loop, no se pausa con el juego)")]
    public AudioSource zumbido;

    [Tooltip("Sonido de los menús: para el chasquido y el latido")]
    public AudioSource audioMenu;

    static readonly Color RojoApagado = new Color(0.28f, 0.03f, 0.04f);
    static readonly Color RojoEncendido = new Color(1f, 0.16f, 0.14f);
    const float SEGUNDOS_LATIDO = 1.7f;
    const float SEGUNDOS_INTERFERENCIA = 6f;   // baja en 4 s y espera 2

    MaterialPropertyBlock bloque;
    float proximoParpadeo, proximaFalla, faseAnterior;
    bool parpadeando, fallando;
    Vector3 posicionFondo, posicionTitulo;

    void Awake()
    {
        bloque = new MaterialPropertyBlock();
        if (zumbido != null)
        {
            zumbido.clip = SonidoSintetico.Ruido(0.97f, 3f);   // grave y continuo
            zumbido.loop = true;
            zumbido.ignoreListenerPause = true;
        }
        if (luzAlarma != null) luzAlarma.enabled = false;
        if (fondo != null) posicionFondo = fondo.transform.localPosition;
        if (tituloParpadeante != null) posicionTitulo = tituloParpadeante.transform.localPosition;
        proximoParpadeo = 2f;
        proximaFalla = 3.5f;
    }

    void Update()
    {
        bool abierto = panel != null && panel.Visible;
        if (!abierto)
        {
            if (zumbido != null && zumbido.isPlaying) zumbido.Stop();
            if (luzAlarma != null) luzAlarma.enabled = false;
            return;
        }
        if (zumbido != null && !zumbido.isPlaying) zumbido.Play();

        float t = Time.unscaledTime;

        // La alarma: sube rápido y baja despacio. Al empezar cada latido, el corazón.
        float fase = (t % SEGUNDOS_LATIDO) / SEGUNDOS_LATIDO;
        if (fase < faseAnterior) StartCoroutine(Corazon());
        faseAnterior = fase;
        float latido = fase < 0.12f ? fase / 0.12f : 1f - (fase - 0.12f) / 0.88f;
        latido *= latido;
        if (luzAlarma != null)
        {
            luzAlarma.enabled = true;
            luzAlarma.intensity = intensidadLuz * latido;
        }
        Pintar(baliza, Color.Lerp(RojoApagado, RojoEncendido, latido));

        // El LED: medio segundo prendido, medio apagado
        Pintar(ledAlerta, t % 1f < 0.5f ? RojoEncendido : RojoApagado);

        // La línea de interferencia baja de arriba a abajo y después espera
        if (lineaInterferencia != null)
        {
            float recorrido = (t % SEGUNDOS_INTERFERENCIA) / 4f;
            bool visible = recorrido <= 1f;
            if (lineaInterferencia.gameObject.activeSelf != visible) lineaInterferencia.gameObject.SetActive(visible);
            Vector3 pos = lineaInterferencia.localPosition;
            pos.y = Mathf.Lerp(altoPantalla / 2f, -altoPantalla / 2f, recorrido);
            lineaInterferencia.localPosition = pos;
        }

        if (!parpadeando && t >= proximoParpadeo) StartCoroutine(Parpadear());
        if (!fallando && t >= proximaFalla) StartCoroutine(FallaDeSenal());
    }

    // Dos golpes graves seguidos: pum-pum
    IEnumerator Corazon()
    {
        if (audioMenu == null) yield break;
        audioMenu.PlayOneShot(SonidoSintetico.Golpe(), 0.5f);
        yield return new WaitForSecondsRealtime(0.24f);
        audioMenu.PlayOneShot(SonidoSintetico.Golpe(), 0.32f);
    }

    // El título: dos a cuatro cortes rápidos, con un chasquido al primero
    IEnumerator Parpadear()
    {
        parpadeando = true;
        if (audioMenu != null) audioMenu.PlayOneShot(SonidoSintetico.Ruido(0.3f, 0.07f), 0.25f);
        int cortes = Random.Range(2, 5);
        for (int i = 0; i < cortes && tituloParpadeante != null; i++)
        {
            tituloParpadeante.alpha = Random.Range(0.08f, 0.35f);
            yield return new WaitForSecondsRealtime(Random.Range(0.03f, 0.09f));
            tituloParpadeante.alpha = 1f;
            yield return new WaitForSecondsRealtime(Random.Range(0.04f, 0.14f));
        }
        proximoParpadeo = Time.unscaledTime + Random.Range(2.5f, 6f);
        parpadeando = false;
    }

    // La imagen salta de costado y se tiñe de rojo, dos o tres veces muy rápido; el título tiembla
    IEnumerator FallaDeSenal()
    {
        fallando = true;
        int saltos = Random.Range(2, 4);
        for (int i = 0; i < saltos; i++)
        {
            Vector3 salto = Vector3.right * Random.Range(-0.008f, 0.008f);
            if (fondo != null) fondo.transform.localPosition = posicionFondo + salto;
            if (tituloParpadeante != null) tituloParpadeante.transform.localPosition = posicionTitulo - salto * 0.6f;
            Pintar(fondo, new Color(1f, 0.55f, 0.55f));
            yield return new WaitForSecondsRealtime(Random.Range(0.04f, 0.08f));
            if (fondo != null) fondo.transform.localPosition = posicionFondo;
            if (tituloParpadeante != null) tituloParpadeante.transform.localPosition = posicionTitulo;
            Pintar(fondo, Color.white);
            yield return new WaitForSecondsRealtime(Random.Range(0.05f, 0.12f));
        }
        proximaFalla = Time.unscaledTime + Random.Range(4f, 9f);
        fallando = false;
    }

    void Pintar(Renderer r, Color color)
    {
        if (r == null) return;
        r.GetPropertyBlock(bloque);
        bloque.SetColor("_BaseColor", color);
        r.SetPropertyBlock(bloque);
    }
}
