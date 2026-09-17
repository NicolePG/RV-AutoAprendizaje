using UnityEngine;

// Muestra u oculta el cartel de pistas al tocar la hoja del escritorio.
// Se conecta desde el evento del XRSimpleInteractable de la hoja.
public class MostrarPistas : MonoBehaviour
{
    [Tooltip("El cartel con las pistas que aparece sobre el escritorio")]
    public GameObject panel;

    public void Alternar()
    {
        if (panel != null) panel.SetActive(!panel.activeSelf);
    }
}
