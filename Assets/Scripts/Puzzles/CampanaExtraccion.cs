using TMPro;
using Unity.VRTemplate;
using UnityEngine;

// Acertijo 2 del Cuarto 4: la campana de extracción.
//
// En un laboratorio de verdad el gas no se abre si la campana no está extrayendo: la línea
// tiene un bloqueo de seguridad. El protocolo pegado junto a las llaves de gas lo dice:
// "bajar el vidrio de la campana y poner el extractor en velocidad 3". Hay que hacer las dos
// cosas con las manos: bajar el vidrio (VentanaCampana) y girar la perilla (XRKnob) hasta 3.
// Recién ahí se dispara "alResolverse", que destraba las llaves de gas.
//
// Sin corriente (antes del acertijo 1) la campana está apagada: el visor no muestra nada y
// el extractor no suena aunque se gire la perilla.
//
// En la escena: va en la campana. ConstructorCuarto4 la arma y la conecta.
public class CampanaExtraccion : PuzzleBase
{
    public VentanaCampana ventana;

    [Tooltip("Perilla del extractor: 0 a 3, cada punto es un cuarto de vuelta")]
    public XRKnob perilla;

    [Tooltip("Visor del panel de la campana")]
    public TMP_Text visor;

    [Tooltip("Lucecita del panel: roja = falta algo, verde = extrayendo")]
    public Renderer luz;
    public Material luzVerde, luzRoja, luzApagada;

    [Tooltip("Zumbido del extractor (en loop): sube con la velocidad")]
    public AudioSource extractor;

    [Tooltip("Luz de adentro de la campana: se prende con la corriente")]
    public Light luzInterior;

    bool energia;
    string ultimoTexto;

    void Start()
    {
        if (extractor != null && extractor.clip == null) extractor.clip = SonidoSintetico.Ruido(0.8f);
        if (luzInterior != null) luzInterior.enabled = false;
        Mostrar("", luzApagada);
    }

    // La llama el tablero eléctrico al volver la corriente
    public void DarEnergia()
    {
        energia = true;
        if (luzInterior != null) luzInterior.enabled = true;
    }

    void Update()
    {
        if (!energia) return;

        int velocidad = perilla != null ? Mathf.RoundToInt(perilla.value * 3f) : 0;
        bool cerrada = ventana != null && ventana.Cierre > 0.95f;

        // El extractor suena más fuerte y más agudo con cada punto de velocidad
        if (extractor != null)
        {
            if (velocidad > 0 && !extractor.isPlaying) extractor.Play();
            if (velocidad == 0 && extractor.isPlaying) extractor.Stop();
            extractor.volume = 0.15f + velocidad * 0.12f;
            extractor.pitch = 0.8f + velocidad * 0.15f;
        }

        if (Resuelto) return;
        if (cerrada && velocidad == 3)
        {
            Resolver();
            return;
        }
        Mostrar("VIDRIO: " + (cerrada ? "CERRADO" : "ABIERTO") + "\nEXTRACTOR: " + velocidad + " / 3", luzRoja);
    }

    protected override void MostrarAcierto()
    {
        Mostrar("EXTRACCIÓN OK\n<size=70%>GAS HABILITADO</size>", luzVerde);
        if (visor != null) visor.color = new Color(0.4f, 1f, 0.55f);
    }

    void Mostrar(string texto, Material materialLuz)
    {
        if (luz != null && materialLuz != null) luz.sharedMaterial = materialLuz;
        if (visor == null || texto == ultimoTexto) return;   // TextMeshPro solo se actualiza si cambió
        ultimoTexto = texto;
        visor.text = texto;
    }
}
