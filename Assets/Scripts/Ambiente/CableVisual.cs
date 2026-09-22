using UnityEngine;

// Dibuja un cable, como un cable de verdad. En el Cuarto 3 une la torre de cada computadora con
// su ficha de red: así se ve de dónde sale cada cable y que la red llega por el cable (no es
// inalámbrica). Sigue a la ficha a donde se la lleve. No es físico: no choca con nada, solo se dibuja.
//
// Recorrido del cable:
//  - Sin borde: de la torre a la ficha, con una caída suave en el medio.
//  - Con borde (bordeDesde y bordeHasta): cruza la mesada de la torre al borde de adelante, va
//    apoyado a lo largo del borde hasta el punto más cercano a la ficha y de ahí cuelga hasta la
//    ficha. Es lo que pasa al tirar de un cable que está sobre una mesa, y así el dibujo nunca
//    atraviesa los monitores de los otros puestos.
//
// En la escena: va en la ficha, junto a un LineRenderer. En "inicio" va el punto de la torre de
// donde sale el cable y en "fin" la parte de atrás de la ficha. ConstructorCuarto3 lo arma solo.
[RequireComponent(typeof(LineRenderer))]
public class CableVisual : MonoBehaviour
{
    [Tooltip("De dónde sale el cable (la torre)")]
    public Transform inicio;

    [Tooltip("Dónde termina (la parte de atrás de la ficha)")]
    public Transform fin;

    [Tooltip("Opcional: el borde de la mesada por donde el cable va apoyado. Desde = frente de su puesto")]
    public Transform bordeDesde;

    [Tooltip("Opcional: hasta dónde llega el borde (la punta de la mesada)")]
    public Transform bordeHasta;

    [Tooltip("Cantidad de puntos del tramo que cuelga: más = curva más suave")]
    public int puntos = 18;

    [Tooltip("Cuánto cuelga el cable por cada metro de distancia entre las puntas")]
    public float caidaPorMetro = 0.07f;

    [Tooltip("Altura del piso en el mundo: el cable nunca baja de ahí")]
    public float alturaPiso;

    LineRenderer linea;

    void Awake()
    {
        linea = GetComponent<LineRenderer>();
        linea.useWorldSpace = true;
    }

    // LateUpdate: después de que la mano (o la toma) movió la ficha en este cuadro
    void LateUpdate()
    {
        if (inicio == null || fin == null) return;

        bool conBorde = bordeDesde != null && bordeHasta != null;
        linea.positionCount = puntos + (conBorde ? 2 : 0);
        int n = 0;
        Vector3 desde = inicio.position;

        if (conBorde)
        {
            // Tramo apoyado: de la torre al borde, y por el borde hasta el punto más cercano a la ficha
            Vector3 a = bordeDesde.position, eje = bordeHasta.position - a;
            float t = eje.sqrMagnitude > 0f ? Mathf.Clamp01(Vector3.Dot(fin.position - a, eje) / eje.sqrMagnitude) : 0f;
            linea.SetPosition(n++, inicio.position);
            linea.SetPosition(n++, a);
            desde = a + eje * t;
        }

        // Tramo que cuelga hasta la ficha: una parábola, cae más en el medio
        Vector3 b = fin.position;
        float caida = Mathf.Min(0.3f, caidaPorMetro * Vector3.Distance(desde, b));
        for (int i = 0; i < puntos; i++)
        {
            float t = i / (puntos - 1f);
            Vector3 p = Vector3.Lerp(desde, b, t);
            p.y -= caida * 4f * t * (1f - t);
            p.y = Mathf.Max(p.y, alturaPiso + 0.005f);
            linea.SetPosition(n++, p);
        }
    }
}
