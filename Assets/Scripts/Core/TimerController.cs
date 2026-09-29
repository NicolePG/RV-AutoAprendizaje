using UnityEngine;
using UnityEngine.Events;

// El reloj de la partida: una cuenta regresiva de 15 minutos.
//
// Arranca solo al empezar el juego. GameManager lo pausa mientras está abierto el menú, lo detiene
// cuando se abre la salida del Cuarto 4 (ya ganó) y, al cargar una partida, le pone el tiempo que
// quedaba (Fijar).
// Avisa con eventos cuando quedan 5 minutos, cuando empieza el último minuto y cuando llega a cero.
//
// En la escena: va en el objeto "Sistema". ConstructorSistema lo arma.
public class TimerController : MonoBehaviour
{
    [Tooltip("Duración de la partida en segundos (15 minutos = 900)")]
    public float duracion = 900f;

    [Tooltip("Cuando quedan 5 minutos")]
    public UnityEvent alQuedarCincoMinutos = new UnityEvent();

    [Tooltip("Cuando empieza el último minuto")]
    public UnityEvent alUltimoMinuto = new UnityEvent();

    [Tooltip("Cuando llega a cero")]
    public UnityEvent alTerminar = new UnityEvent();

    public float TiempoRestante { get; private set; }
    public bool Corriendo { get; private set; }

    // En pausa el tiempo no avanza, pero el reloj sigue "corriendo" (al reanudar sigue solo)
    public bool Pausado { get; set; }

    // Cuánto tardó el jugador (sirve también después de cargar: cuenta desde el principio)
    public float TiempoUsado => duracion - TiempoRestante;

    void Awake()
    {
        TiempoRestante = duracion;
        Corriendo = true;
    }

    void Update()
    {
        if (!Corriendo || Pausado) return;

        float antes = TiempoRestante;
        TiempoRestante = Mathf.Max(0f, TiempoRestante - Time.deltaTime);

        if (Cruzo(antes, 300f)) alQuedarCincoMinutos.Invoke();
        if (Cruzo(antes, 60f)) alUltimoMinuto.Invoke();
        if (TiempoRestante <= 0f)
        {
            Corriendo = false;
            alTerminar.Invoke();
        }
    }

    // true si en este cuadro pasó por la marca (por ejemplo, de 300.01 a 299.98 segundos)
    bool Cruzo(float antes, float marca) => antes > marca && TiempoRestante <= marca;

    public void Detener() => Corriendo = false;

    // Pone el reloj en un tiempo dado (al cargar una partida) y lo deja corriendo desde ahí.
    // Nunca menos de un segundo: si no, la partida se terminaría apenas se carga.
    public void Fijar(float segundos)
    {
        TiempoRestante = Mathf.Clamp(segundos, 1f, duracion);
        Corriendo = true;
    }

    // "12:34": minutos y segundos, redondeando hacia arriba (con 0.4 s todavía muestra 00:01)
    public static string Formato(float segundos)
    {
        int total = Mathf.CeilToInt(Mathf.Max(0f, segundos));
        return (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
    }
}
