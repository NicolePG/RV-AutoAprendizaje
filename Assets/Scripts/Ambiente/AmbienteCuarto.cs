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
    [Tooltip("Grabación de fondo (opcional). Vacío = se arma por código")]
    public AudioClip fondo;

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
    float proximoCrujido;
    Transform camara;

    void Start()
    {
        fuente = GetComponent<AudioSource>();
        fuente.clip = fondo != null ? fondo : SonidoSintetico.Ruido(gravedad, 3f);
        fuente.loop = true;
        fuente.playOnAwake = false;
        fuente.spatialBlend = 1f;                       // 3D: se oye el de cada cuarto
        fuente.rolloffMode = AudioRolloffMode.Linear;
        fuente.minDistance = 1f;
        fuente.maxDistance = alcance;
        fuente.volume = volumenSinLuz;
        fuente.Play();

        ProgramarCrujido();
    }

    void Update()
    {
        // La luz ambiental va de casi negra (cuarto a oscuras) a gris claro (luz prendida).
        // Con 0.25 de gris ya se considera el cuarto iluminado.
        float luz = Mathf.InverseLerp(0.02f, 0.25f, RenderSettings.ambientLight.grayscale);
        float objetivo = Mathf.Lerp(volumenSinLuz, volumenConLuz, luz);
        fuente.volume = Mathf.MoveTowards(fuente.volume, objetivo, Time.deltaTime * 0.3f);

        if (Time.time < proximoCrujido) return;
        ProgramarCrujido();
        Crujir();
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

        // La mitad de las veces madera que cruje, la otra mitad un golpe lejano
        AudioClip clip = Random.value < 0.5f
            ? SonidoSintetico.Chirrido(Random.Range(0.4f, 0.9f))
            : SonidoSintetico.Golpe();

        SonidoSintetico.Tocar(clip, donde, volumenCrujidos);
    }
}
