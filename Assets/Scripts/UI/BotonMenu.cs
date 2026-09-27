using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Botón de los menús (pausa y tiempo agotado). Se apunta con el rayo del control y se aprieta el
// botón de agarre, igual que los botones de los cuartos.
//  - Con el rayo encima: se aclara y crece apenas.
//  - Al apretarlo: hace clic, se hunde un instante y dispara "alPresionar".
//  - Deshabilitado (por ejemplo "Cargar partida" sin partida guardada): queda apagado y al
//    apretarlo suena un zumbido corto, sin hacer nada.
// Funciona con el juego en pausa: se anima con el tiempo real y su sonido no se pausa
// (el AudioSource de los menús ignora la pausa del audio).
//
// En la escena: va en la raíz del botón, junto a un XRSimpleInteractable y un BoxCollider.
// "vista" es el hijo con el fondo y los textos (lo que se anima). ConstructorSistema lo arma.
[RequireComponent(typeof(XRSimpleInteractable))]
public class BotonMenu : MonoBehaviour
{
    public Transform vista;
    public Renderer fondo;
    public TMP_Text texto;

    [Tooltip("Segunda línea, más chica (opcional)")]
    public TMP_Text detalle;

    [Tooltip("Sonido de los menús: un AudioSource 2D que no se pausa con el juego")]
    public AudioSource audioMenu;

    public UnityEvent alPresionar = new UnityEvent();

    public bool Habilitado { get; private set; } = true;

    XRSimpleInteractable interactable;
    MaterialPropertyBlock bloque;
    Color colorBase, colorTexto, colorDetalle;
    Color? colorForzado;
    string textoOriginal, detalleOriginal;
    float encimaAvance, pulso;
    bool preparado;

    void Awake() => Preparar();

    // Anota cómo es el botón en la escena (colores y textos) antes de que nadie lo cambie. Se
    // llama también desde los métodos públicos: el menú puede cambiar los textos mientras el botón
    // todavía está apagado y su Awake no corrió.
    void Preparar()
    {
        if (preparado) return;
        preparado = true;
        interactable = GetComponent<XRSimpleInteractable>();
        bloque = new MaterialPropertyBlock();
        if (fondo != null) colorBase = fondo.sharedMaterial.GetColor("_BaseColor");
        if (texto != null) { colorTexto = texto.color; textoOriginal = texto.text; }
        if (detalle != null) { colorDetalle = detalle.color; detalleOriginal = detalle.text; }
        if (audioMenu != null) audioMenu.ignoreListenerPause = true;
    }

    void OnEnable() => interactable.selectEntered.AddListener(Presionar);

    void OnDisable()
    {
        interactable.selectEntered.RemoveListener(Presionar);
        encimaAvance = 0f;
        pulso = 0f;
        if (vista != null) vista.localScale = Vector3.one;
    }

    public void Habilitar(bool habilitado) => Habilitado = habilitado;

    // Otro color mientras tanto (el "¿seguro?" en rojo). Con null vuelve al suyo.
    public void Colorear(Color? color) => colorForzado = color;

    // Cambia los textos. Con null, esa línea vuelve a ser la que tenía en la escena.
    public void Textos(string principal, string segunda = null)
    {
        Preparar();
        if (texto != null) texto.text = principal ?? textoOriginal ?? "";
        if (detalle != null) detalle.text = segunda ?? detalleOriginal ?? "";
    }

    void Presionar(SelectEnterEventArgs args)
    {
        if (!Habilitado)
        {
            Sonar(SonidoSintetico.Zumbido(170f, 0.12f), 0.45f);
            return;
        }
        pulso = 1f;
        Sonar(SonidoSintetico.Pitido(1760f, 0.035f), 0.6f);
        GameManager.Vibrar(0.3f, 0.05f);
        alPresionar.Invoke();
    }

    void Sonar(AudioClip clip, float volumen)
    {
        if (audioMenu != null) audioMenu.PlayOneShot(clip, volumen);
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        bool encima = Habilitado && interactable.isHovered;
        encimaAvance = Mathf.MoveTowards(encimaAvance, encima ? 1f : 0f, dt / 0.12f);
        pulso = Mathf.MoveTowards(pulso, 0f, dt / 0.15f);

        if (vista != null)
            vista.localScale = Vector3.one * (1f + 0.035f * encimaAvance - 0.06f * pulso);

        // Color del fondo: el suyo (o el forzado), más claro con el rayo encima, apagado si no anda
        Color color = colorForzado ?? colorBase;
        if (!Habilitado) color = Color.Lerp(color, new Color(0.16f, 0.19f, 0.24f), 0.75f);
        else color = Color.Lerp(color, Color.white, 0.2f * encimaAvance);
        if (fondo != null)
        {
            fondo.GetPropertyBlock(bloque);
            bloque.SetColor("_BaseColor", color);
            fondo.SetPropertyBlock(bloque);
        }

        float alfa = Habilitado ? 1f : 0.4f;
        if (texto != null) texto.color = new Color(colorTexto.r, colorTexto.g, colorTexto.b, alfa);
        if (detalle != null) detalle.color = new Color(colorDetalle.r, colorDetalle.g, colorDetalle.b, alfa * colorDetalle.a);
    }
}
