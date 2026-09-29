using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

// Cómo se mueve y gira el jugador con los joysticks.
//  - Moverse: teletransporte (lo que pide la consigna, porque no marea). Con el joystick hacia
//    adelante sale el arco, se apunta al piso y al soltar se aparece ahí.
//  - Girar: joystick a los costados. El giro es continuo y fluido ("velocidadDeGiro" grados por
//    segundo), no de a saltos de 45°. Tampoco hay media vuelta: el XR Interaction Toolkit trae
//    activado que el joystick hacia atrás gire 180° de golpe, y el jugador quedaba mirando la
//    pared de enfrente como si se hubiera teletransportado.
//  - Desde Opciones del menú de inicio se puede activar "Caminar con joystick": el joystick
//    izquierdo camina de forma continua (hacia donde mira la cabeza) y, mientras camina, los
//    bordes de la vista se oscurecen (la viñeta de confort del XR Interaction Toolkit) para que
//    maree menos. El derecho sigue teletransportando y girando.
// La elección de caminar se guarda en el visor (PlayerPrefs). Con un menú abierto no se camina ni
// se gira (Bloquear): si no, el jugador podría girar y dejar el menú fuera de la vista.
//
// En la escena: va en el XR Origin; "manoIzquierda" y "manoDerecha" son los
// ControllerInputActionManager de cada control (los del XR Origin de los Starter Assets).
// ConstructorSistema lo agrega y los conecta; si falta alguno, lo busca solo al empezar.
public class ModoDeMovimiento : MonoBehaviour
{
    const string CLAVE = "caminarConJoystick";

    public static ModoDeMovimiento Instancia { get; private set; }

    [Tooltip("El ControllerInputActionManager del control izquierdo")]
    public ControllerInputActionManager manoIzquierda;

    [Tooltip("El ControllerInputActionManager del control derecho")]
    public ControllerInputActionManager manoDerecha;

    [Tooltip("Grados por segundo al girar con el joystick (90 = un cuarto de vuelta por segundo)")]
    public float velocidadDeGiro = 90f;

    // Lo que eligió el jugador (se lee del visor): por defecto, no
    public static bool CaminarConJoystick => PlayerPrefs.GetInt(CLAVE, 0) == 1;

    bool bloqueado;
    ContinuousTurnProvider[] girosContinuos;
    SnapTurnProvider[] girosDeASaltos;

    void Awake()
    {
        Instancia = this;
        if (manoIzquierda == null || manoDerecha == null) BuscarManos();

        // Los dos giros del XR Origin: el continuo con su velocidad y ninguno con media vuelta
        girosContinuos = GetComponentsInChildren<ContinuousTurnProvider>(true);
        girosDeASaltos = GetComponentsInChildren<SnapTurnProvider>(true);
        foreach (var giro in girosContinuos)
        {
            giro.turnSpeed = velocidadDeGiro;
            giro.enableTurnAround = false;
        }
        foreach (var giro in girosDeASaltos) giro.enableTurnAround = false;

        Aplicar();
    }

    void OnDestroy()
    {
        if (Instancia == this) Instancia = null;
    }

    // Los controles del XR Origin se llaman "Left Controller" y "Right Controller"
    void BuscarManos()
    {
        foreach (var mano in GetComponentsInChildren<ControllerInputActionManager>(true))
        {
            if (manoIzquierda == null && mano.name.Contains("Left")) manoIzquierda = mano;
            if (manoDerecha == null && mano.name.Contains("Right")) manoDerecha = mano;
        }
    }

    // Lo llama el botón de Opciones: prende o apaga caminar con el joystick y lo guarda
    public void Alternar()
    {
        PlayerPrefs.SetInt(CLAVE, CaminarConJoystick ? 0 : 1);
        PlayerPrefs.Save();
        Aplicar();
    }

    // Lo llama GameManager: con el menú de inicio, la pausa o el tiempo agotado no se camina ni se gira
    public void Bloquear(bool bloquear)
    {
        bloqueado = bloquear;
        Aplicar();
    }

    void Aplicar()
    {
        // El joystick izquierdo camina solo si el jugador lo eligió; si no, teletransporta
        if (manoIzquierda != null)
        {
            manoIzquierda.smoothMotionEnabled = CaminarConJoystick && !bloqueado;
            manoIzquierda.smoothTurnEnabled = true;
        }
        // Los dos joysticks giran de forma continua (y no de a saltos)
        if (manoDerecha != null) manoDerecha.smoothTurnEnabled = true;

        // Con un menú abierto no se gira
        if (girosContinuos != null)
            foreach (var giro in girosContinuos) if (giro != null) giro.enabled = !bloqueado;
        if (girosDeASaltos != null)
            foreach (var giro in girosDeASaltos) if (giro != null) giro.enabled = !bloqueado;
    }
}
