using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Manecilla de reloj que se gira con la mano para poner el reloj en hora.
// Mientras el jugador la sostiene, la manecilla sigue a la mano girando sobre el centro
// del reloj. Al soltarla se acomoda sola a la hora más cercana (cada hora son 30 grados).
// Si esa hora es la correcta, avisa una sola vez con "alPonerEnHora".
//
// En la escena: va en el pivote de la manecilla (objeto vacío en el centro del reloj,
// con la aguja como hijo), junto a un XRSimpleInteractable.
[RequireComponent(typeof(XRSimpleInteractable))]
public class RelojManecilla : MonoBehaviour
{
    [Tooltip("Hora que hay que marcar para resolverlo, de 1 a 12")]
    public int horaObjetivo = 9;

    [Tooltip("Sonido del clic al acomodarse en una hora (opcional)")]
    public AudioClip sonidoClic;

    [Tooltip("Qué pasa cuando la manecilla queda en la hora correcta")]
    public UnityEvent alPonerEnHora = new UnityEvent();

    public bool EnHora { get; private set; }

    XRSimpleInteractable interactable;

    void Awake() => interactable = GetComponent<XRSimpleInteractable>();

    void OnEnable() => interactable.selectExited.AddListener(AlSoltar);
    void OnDisable() => interactable.selectExited.RemoveListener(AlSoltar);

    void Update()
    {
        if (EnHora || !interactable.isSelected) return;

        // Dónde está la mano, medido sobre la cara del reloj
        Vector3 mano = interactable.interactorsSelecting[0].GetAttachTransform(interactable).position;
        Vector3 local = transform.parent.InverseTransformPoint(mano);
        float grados = Mathf.Atan2(local.x, local.y) * Mathf.Rad2Deg;
        transform.localRotation = Quaternion.Euler(0f, 0f, -grados);
    }

    void AlSoltar(SelectExitEventArgs args)
    {
        if (EnHora) return;

        // Se acomoda a la hora más cercana
        int hora = Mathf.RoundToInt(-transform.localEulerAngles.z / 30f);
        hora = ((hora % 12) + 12) % 12;
        if (hora == 0) hora = 12;

        transform.localRotation = Quaternion.Euler(0f, 0f, -hora * 30f);
        if (sonidoClic != null) AudioSource.PlayClipAtPoint(sonidoClic, transform.position);

        if (hora != horaObjetivo) return;

        EnHora = true;
        alPonerEnHora.Invoke();
    }
}
