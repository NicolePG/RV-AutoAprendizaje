using Unity.VRTemplate;
using UnityEngine;
using UnityEngine.Events;

// Una llave de paso de la línea de gas (V1 o V2) del Cuarto 4, acertijo 3.
//
// Es una válvula de compuerta de verdad: se abre agarrando el volante y GIRÁNDOLO con la
// mano, dos vueltas completas (el giro lo maneja XRKnob, la perilla de la plantilla de VR de
// Unity: en el Quest se gira la muñeca o la mano alrededor del volante; en el PC, mouse en
// círculos). Mientras gira, la aguja del manómetro sube y se oye el gas.
//
// Empieza BLOQUEADA (tiene colgada la tarjeta roja): hasta que la campana de extracción no
// está andando, el volante no se puede agarrar. Desbloquear() la habilita.
//
// "alMitad" avisa cuando va por la mitad del giro: ahí salta el susto de la camilla (con la
// segunda llave), porque el jugador tiene la mano ocupada y está de espaldas.
//
// En la escena: va en la válvula, junto a su XRKnob. ConstructorCuarto4 la arma.
public class ValvulaGas : MonoBehaviour
{
    public XRKnob volante;

    [Tooltip("Aguja del manómetro: gira sobre su Z")]
    public Transform aguja;
    public float recorridoAguja = 240f;

    [Tooltip("Siseo del gas mientras se abre")]
    public AudioSource siseo;

    [Tooltip("Tarjeta roja de bloqueo (se saca al habilitarla)")]
    public GameObject tarjetaBloqueo;

    [Tooltip("Tarjeta verde de habilitada (aparece al habilitarla)")]
    public GameObject tarjetaHabilitada;

    public float puntoDelMedio = 0.5f;
    public UnityEvent alMitad = new UnityEvent();
    public UnityEvent alAbrir = new UnityEvent();

    public bool Abierta { get; private set; }

    Quaternion agujaCero;
    float ultimoValor;
    bool avisoMitad;
    float siseoHasta;

    void Awake()
    {
        if (siseo != null && siseo.clip == null) siseo.clip = SonidoSintetico.Ruido(0.15f);
        if (aguja != null) agujaCero = aguja.localRotation;
        if (volante != null)
        {
            volante.onValueChange.AddListener(AlGirar);
            volante.enabled = false;   // bloqueada hasta que ande la campana
        }
        if (tarjetaHabilitada != null) tarjetaHabilitada.SetActive(false);
    }

    public void Desbloquear()
    {
        if (volante != null && !Abierta) volante.enabled = true;
        if (tarjetaBloqueo != null) tarjetaBloqueo.SetActive(false);
        if (tarjetaHabilitada != null) tarjetaHabilitada.SetActive(true);
        SonidoSintetico.Tocar(SonidoSintetico.Pitido(900f, 0.08f), transform.position, 0.5f);
    }

    void AlGirar(float valor)
    {
        if (Abierta) return;
        if (aguja != null) aguja.localRotation = agujaCero * Quaternion.Euler(0f, 0f, -valor * recorridoAguja);

        // Mientras el volante se mueve, suena el gas
        if (Mathf.Abs(valor - ultimoValor) > 0.001f) siseoHasta = Time.time + 0.25f;
        ultimoValor = valor;

        if (!avisoMitad && valor >= puntoDelMedio)
        {
            avisoMitad = true;
            alMitad.Invoke();
        }
        if (valor < 0.995f) return;

        // Abierta del todo: queda así (el volante ya no se mueve)
        Abierta = true;
        volante.enabled = false;
        if (siseo != null) siseo.Stop();
        SonidoSintetico.Tocar(SonidoSintetico.Golpe(), transform.position, 0.6f);
        alAbrir.Invoke();
    }

    void Update()
    {
        if (siseo == null || Abierta) return;
        bool girando = Time.time < siseoHasta;
        if (girando && !siseo.isPlaying) siseo.Play();
        if (!girando && siseo.isPlaying) siseo.Stop();
    }
}
