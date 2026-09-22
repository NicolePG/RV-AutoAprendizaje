using UnityEngine;
using UnityEngine.Rendering;

// El "clima" de un cuarto: su luz ambiental, cuánto brillan los reflejos y la niebla.
// Esos valores son de TODA la escena (no de un cuarto), así que cada cuarto pone los suyos
// cuando el jugador entra. Si no, se vería con el clima que dejó el cuarto anterior.
//
// Tiene dos climas: a oscuras (de noche) y con las luces prendidas. "nivel" dice cuánto de
// cada uno se usa (0 = noche, 1 = luces prendidas), así el cuarto se va aclarando de a poco
// a medida que se prenden las lámparas (lo hace TableroLuces con Mezclar()).
//
// Además de la luz ambiental se arma la "sonda ambiental" (ambientProbe) con el mismo color:
// es la que usan de verdad los objetos que no tienen luz horneada, y así el cambio se ve
// aunque la escena tenga iluminación horneada (la del Cuarto 1).
//
// En la escena: va en la zona de la entrada del cuarto, junto a un DisparadorJugador que
// llama a Aplicar() cuando entra el jugador. ConstructorCuarto3 lo arma solo.
public class ClimaCuarto : MonoBehaviour
{
    [Header("A oscuras (de noche)")]
    [Tooltip("Luz que llega a todos lados por igual (lo que se ve en las sombras)")]
    public Color luzAmbiente = new Color(0.035f, 0.04f, 0.06f);

    [Tooltip("Cuánto se refleja el entorno en metales y vidrios (0 = nada, 1 = normal)")]
    public float reflejos = 0.1f;

    [Tooltip("Color de la niebla. Conviene que sea parecido a la luz ambiental, más oscuro")]
    public Color colorNiebla = new Color(0.01f, 0.012f, 0.02f);

    [Tooltip("Qué tan espesa es la niebla. 0 = sin niebla")]
    public float densidadNiebla = 0.05f;

    [Header("Con las luces prendidas")]
    public Color luzAmbienteEncendido = new Color(0.36f, 0.37f, 0.4f);
    public float reflejosEncendido = 0.6f;
    public Color colorNieblaEncendido = new Color(0.1f, 0.1f, 0.11f);
    public float densidadNieblaEncendido = 0.012f;

    [Tooltip("0 = noche, 1 = luces prendidas")]
    [Range(0f, 1f)] public float nivel;

    // Cambia el nivel y lo aplica ya (el jugador está en el cuarto)
    public void Mezclar(float nuevoNivel)
    {
        nivel = Mathf.Clamp01(nuevoNivel);
        Aplicar();
    }

    public void Aplicar()
    {
        Color ambiente = Color.Lerp(luzAmbiente, luzAmbienteEncendido, nivel);
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = ambiente;

        var sonda = new SphericalHarmonicsL2();
        sonda.AddAmbientLight(ambiente);
        RenderSettings.ambientProbe = sonda;

        RenderSettings.reflectionIntensity = Mathf.Lerp(reflejos, reflejosEncendido, nivel);

        float densidad = Mathf.Lerp(densidadNiebla, densidadNieblaEncendido, nivel);
        RenderSettings.fog = densidad > 0f;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogColor = Color.Lerp(colorNiebla, colorNieblaEncendido, nivel);
        RenderSettings.fogDensity = densidad;
    }
}
