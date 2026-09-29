using UnityEngine;

// Hace que los tubos de una lámpara brillen al mismo ritmo que su luz.
// Parpadeo sube y baja la intensidad de la luz, pero el tubo (un material que brilla solo)
// quedaría prendido fijo: este script copia ese parpadeo al brillo del tubo.
//
// En la escena: va en la luz, junto a Parpadeo. En "piezas" van los renderers de los tubos,
// que tienen que usar un material con emisión. ConstructorCuarto3 lo arma solo.
[RequireComponent(typeof(Light))]
public class BrilloSegunLuz : MonoBehaviour
{
    [Tooltip("Renderers que brillan junto con la luz (los tubos)")]
    public Renderer[] piezas;

    [Tooltip("Brillo de los tubos cuando la luz está al máximo")]
    public Color brillo = new Color(1.3f, 1.35f, 1.4f);

    [Tooltip("Intensidad de la luz que corresponde al brillo máximo")]
    public float intensidadMaxima = 1.8f;

    Light luz;
    MaterialPropertyBlock bloque;

    void Awake()
    {
        luz = GetComponent<Light>();
        bloque = new MaterialPropertyBlock();
    }

    // LateUpdate: después de que Parpadeo cambió la luz en este cuadro
    void LateUpdate()
    {
        float nivel = Mathf.Clamp01(luz.intensity / intensidadMaxima);
        bloque.SetColor("_EmissionColor", brillo * nivel);
        foreach (Renderer pieza in piezas)
            if (pieza != null) pieza.SetPropertyBlock(bloque);
    }
}
