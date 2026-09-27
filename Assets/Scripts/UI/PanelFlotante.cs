using UnityEngine;

// Un panel de menú que aparece flotando delante del jugador. Lo usan el menú de pausa y la
// pantalla de tiempo agotado.
//
// Al mostrarse se ubica a "distancia" delante de la cabeza, apenas debajo de los ojos y de frente
// al jugador. Si hay una pared o un mueble más cerca (al centro o a los costados), se acerca para
// no quedar metido adentro, y se achica en la misma proporción: se sigue viendo del mismo tamaño.
// Entra con una animación corta y oscurece el resto del cuarto con una esfera semitransparente
// alrededor de la cabeza.
//
// Se anima con el tiempo real (unscaledDeltaTime), así no depende de la velocidad del juego.
// Después de moverlo se sincroniza la física (Physics.SyncTransforms): así el rayo del control
// encuentra los botones donde están ahora, desde el primer cuadro.
//
// En la escena: va en el objeto del menú. "contenido" es el hijo con las piezas del panel.
public class PanelFlotante : MonoBehaviour
{
    [Tooltip("El hijo con las piezas del panel: se prende al mostrarlo y se apaga al ocultarlo")]
    public Transform contenido;

    [Tooltip("A cuántos metros de la cabeza aparece")]
    public float distancia = 1.1f;

    [Tooltip("Lo más cerca que puede quedar si hay una pared adelante")]
    public float distanciaMinima = 0.55f;

    [Tooltip("Ancho del panel en metros: para revisar si hay algo a los costados")]
    public float anchoPanel = 1f;

    [Tooltip("Cuánto más abajo de los ojos queda el centro del panel")]
    public float bajoLosOjos = 0.06f;

    [Header("Oscurecer el resto")]
    [Tooltip("Esfera alrededor de la cabeza, sin collider")]
    public Renderer oscurecedor;

    [Range(0f, 1f)] public float oscuridad = 0.6f;

    public bool Visible { get; private set; }

    Transform camara;
    MaterialPropertyBlock bloque;
    float avance;   // 0 = oculto, 1 = visible del todo

    void Awake()
    {
        bloque = new MaterialPropertyBlock();
        if (contenido != null) contenido.gameObject.SetActive(false);
        if (oscurecedor != null) oscurecedor.gameObject.SetActive(false);
    }

    public void Mostrar()
    {
        if (camara == null && Camera.main != null) camara = Camera.main.transform;
        Colocar();
        Visible = true;
        contenido.gameObject.SetActive(true);
        if (oscurecedor != null) oscurecedor.gameObject.SetActive(true);
        Animar();
    }

    public void Ocultar() => Visible = false;

    void Update()
    {
        if (contenido == null || !contenido.gameObject.activeSelf) return;

        float objetivo = Visible ? 1f : 0f;
        if (avance != objetivo)
        {
            avance = Mathf.MoveTowards(avance, objetivo, Time.unscaledDeltaTime / 0.2f);
            Animar();
        }

        if (!Visible && avance <= 0f)
        {
            contenido.gameObject.SetActive(false);
            if (oscurecedor != null) oscurecedor.gameObject.SetActive(false);
        }
    }

    void LateUpdate()
    {
        // La esfera oscura acompaña a la cabeza (el jugador se puede mover con el cuerpo)
        if (oscurecedor != null && oscurecedor.gameObject.activeSelf && camara != null)
            oscurecedor.transform.position = camara.position;
    }

    void Animar()
    {
        float suave = avance * avance * (3f - 2f * avance);
        contenido.localScale = Vector3.one * Mathf.Lerp(0.9f, 1f, suave);
        Physics.SyncTransforms();

        if (oscurecedor != null)
        {
            oscurecedor.GetPropertyBlock(bloque);
            bloque.SetColor("_BaseColor", new Color(0f, 0f, 0f, oscuridad * suave));
            oscurecedor.SetPropertyBlock(bloque);
        }
    }

    // Delante de la cabeza, en horizontal (aunque mire al piso) y a la altura de los ojos
    void Colocar()
    {
        if (camara == null) return;
        Vector3 ojos = camara.position;
        Vector3 frente = Vector3.ProjectOnPlane(camara.forward, Vector3.up);
        if (frente.sqrMagnitude < 0.001f) frente = Vector3.ProjectOnPlane(camara.up, Vector3.up);   // mira justo abajo
        frente.Normalize();
        Vector3 derecha = Vector3.Cross(Vector3.up, frente);

        // Tres rayos: al centro del panel y a sus dos bordes. Si alguno choca antes, se acerca.
        float d = distancia;
        foreach (float lado in new[] { 0f, -0.5f, 0.5f })
        {
            Vector3 hasta = frente * distancia + derecha * (lado * anchoPanel);
            if (Physics.Raycast(ojos, hasta.normalized, out RaycastHit golpe, hasta.magnitude + 0.1f, ~0,
                                QueryTriggerInteraction.Ignore))
            {
                // La distancia del golpe, llevada al eje del frente, con 10 cm de margen
                float adelante = golpe.distance * (distancia / hasta.magnitude) - 0.1f;
                d = Mathf.Min(d, adelante);
            }
        }
        d = Mathf.Max(d, distanciaMinima);

        transform.SetPositionAndRotation(ojos + frente * d - Vector3.up * bajoLosOjos, Quaternion.LookRotation(frente));
        transform.localScale = Vector3.one * (d / distancia);
        if (oscurecedor != null) oscurecedor.transform.position = ojos;
    }
}
