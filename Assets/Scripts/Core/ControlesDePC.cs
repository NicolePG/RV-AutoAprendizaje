using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

// Controles para probar el juego en el PC con el simulador de XR:
//  - CTRL (o C): agacharse. Se presiona otra vez para levantarse.
//    Ojo: CTRL + S es "guardar escena" en Unity; como agacharse se activa con un solo toque,
//    no hace falta mantener CTRL mientras se camina hacia atrás.
//  - SHIFT (mantenido): correr. WASD mueve más rápido.
// (Las herramientas, llave y destornillador, se agarran con un clic: eso lo hace HerramientaEnMano.)
//
// En el Quest no hay teclado, así que este script no hace nada: ahí el jugador se agacha de verdad
// y se mueve con teletransporte (el GDD pide no usar movimiento continuo).
//
// Ojo: en el simulador SHIFT también cambia G y T a la mano izquierda. Para agarrar con la mano
// derecha hay que soltar SHIFT.
//
// En la escena: va en el XR Origin (XR Rig), junto a LimitesDelJugador. Cuarto1Builder lo agrega solo.
[RequireComponent(typeof(LimitesDelJugador))]
public class ControlesDePC : MonoBehaviour
{
    [Tooltip("Cuántas veces más rápido se mueve al correr")]
    public float multiplicadorCorrer = 2f;

    LimitesDelJugador limites;
    XRInteractionSimulator simulador;
    float velocidadNormal;

    void Awake()
    {
        limites = GetComponent<LimitesDelJugador>();
    }

    void Update()
    {
        Keyboard teclado = Keyboard.current;
        if (teclado == null) return; // en el Quest no hay teclado

        // CTRL o C cambia entre agachado y de pie
        if (teclado.leftCtrlKey.wasPressedThisFrame || teclado.rightCtrlKey.wasPressedThisFrame || teclado.cKey.wasPressedThisFrame)
            limites.agachado = !limites.agachado;

        // El simulador aparece solo al dar Play: se busca hasta encontrarlo y se guarda su velocidad normal
        if (simulador == null)
        {
            simulador = FindAnyObjectByType<XRInteractionSimulator>();
            if (simulador == null) return;
            velocidadNormal = simulador.bodyTranslateMultiplier;
        }

        bool corriendo = teclado.leftShiftKey.isPressed || teclado.rightShiftKey.isPressed;
        simulador.bodyTranslateMultiplier = corriendo ? velocidadNormal * multiplicadorCorrer : velocidadNormal;
    }
}
