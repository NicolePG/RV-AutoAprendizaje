using UnityEngine;
using UnityEngine.Events;

// Segundo paso del laboratorio: la línea de gas.
//
// Cuenta las llaves que el jugador va abriendo. Cuando están las dos, el gas empieza a
// salir: se llena el cuarto de niebla, encienden los mecheros y, sobre todo, la niebla
// hace visibles los haces de luz que hasta ese momento no se veían. Esos haces son los
// que muestran el orden de las palancas (el tercer paso).
//
// En la escena: va en un objeto vacío llamado "Gas". Cada LlaveDeGas llama a AbrirUna()
// desde su evento "alAbrir".
public class SistemaGas : MonoBehaviour
{
    [Tooltip("Cuántas llaves hay que abrir")]
    public int llavesNecesarias = 2;

    [Tooltip("Lo que aparece cuando sale el gas: vapor, llamas de los mecheros y los haces de luz")]
    public GameObject[] objetosConGas;

    [Tooltip("Sonido del gas llenando la línea")]
    public AudioSource sonido;

    [Header("Niebla que larga el gas")]
    public Color colorNiebla = new Color(0.16f, 0.26f, 0.2f);
    public float densidadNiebla = 0.055f;

    [Tooltip("Cuánto tarda la niebla en llenar el cuarto, en segundos")]
    public float segundosDeNiebla = 3f;

    [Tooltip("Qué pasa cuando el gas queda abierto del todo")]
    public UnityEvent alAbrirTodo = new UnityEvent();

    public bool Abierto { get; private set; }

    int abiertas;
    float densidadInicial;
    Color colorInicial;
    float t;

    // Lo llama cada llave de gas cuando termina de abrirse
    public void AbrirUna()
    {
        if (Abierto) return;

        abiertas++;
        if (abiertas < llavesNecesarias) return;

        Abierto = true;
        densidadInicial = RenderSettings.fogDensity;
        colorInicial = RenderSettings.fogColor;
        RenderSettings.fog = true;

        foreach (var objeto in objetosConGas)
            if (objeto != null) objeto.SetActive(true);

        if (sonido != null) sonido.Play();
        alAbrirTodo.Invoke();
    }

    void Update()
    {
        if (!Abierto || t >= 1f) return;

        // La niebla no aparece de golpe: va llenando el cuarto
        t += Time.deltaTime / Mathf.Max(0.1f, segundosDeNiebla);
        RenderSettings.fogDensity = Mathf.Lerp(densidadInicial, densidadNiebla, Mathf.Clamp01(t));
        RenderSettings.fogColor = Color.Lerp(colorInicial, colorNiebla, Mathf.Clamp01(t));
    }
}
