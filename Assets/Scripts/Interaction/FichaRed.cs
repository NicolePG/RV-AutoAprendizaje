using UnityEngine;

// La ficha (conector RJ45) de un cable de red. El cable sale de la torre de una computadora
// (lo dibuja CableVisual) y la ficha se agarra con la mano y se enchufa en una toma de la
// pared (TomaDeRed). Esa computadora tiene red solo si SU ficha está en la toma que le
// corresponde según el mapa de la red (lo revisa RedSala).
//
// En la escena: va en la ficha, junto a su XRGrabInteractable. ConstructorCuarto3 la arma sola.
public class FichaRed : MonoBehaviour
{
    [Tooltip("La computadora de la que sale este cable")]
    public ComputadoraSala computadora;

    [Tooltip("La toma donde tiene que ir, según el mapa de la red")]
    public TomaDeRed tomaCorrecta;
}
