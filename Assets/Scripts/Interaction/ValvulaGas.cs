using TMPro;
using Unity.VRTemplate;
using UnityEngine;
using UnityEngine.Events;

// Una llave de paso de la línea de gas (V1 o V2) del Cuarto 4, acertijo 3.
//
// Es una válvula esférica moderna, la que se usa para gas: una manija amarilla que se gira un
// cuarto de vuelta. Cerrada, la manija queda atravesada al caño; abierta, queda paralela
// (así se sabe de un vistazo cómo está cualquier llave de gas). El giro lo maneja XRKnob, la
// perilla de la plantilla de VR de Unity: se agarra la manija y se gira la mano hacia arriba.
// Mientras gira, el visor digital marca la presión y se oye el gas.
//
// Empieza BLOQUEADA (cartel rojo): hasta que la campana de extracción no está andando, la
// manija no se puede agarrar. Desbloquear() la habilita (cartel verde).
//
// "alMitad" avisa cuando va por la mitad del giro: ahí salta el susto de la camilla (con la
// segunda llave), porque el jugador tiene la mano ocupada y está de espaldas.
//
// En la escena: va en la válvula, junto a su XRKnob. ConstructorCuarto4 la arma.
public class ValvulaGas : MonoBehaviour
{
    public XRKnob volante;

    [Tooltip("Visor digital de presión")]
    public TMP_Text presion;

    [Tooltip("Presión de la línea con la llave abierta del todo, en kPa")]
    public float presionMaxima = 2.1f;

    [Tooltip("Siseo del gas mientras se abre")]
    public AudioSource siseo;

    [Tooltip("Cartel rojo de bloqueo (se apaga al habilitarla)")]
    public GameObject tarjetaBloqueo;

    [Tooltip("Cartel verde de habilitada (aparece al habilitarla)")]
    public GameObject tarjetaHabilitada;

    public float puntoDelMedio = 0.5f;
    public UnityEvent alMitad = new UnityEvent();
    public UnityEvent alAbrir = new UnityEvent();

    public bool Abierta { get; private set; }

    float ultimoValor;
    bool avisoMitad;
    float siseoHasta;

    void Awake()
    {
        if (siseo != null && siseo.clip == null) siseo.clip = SonidoSintetico.Ruido(0.15f);
        if (volante != null)
        {
            volante.onValueChange.AddListener(AlGirar);
            volante.enabled = false;   // bloqueada hasta que ande la campana
        }
        if (tarjetaHabilitada != null) tarjetaHabilitada.SetActive(false);
        MostrarPresion(0f);
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
        MostrarPresion(valor);

        // Mientras la manija se mueve, suena el gas
        if (Mathf.Abs(valor - ultimoValor) > 0.001f) siseoHasta = Time.time + 0.25f;
        ultimoValor = valor;

        if (!avisoMitad && valor >= puntoDelMedio)
        {
            avisoMitad = true;
            alMitad.Invoke();
        }
        if (valor < 0.98f) return;

        // Abierta del todo: queda así (la manija ya no se mueve)
        Abierta = true;
        volante.enabled = false;
        MostrarPresion(1f);
        if (siseo != null) siseo.Stop();
        SonidoSintetico.Tocar(SonidoSintetico.Golpe(), transform.position, 0.6f);
        alAbrir.Invoke();
    }

    void MostrarPresion(float valor)
    {
        if (presion != null) presion.text = (valor * presionMaxima).ToString("0.0") + " kPa";
    }

    void Update()
    {
        if (siseo == null || Abierta) return;
        bool girando = Time.time < siseoHasta;
        if (girando && !siseo.isPlaying) siseo.Play();
        if (!girando && siseo.isPlaying) siseo.Stop();
    }
}
