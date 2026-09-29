using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

// Cómo se mueve el jugador con los joysticks.
//  - Por defecto, solo teletransporte (lo que pide la consigna, porque no marea): con cualquiera
//    de los dos joysticks hacia adelante sale el arco, se apunta al piso y al soltar se aparece
//    ahí. A los costados, gira de a 45°.
//  - Desde Opciones del menú de inicio se puede activar "Caminar con joystick": el joystick
//    izquierdo camina de forma continua (hacia donde mira la cabeza) y, mientras camina, los
//    bordes de la vista se oscurecen (la viñeta de confort del XR Interaction Toolkit) para que
//    maree menos. El derecho sigue teletransportando y girando.
// La elección se guarda en el visor (PlayerPrefs). Con un menú abierto no se camina (Bloquear).
//
// En la escena: va en el XR Origin; "manoIzquierda" es el ControllerInputActionManager del control
// izquierdo (el del XR Origin de los Starter Assets). ConstructorSistema lo agrega y lo conecta.
public class ModoDeMovimiento : MonoBehaviour
{
    const string CLAVE = "caminarConJoystick";

    public static ModoDeMovimiento Instancia { get; private set; }

    [Tooltip("El ControllerInputActionManager del control izquierdo")]
    public ControllerInputActionManager manoIzquierda;

    // Lo que eligió el jugador (se lee del visor): por defecto, no
    public static bool CaminarConJoystick => PlayerPrefs.GetInt(CLAVE, 0) == 1;

    bool bloqueado;

    void Awake()
    {
        Instancia = this;
        Aplicar();
    }

    void OnDestroy()
    {
        if (Instancia == this) Instancia = null;
    }

    // Lo llama el botón de Opciones: prende o apaga caminar con el joystick y lo guarda
    public void Alternar()
    {
        PlayerPrefs.SetInt(CLAVE, CaminarConJoystick ? 0 : 1);
        PlayerPrefs.Save();
        Aplicar();
    }

    // Lo llama GameManager: con el menú de inicio, la pausa o el tiempo agotado no se camina
    public void Bloquear(bool bloquear)
    {
        bloqueado = bloquear;
        Aplicar();
    }

    void Aplicar()
    {
        // Sin caminar, el XR Interaction Toolkit usa ese joystick para teletransportar y girar
        if (manoIzquierda != null) manoIzquierda.smoothMotionEnabled = CaminarConJoystick && !bloqueado;
    }
}
