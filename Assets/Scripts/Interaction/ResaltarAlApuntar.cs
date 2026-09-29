using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// Resalta un objeto con un tono ámbar mientras la mano o el rayo lo apuntan, para que el jugador sepa
// que puede usarlo (feedback del CLAUDE.md: "objeto agarrable: resaltado al acercar la mano").
// Sirve para cualquier cosa interactiva: objetos, cajones, botones y palancas.
//
// No cambia los materiales (que comparten muchos objetos): usa un MaterialPropertyBlock, que cambia el
// color solo en este objeto y solo mientras está apuntado.
//
// En la escena: va junto al XRGrabInteractable o XRSimpleInteractable del objeto. Cuarto1Builder lo agrega solo.
[RequireComponent(typeof(XRBaseInteractable))]
public class ResaltarAlApuntar : MonoBehaviour
{
    public Color colorResaltado = new Color(1f, 0.72f, 0.25f);

    [Range(0f, 1f)]
    [Tooltip("Cuánto se mezcla el color de resaltado con el color original")]
    public float intensidad = 0.4f;

    // Nombres del color principal en los materiales de URP y en los de los modelos de Poly Haven (glTFast)
    static readonly string[] PropiedadesDeColor = { "_BaseColor", "baseColorFactor" };

    XRBaseInteractable interactable;
    Renderer[] partes;
    MaterialPropertyBlock bloque;

    void Awake()
    {
        interactable = GetComponent<XRBaseInteractable>();
        bloque = new MaterialPropertyBlock();

        // Solo las partes de este objeto: si es un cajón, no se resalta lo que tiene adentro
        var lista = new System.Collections.Generic.List<Renderer>();
        foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
            if (r.GetComponentInParent<XRBaseInteractable>() == interactable) lista.Add(r);
        partes = lista.ToArray();
    }

    void OnEnable()
    {
        interactable.hoverEntered.AddListener(AlApuntar);
        interactable.hoverExited.AddListener(AlDejarDeApuntar);
    }

    void OnDisable()
    {
        interactable.hoverEntered.RemoveListener(AlApuntar);
        interactable.hoverExited.RemoveListener(AlDejarDeApuntar);
        Resaltar(false);
    }

    void AlApuntar(HoverEnterEventArgs args)
    {
        if (ApuntadoPorUnaMano()) Resaltar(true);
    }

    void AlDejarDeApuntar(HoverExitEventArgs args)
    {
        // Puede estar apuntado por las dos manos: se apaga solo cuando ninguna lo apunta
        if (!ApuntadoPorUnaMano()) Resaltar(false);
    }

    // Los encajes (sockets) también "apuntan" a lo que tienen cerca: esos no cuentan, solo las manos
    bool ApuntadoPorUnaMano()
    {
        foreach (var quien in interactable.interactorsHovering)
            if (!(quien is XRSocketInteractor)) return true;
        return false;
    }

    void Resaltar(bool encendido)
    {
        foreach (Renderer parte in partes)
        {
            if (parte == null) continue;
            if (!encendido)
            {
                parte.SetPropertyBlock(null);
                continue;
            }

            Material material = parte.sharedMaterial;
            if (material == null) continue;
            bloque.Clear();
            bool tieneColor = false;
            foreach (string propiedad in PropiedadesDeColor)
            {
                if (!material.HasProperty(propiedad)) continue;
                bloque.SetColor(propiedad, Color.Lerp(material.GetColor(propiedad), colorResaltado, intensidad));
                tieneColor = true;
            }
            if (tieneColor) parte.SetPropertyBlock(bloque);
        }
    }
}
