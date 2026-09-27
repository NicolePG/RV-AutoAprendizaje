using UnityEngine;

// El ruido de fondo de un cuarto, y cada tanto un crujido suelto.
//
// Sin esto el colegio queda en silencio absoluto entre una interacción y la otra, que es
// justo lo que rompe el clima en un escape room de terror: el jugador deja de sentir que
// el lugar existe y lo único que oye son sus propios clics.
//
// Son dos capas:
//
//  1. EL FONDO. Un ruido grave en bucle: la instalación eléctrica, el viento que entra por
//     las ventanas, el zumbido de los equipos. Suena todo el tiempo y no se nota; lo que se
//     nota es cuando no está.
//
//  2. LOS CRUJIDOS. Cada tantos segundos, un chirrido de madera o un golpe lejano, en un
//     punto al azar del cuarto. Son los que hacen mirar para atrás.
//
// El volumen sube cuando el cuarto está a oscuras y baja cuando vuelve la luz. Para saberlo
// mira la luz ambiental de la escena, que es lo que cambian ControlEnergia (Cuarto 2) y
// ClimaCuarto (Cuartos 3 y 4). Así funciona en los cuatro cuartos sin depender de ninguno.
//
// En la escena: lo pone el menú Escape Room > Construir los 4 cuartos, un objeto por cuarto,
// en el centro y a la altura de la cabeza. Si se le arrastra una grabación al campo "fondo",
// esa reemplaza al ruido armado por código.
[RequireComponent(typeof(AudioSource))]
public class AmbienteCuarto : MonoBehaviour
{
    [Header("Fondo")]
    [Tooltip("Nombre de la grabación en Resources/Audio, por ejemplo ambiente_cuarto2")]
    public string nombreDelFondo = "";

    [Tooltip("Grabación de fondo (opcional). Si está vacía se busca por 'nombreDelFondo', " +
             "y si tampoco está se arma el ruido por código")]
    public AudioClip fondo;

    [Tooltip("Grabación que suena UNA VEZ al entrar al cuarto, por encima del fondo. " +
             "Vacío = no suena ninguna")]
    public string nombreDeLaEntrada = "";

    [Tooltip("Volumen de esa entrada")]
    public float volumenEntrada = 0.8f;

    [Tooltip("Qué tan grave es el ruido: 0 = siseo fino, 0.95 = rugido grave")]
    [Range(0f, 0.97f)]
    public float gravedad = 0.9f;

    [Tooltip("Volumen del fondo con el cuarto a oscuras")]
    public float volumenSinLuz = 0.35f;

    [Tooltip("Volumen del fondo con la luz prendida")]
    public float volumenConLuz = 0.16f;

    [Tooltip("Hasta dónde llega el sonido de este cuarto, en metros")]
    public float alcance = 14f;

    [Header("Crujidos")]
    [Tooltip("Cada cuántos segundos suena uno: mínimo y máximo")]
    public Vector2 cadaCuanto = new Vector2(16f, 38f);

    [Tooltip("Volumen de los crujidos")]
    public float volumenCrujidos = 0.3f;

    [Tooltip("Qué tan lejos del centro del cuarto pueden sonar")]
    public float dispersion = 3f;

    AudioSource fuente;
    AudioSource entrada;       // la que suena una sola vez, al llegar
    bool yaEntro;
    float proximoCrujido;
    Transform camara;

    void Start()
    {
        fuente = GetComponent<AudioSource>();
        // 1) la grabación arrastrada a mano, 2) la de Resources/Audio, 3) el ruido por código
        AudioClip clip = fondo;
        if (clip == null && !string.IsNullOrEmpty(nombreDelFondo))
            clip = SonidoSintetico.Grabado(nombreDelFondo);
        if (clip == null) clip = SonidoSintetico.Ruido(gravedad, 3f);
        fuente.clip = clip;
        fuente.loop = true;
        fuente.playOnAwake = false;
        fuente.spatialBlend = 1f;                       // 3D: se oye el de cada cuarto
        fuente.rolloffMode = AudioRolloffMode.Linear;
        fuente.minDistance = 1f;
        fuente.maxDistance = alcance;
        fuente.volume = volumenSinLuz;
        fuente.Play();

        PrepararEntrada();
        ProgramarCrujido();
    }

    void Update()
    {
        // La luz ambiental va de casi negra (cuarto a oscuras) a gris claro (luz prendida).
        // Con 0.25 de gris ya se considera el cuarto iluminado.
        if (camara == null && Camera.main != null) camara = Camera.main.transform;
        ProbarEntrada();

        float luz = Mathf.InverseLerp(0.02f, 0.25f, RenderSettings.ambientLight.grayscale);
        float objetivo = Mathf.Lerp(volumenSinLuz, volumenConLuz, luz);
        fuente.volume = Mathf.MoveTowards(fuente.volume, objetivo, Time.deltaTime * 0.3f);

        if (Time.time < proximoCrujido) return;
        ProgramarCrujido();
        Crujir();
    }

    // La entrada del cuarto: un tema distinto que suena una sola vez, cuando el jugador
    // llega, por encima del fondo que se repite. Así el cuarto tiene un golpe de efecto al
    // entrar y después se queda el fondo, en vez de oírse siempre lo mismo.
    void PrepararEntrada()
    {
        if (string.IsNullOrEmpty(nombreDeLaEntrada)) return;

        AudioClip clip = SonidoSintetico.Grabado(nombreDeLaEntrada);
        if (clip == null) return;

        var go = new GameObject("Entrada");
        go.transform.SetParent(transform, false);
        entrada = go.AddComponent<AudioSource>();
        entrada.clip = clip;
        entrada.loop = false;
        entrada.playOnAwake = false;
        entrada.spatialBlend = 0f;        // 2D: es un golpe de efecto, no viene de un lugar
        entrada.volume = volumenEntrada;
    }

    // Suena la primera vez que el jugador se acerca, y nunca más
    void ProbarEntrada()
    {
        if (yaEntro || entrada == null || camara == null) return;
        if (Vector3.Distance(camara.position, transform.position) > alcance) return;

        yaEntro = true;
        entrada.Play();
    }

    void ProgramarCrujido() => proximoCrujido = Time.time + Random.Range(cadaCuanto.x, cadaCuanto.y);

    void Crujir()
    {
        // Solo cruje el cuarto donde está el jugador: si no, se oirían los cuatro a la vez
        if (camara == null)
        {
            if (Camera.main == null) return;
            camara = Camera.main.transform;
        }
        if (Vector3.Distance(camara.position, transform.position) > alcance) return;

        // En un punto al azar alrededor del centro del cuarto, nunca siempre en el mismo lado
        Vector3 donde = transform.position + new Vector3(
            Random.Range(-dispersion, dispersion),
            Random.Range(-1f, 0.6f),
            Random.Range(-dispersion, dispersion));

        // Uno de los cuatro crujidos grabados, al azar, para que no se repita siempre el
        // mismo. Si no estan los archivos, madera o un golpe armados por codigo.
        AudioClip clip = SonidoSintetico.Grabado("crujido" + Random.Range(1, 5));
        if (clip == null)
            clip = Random.value < 0.5f
                ? SonidoSintetico.Chirrido(Random.Range(0.4f, 0.9f))
                : SonidoSintetico.Golpe();

        SonidoSintetico.Tocar(clip, donde, volumenCrujidos);
    }
}
