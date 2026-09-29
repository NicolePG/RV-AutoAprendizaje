using Unity.XR.CoreUtils;
using UnityEngine;

// Límites del jugador: su cabeza no atraviesa paredes ni muebles, y no se agacha hasta el piso ni "flota".
//
// En VR la cámara es la cabeza del jugador real: si camina contra una pared del juego (o en el simulador
// usa WASD, Q y E), nada la detiene. Este script:
//  1. Mantiene la altura de los ojos entre alturaMinima y alturaMaxima, subiendo o bajando el
//     "Camera Offset" del XR Origin (el objeto que decide a qué altura está la cámara).
//     Si "agachado" está activo, baja la cabeza (lo usa ControlesDePC con CTRL o C).
//  2. Si la cabeza toca algo fijo del escenario, corre todo el XR Origin hacia atrás para que la cabeza
//     vuelva a su último lugar libre. Solo en horizontal: la altura la controla el paso 1.
//
// En la escena: va en el XR Origin (XR Rig). Cuarto1Builder lo agrega solo.
[RequireComponent(typeof(XROrigin))]
public class LimitesDelJugador : MonoBehaviour
{
    [Tooltip("Radio de la cabeza en metros: la cámara no se acerca más que esto a una pared")]
    public float radioCabeza = 0.15f;

    [Tooltip("Altura mínima de los ojos sobre el piso (agachado)")]
    public float alturaMinima = 0.8f;

    [Tooltip("Altura máxima de los ojos sobre el piso")]
    public float alturaMaxima = 1.85f;

    [Header("Agacharse (en el PC lo cambia la tecla CTRL o C)")]
    [Tooltip("true = agachado: la cámara baja 'bajadaAlAgacharse' metros (sin pasar de alturaMinima)")]
    public bool agachado;

    [Tooltip("Cuántos metros baja la cabeza al agacharse")]
    public float bajadaAlAgacharse = 0.55f;

    [Tooltip("Velocidad con la que baja o sube la cabeza, en metros por segundo")]
    public float velocidadAgacharse = 2.5f;

    float bajadaActual;          // va de 0 a bajadaAlAgacharse poco a poco, para que no sea un salto brusco

    XROrigin origen;
    Transform camara;
    Transform offsetCamara;      // "Camera Offset": sube o baja la cámara
    float correccionAltura;      // cuánto subió (+) o bajó (-) este script la cámara en el último cuadro
    float ultimoOffsetEscrito;   // altura que este script le dejó al Camera Offset en el último cuadro
    Vector3 ultimaCabezaLibre;   // último lugar donde la cabeza no tocaba nada
    Vector3 ultimaPosicionOrigen;
    readonly Collider[] cercanos = new Collider[16];

    void Start()
    {
        origen = GetComponent<XROrigin>();
        camara = origen.Camera.transform;
        offsetCamara = origen.CameraFloorOffsetObject.transform;
        ultimaCabezaLibre = camara.position;
        ultimaPosicionOrigen = transform.position;
        ultimoOffsetEscrito = offsetCamara.localPosition.y;
    }

    // LateUpdate: después de que el visor (o el simulador) movió la cámara en este cuadro
    void LateUpdate()
    {
        LimitarAltura();
        LimitarParedes();
    }

    void LimitarAltura()
    {
        // Al agacharse o levantarse, la cabeza baja o sube de a poco
        float bajadaObjetivo = agachado ? bajadaAlAgacharse : 0f;
        bajadaActual = Mathf.MoveTowards(bajadaActual, bajadaObjetivo, velocidadAgacharse * Time.deltaTime);

        // Si el XR Origin reacomodó el Camera Offset por su cuenta (pasa al iniciar el visor o al recentrar),
        // la corrección anterior ya no está aplicada: se empieza de cero desde la altura nueva
        if (!Mathf.Approximately(offsetCamara.localPosition.y, ultimoOffsetEscrito))
            correccionAltura = 0f;

        // Altura real de los ojos sobre el piso (el piso es la altura del XR Origin), sin la corrección
        // que este script aplicó en el cuadro anterior. Se mide en el mundo, así funciona igual con
        // el Quest o con el simulador, sin importar cómo configure el XR Origin la altura de la cámara.
        float alturaSinCorreccion = (camara.position.y - transform.position.y) - correccionAltura;
        float alturaPermitida = Mathf.Clamp(alturaSinCorreccion - bajadaActual, alturaMinima, alturaMaxima);
        float nuevaCorreccion = alturaPermitida - alturaSinCorreccion;

        // Se sube o baja el Camera Offset solo la diferencia con la corrección anterior
        Vector3 p = offsetCamara.localPosition;
        p.y += nuevaCorreccion - correccionAltura;
        offsetCamara.localPosition = p;
        correccionAltura = nuevaCorreccion;
        ultimoOffsetEscrito = p.y;
    }

    void LimitarParedes()
    {
        // Si otro script movió el XR Origin desde el cuadro anterior (un teletransporte o un giro),
        // el lugar nuevo es válido aunque la cabeza quede cerca de una pared
        if (transform.position != ultimaPosicionOrigen)
            ultimaCabezaLibre = camara.position;

        if (CabezaTocaEscenario(camara.position))
        {
            // Se corre el XR Origin lo justo para que la cabeza vuelva a su último lugar libre
            Vector3 correccion = ultimaCabezaLibre - camara.position;
            correccion.y = 0;
            transform.position += correccion;
        }
        else
        {
            ultimaCabezaLibre = camara.position;
        }
        ultimaPosicionOrigen = transform.position;
    }

    // true si la cabeza toca algo fijo: paredes, techo, mostrador, muebles.
    // No cuenta lo que se mueve (objetos con Rigidbody, como la llave o los cajones) ni el propio jugador.
    bool CabezaTocaEscenario(Vector3 cabeza)
    {
        int cantidad = Physics.OverlapSphereNonAlloc(cabeza, radioCabeza, cercanos, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < cantidad; i++)
        {
            Collider c = cercanos[i];
            if (c.attachedRigidbody == null && !c.transform.IsChildOf(transform)) return true;
        }
        return false;
    }
}
