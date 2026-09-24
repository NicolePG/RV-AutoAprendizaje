using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// El vidrio corredizo de la campana de extracción (Cuarto 4, acertijo 2).
//
// Se agarra de la manija y sigue a la mano hacia arriba o hacia abajo, como una ventana de
// guillotina. Solo se mueve en vertical, entre abierta (donde empieza) y cerrada
// ("recorrido" metros más abajo). Si se suelta, queda donde está, igual que una real.
// Se usa la altura de la mano, así funciona igual agarrándola de cerca o con el rayo.
//
// En la escena: va en el vidrio, junto a un XRSimpleInteractable y un collider. El vidrio
// tiene que empezar ABIERTO (arriba) en el editor. ConstructorCuarto4 lo arma.
[RequireComponent(typeof(XRSimpleInteractable))]
public class VentanaCampana : MonoBehaviour
{
    [Tooltip("Cuántos metros baja desde abierta hasta cerrada")]
    public float recorrido = 0.4f;

    // 0 = abierta del todo, 1 = cerrada del todo
    public float Cierre => recorrido > 0f ? Mathf.Clamp01(-desplazamiento / recorrido) : 0f;

    XRSimpleInteractable interactable;
    Vector3 posicionAbierta;
    float desplazamiento;           // metros bajados (negativo = más abajo)
    float desplazamientoAlAgarrar;
    float alturaManoAlAgarrar;

    void Awake()
    {
        interactable = GetComponent<XRSimpleInteractable>();
        posicionAbierta = transform.localPosition;
    }

    void OnEnable() => interactable.selectEntered.AddListener(AlAgarrar);
    void OnDisable() => interactable.selectEntered.RemoveListener(AlAgarrar);

    void AlAgarrar(SelectEnterEventArgs args)
    {
        desplazamientoAlAgarrar = desplazamiento;
        alturaManoAlAgarrar = args.interactorObject.GetAttachTransform(interactable).position.y;
    }

    void Update()
    {
        if (!interactable.isSelected) return;
        float alturaMano = interactable.interactorsSelecting[0].GetAttachTransform(interactable).position.y;
        float nuevo = Mathf.Clamp(desplazamientoAlAgarrar + (alturaMano - alturaManoAlAgarrar), -recorrido, 0f);
        if (Mathf.Approximately(nuevo, desplazamiento)) return;
        desplazamiento = nuevo;
        transform.localPosition = posicionAbierta + Vector3.up * desplazamiento;
    }
}
