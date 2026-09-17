using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Contorno resaltado al acercar la mano a un objeto interactivo.
// Mientras la mano está en rango (hover, sin agarrar todavía): se cambia el material
// del renderer por "materialResaltado". Al alejar la mano: vuelve al material original.
//
// En la escena: va en cualquier objeto interactivo, junto a un XRBaseInteractable
// (XRSimpleInteractable o XRGrabInteractable) y un Renderer (o MeshRenderer hijo).
// En el Inspector se arrastra el "materialResaltado" (un material del color del cuarto,
// según la sección 4 del documento de diseño).
[RequireComponent(typeof(XRBaseInteractable))]
public class HoverHighlight : MonoBehaviour
{
    [Tooltip("Renderer a resaltar. Si se deja vacío, se busca uno en este objeto o sus hijos")]
    public Renderer renderer_;

    [Tooltip("Material que se muestra mientras la mano está cerca")]
    public Material materialResaltado;

    XRBaseInteractable interactable;
    Material materialOriginal;

    void Awake()
    {
        interactable = GetComponent<XRBaseInteractable>();
        if (renderer_ == null) renderer_ = GetComponentInChildren<Renderer>();
        if (renderer_ != null) materialOriginal = renderer_.sharedMaterial;
    }

    void OnEnable()
    {
        interactable.hoverEntered.AddListener(AlAcercarMano);
        interactable.hoverExited.AddListener(AlAlejarMano);
    }

    void OnDisable()
    {
        interactable.hoverEntered.RemoveListener(AlAcercarMano);
        interactable.hoverExited.RemoveListener(AlAlejarMano);
    }

    void AlAcercarMano(HoverEnterEventArgs args)
    {
        if (renderer_ != null && materialResaltado != null) renderer_.sharedMaterial = materialResaltado;
    }

    void AlAlejarMano(HoverExitEventArgs args)
    {
        if (renderer_ != null) renderer_.sharedMaterial = materialOriginal;
    }
}
