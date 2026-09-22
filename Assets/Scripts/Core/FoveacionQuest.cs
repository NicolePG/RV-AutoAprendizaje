using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

// Optimización para el Quest: renderizado foveado.
//
// El ojo solo ve nítido el centro de la vista; en los bordes de las lentes del visor la imagen
// ya se ve borrosa. El renderizado foveado aprovecha eso: dibuja los bordes con menos detalle
// y el centro con todo el detalle. En el Quest ahorra mucho trabajo a la placa de video y casi
// no se nota. (Necesita la función "Foveated Rendering" activada en XR Plug-in Management >
// OpenXR, para Android.)
//
// En el PC con el simulador no hay visor: el script no hace nada y se apaga solo.
//
// En la escena: va en el objeto "Optimizacion" (lo crea el menú Escape Room > Optimizar escena).
public class FoveacionQuest : MonoBehaviour
{
    [Tooltip("Cuánto se baja el detalle en los bordes: 0 = nada, 1 = lo máximo")]
    [Range(0f, 1f)]
    public float nivel = 0.5f;

    readonly List<XRDisplaySubsystem> pantallas = new List<XRDisplaySubsystem>();

    // El visor puede tardar unos cuadros en arrancar: se prueba hasta encontrarlo
    void Update()
    {
        SubsystemManager.GetSubsystems(pantallas);
        foreach (XRDisplaySubsystem pantalla in pantallas)
        {
            if (!pantalla.running) continue;
            pantalla.foveatedRenderingLevel = nivel;
            pantalla.foveatedRenderingFlags = XRDisplaySubsystem.FoveatedRenderingFlags.GazeAllowed;   // en el Quest Pro sigue la mirada
            enabled = false;   // listo: ya no hace falta revisar
            return;
        }
        if (Time.time > 10f) enabled = false;   // sin visor (PC con simulador): no hay nada que hacer
    }
}
