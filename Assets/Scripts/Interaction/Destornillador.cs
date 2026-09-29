using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Mide cuánto "gira" el jugador el destornillador mientras lo tiene en la mano, como con uno real:
//  - En el Quest: girando la muñeca (el destornillador rota sobre su propio eje).
//  - En el PC: moviendo el mouse en círculos (sin el botón derecho, que en el simulador gira la vista).
// Los tornillos (Screw) usan ese giro mientras la punta los toca, y le piden al destornillador que gire
// su modelo para que se vea el movimiento.
//
// En la escena: va en el destornillador, junto a su XRGrabInteractable. La punta apunta a su +X local.
// Cuarto1Builder lo agrega solo.
[RequireComponent(typeof(XRGrabInteractable))]
public class Destornillador : MonoBehaviour
{
    [Tooltip("La pieza visible que gira al desatornillar (el modelo)")]
    public Transform visual;

    [Tooltip("Movimientos del mouse más chicos que esto (en píxeles) no cuentan: evita contar temblores")]
    public float movimientoMinimoMouse = 2f;

    [Tooltip("Cambios de dirección del mouse mayores a esto no son un círculo (es un vaivén) y no cuentan")]
    public float giroMaximoPorCuadro = 60f;

    // Grados que se giró en este cuadro (siempre positivo)
    public float GiroEsteCuadro { get; private set; }

    XRGrabInteractable agarre;
    Vector3 arribaAnterior;
    Vector2 movimientoMouseAnterior;

    void Awake()
    {
        agarre = GetComponent<XRGrabInteractable>();
        arribaAnterior = transform.up;
    }

    void Update()
    {
        GiroEsteCuadro = 0f;
        if (!agarre.isSelected)
        {
            arribaAnterior = transform.up;
            movimientoMouseAnterior = Vector2.zero;
            return;
        }

        // 1. Muñeca (Quest): cuánto rotó el "arriba" del destornillador alrededor de su propio eje (su +X)
        Vector3 eje = transform.right;
        Vector3 antes = Vector3.ProjectOnPlane(arribaAnterior, eje);
        Vector3 ahora = Vector3.ProjectOnPlane(transform.up, eje);
        if (antes.sqrMagnitude > 0.001f && ahora.sqrMagnitude > 0.001f)
            GiroEsteCuadro += Mathf.Abs(Vector3.SignedAngle(antes, ahora, eje));
        arribaAnterior = transform.up;

        // 2. Mouse en círculos (PC): cuánto cambia la dirección del movimiento del mouse de un cuadro al otro.
        //    Al dibujar un círculo la dirección da una vuelta completa: 360°.
        Mouse mouse = Mouse.current;
        if (mouse != null && !mouse.rightButton.isPressed)
        {
            Vector2 movimiento = mouse.delta.ReadValue();
            if (movimiento.magnitude >= movimientoMinimoMouse)
            {
                if (movimientoMouseAnterior != Vector2.zero)
                {
                    float cambio = Mathf.Abs(Vector2.SignedAngle(movimientoMouseAnterior, movimiento));
                    if (cambio <= giroMaximoPorCuadro) GiroEsteCuadro += cambio;
                }
                movimientoMouseAnterior = movimiento;
            }
        }
    }

    // Gira el modelo sobre el eje del destornillador (lo llama el tornillo mientras sale)
    public void GirarVisual(float grados)
    {
        if (visual != null) visual.Rotate(transform.right, -grados, Space.World);
    }
}
