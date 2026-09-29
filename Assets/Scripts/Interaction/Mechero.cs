using UnityEngine;

// Un mechero Bunsen de la mesada (Cuarto 4). Se enciende cuando llega el gas (LineaGas).
//
// Su llama es azul. Tiene una zona invisible (collider "Is Trigger") alrededor de la llama:
// si entra un asa de muestra (MuestraLlama), la llama crece, brilla más y se tiñe del color
// de esa muestra mientras el asa siga adentro. Es el ensayo a la llama de verdad: así se
// identifica qué metal tiene cada muestra.
//
// En la escena: va en la zona de la llama del mechero, con su collider "Is Trigger".
// ConstructorCuarto4 lo arma.
[RequireComponent(typeof(Collider))]
public class Mechero : MonoBehaviour
{
    [Tooltip("La llama (empieza apagada)")]
    public GameObject llama;

    [Tooltip("Las partes de la llama que cambian de color")]
    public Renderer[] partesLlama;

    [Tooltip("La luz de la llama")]
    public Light luz;

    public Color colorNormal = new Color(0.35f, 0.6f, 1f);

    [Tooltip("Cuánto crece la llama con una muestra adentro (así el color se ve de lejos)")]
    public float crecimiento = 1.8f;

    [Tooltip("Rugido suave del mechero (en loop)")]
    public AudioSource sonido;

    public bool Encendido { get; private set; }

    MaterialPropertyBlock bloque;
    MuestraLlama adentro;
    Vector3 escalaLlama;      // el tamaño de la llama sola, como quedó en el editor
    Vector3 escalaActual;     // el de ahora: más grande con una muestra adentro
    float intensidadLuz;
    float semilla;            // para que cada mechero tiemble distinto

    void Awake()
    {
        bloque = new MaterialPropertyBlock();
        if (llama != null)
        {
            escalaLlama = llama.transform.localScale;
            escalaActual = escalaLlama;
            llama.SetActive(false);
        }
        if (luz != null) intensidadLuz = luz.intensity;
        if (sonido != null && sonido.clip == null) sonido.clip = SonidoSintetico.Ruido(0.93f);
        semilla = transform.position.x * 13.7f + transform.position.z * 5.3f;
    }

    // Una llama nunca está quieta: se estira y se achica un poco todo el tiempo
    void Update()
    {
        if (!Encendido || llama == null) return;
        float ruido = Mathf.PerlinNoise(Time.time * 6f, semilla) - 0.5f;
        llama.transform.localScale = Vector3.Scale(escalaActual, new Vector3(1f - ruido * 0.15f, 1f + ruido * 0.3f, 1f - ruido * 0.15f));
    }

    public void Encender()
    {
        if (Encendido) return;
        Encendido = true;
        if (llama != null) llama.SetActive(true);
        if (sonido != null) sonido.Play();
        SonidoSintetico.Tocar(SonidoSintetico.Subida(120f, 300f, 0.25f), transform.position, 0.5f);
        Actualizar();   // por si ya tenían la muestra puesta
    }

    void OnTriggerEnter(Collider otro)
    {
        var muestra = otro.GetComponentInParent<MuestraLlama>();
        if (muestra == null) return;
        adentro = muestra;
        // El "fuuu" de la sal al entrar en la llama
        if (Encendido) SonidoSintetico.Tocar(SonidoSintetico.Subida(200f, 480f, 0.2f), transform.position, 0.4f);
        Actualizar();
    }

    void OnTriggerExit(Collider otro)
    {
        var muestra = otro.GetComponentInParent<MuestraLlama>();
        if (muestra == null || muestra != adentro) return;
        adentro = null;
        Actualizar();
    }

    // Llama chica y azul, o grande y del color de la muestra que tiene adentro
    void Actualizar()
    {
        if (!Encendido) return;
        bool conMuestra = adentro != null;
        Pintar(conMuestra ? adentro.colorLlama : colorNormal);
        escalaActual = escalaLlama * (conMuestra ? crecimiento : 1f);
        if (luz != null) luz.intensity = intensidadLuz * (conMuestra ? 2.5f : 1f);
    }

    // Tiñe la llama y su luz sin tocar el material compartido (MaterialPropertyBlock)
    void Pintar(Color color)
    {
        foreach (Renderer r in partesLlama)
        {
            if (r == null) continue;
            r.GetPropertyBlock(bloque);
            bloque.SetColor("_BaseColor", new Color(color.r, color.g, color.b, 0.75f));
            bloque.SetColor("_EmissionColor", color * 2.2f);
            r.SetPropertyBlock(bloque);
        }
        if (luz != null) luz.color = color;
    }
}
