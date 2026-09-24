using UnityEngine;

// Una tarjeta de acceso (credencial con chip), como las de cualquier edificio moderno.
// Solo dice qué tarjeta es: su ItemData (el asset con el nombre y el tipo). El que decide si
// abre o no es el lector (LectorTarjeta), comparando ese asset con el que él acepta.
//
// Por qué un ItemData y no un texto: el lector pide "este asset" y no "una tarjeta que diga
// DOCENTE". Así una tarjeta nueva se crea como asset, sin tocar código.
//
// En la escena: va en la tarjeta, junto a su XRGrabInteractable. ConstructorCuarto4 la arma.
public class TarjetaAcceso : MonoBehaviour
{
    [Tooltip("Qué tarjeta es (asset en ScriptableObjects/Items)")]
    public ItemData datos;
}
