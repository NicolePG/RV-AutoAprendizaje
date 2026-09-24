using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Un fusible de cartucho del laboratorio (Cuarto 4, acertijo 1).
//
// Se agarra con la mano como las herramientas del Cuarto 1 y se encaja en un portafusibles
// del tablero (PortaFusible). Cada uno tiene su amperaje impreso y su color normalizado
// (6 A verde, 10 A rojo, 16 A gris...). Si se pone en un circuito que consume más de lo
// que aguanta, se quema: queda negro y ya no sirve (el portafusibles no lo vuelve a aceptar).
//
// En la escena: va en el fusible, junto a su XRGrabInteractable. ConstructorCuarto4 lo arma.
[RequireComponent(typeof(XRGrabInteractable))]
public class FusibleLab : MonoBehaviour
{
    [Tooltip("Cuántos amperes aguanta (lo que dice impreso)")]
    public int amperaje = 10;

    [Tooltip("Las partes que se ennegrecen al quemarse (el cuerpo de cerámica)")]
    public Renderer[] cuerpo;

    [Tooltip("Material del fusible quemado")]
    public Material quemado;

    public bool Quemado { get; private set; }

    public void Quemar()
    {
        if (Quemado) return;
        Quemado = true;
        foreach (Renderer r in cuerpo)
            if (r != null) r.sharedMaterial = quemado;
    }
}
