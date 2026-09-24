using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Un fusible de cartucho del laboratorio (Cuarto 4, acertijo 1).
//
// Se agarra con la mano como las herramientas del Cuarto 1 y se encaja en un portafusibles
// del tablero (PortaFusible). Cada uno tiene su amperaje impreso y su color normalizado
// (6 A verde, 10 A rojo, 16 A gris...). Si se pone en el circuito equivocado, el portafusibles
// avisa con luz roja; se saca y se puede volver a probar en otro (el fusible no se arruina).
//
// En la escena: va en el fusible, junto a su XRGrabInteractable. ConstructorCuarto4 lo arma.
[RequireComponent(typeof(XRGrabInteractable))]
public class FusibleLab : MonoBehaviour
{
    [Tooltip("Cuántos amperes aguanta (lo que dice impreso)")]
    public int amperaje = 10;
}
