using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Llave que se maneja sola, para no tener que ir sosteniéndola por todo el cuarto.
// Con el control hay que mantener el gatillo apretado todo el camino y se cae a cada
// rato, así que acá la llave hace el trabajo:
//
// 1) El jugador le hace un clic y la llave se le queda adelante de la vista, como si la
//    llevara en la mano. No se suelta ni hay que mantener nada apretado.
// 2) Cuando el jugador se acerca a la cerradura, la llave se va sola hasta la ranura y
//    se encaja.
// 3) Ahí avisa a la cerradura, que ya puede girar.
//
// En la escena: va en la llave, junto a un XR Simple Interactable. En "ranura" va el
// objeto vacío que está dentro de la cerradura de la puerta. La cámara del jugador
// tiene que tener el tag MainCamera.
[RequireComponent(typeof(XRSimpleInteractable))]
public class LlaveAutomatica : MonoBehaviour
{
    [Tooltip("El punto de la cerradura donde tiene que entrar la llave")]
    public Transform ranura;

    [Tooltip("A qué distancia de la ranura se encaja sola, en metros")]
    public float distanciaParaEncajar = 1.8f;

    [Tooltip("Qué tan rápido acompaña al jugador y viaja hasta la cerradura")]
    public float velocidad = 7f;

    [Tooltip("Qué pasa cuando la llave queda encajada")]
    public UnityEvent alEncajar = new UnityEvent();

    [Tooltip("Qué pasa cuando el jugador toca la llave ya encajada, para girarla")]
    public UnityEvent alGirar = new UnityEvent();

    public bool EnMano { get; private set; }
    public bool Encajada { get; private set; }

    XRSimpleInteractable interactable;
    Transform camara;

    void Awake() => interactable = GetComponent<XRSimpleInteractable>();

    void OnEnable() => interactable.selectEntered.AddListener(Agarrar);
    void OnDisable() => interactable.selectEntered.RemoveListener(Agarrar);

    void Agarrar(SelectEnterEventArgs args)
    {
        // Ya puesta en la cerradura: este toque es el jugador girándola
        if (Encajada)
        {
            alGirar.Invoke();
            return;
        }

        if (EnMano) return;
        EnMano = true;

        // Mientras la lleva no tiene que molestar al rayo del control
        Colisiones(false);
    }

    void Colisiones(bool prendidas)
    {
        foreach (var col in GetComponentsInChildren<Collider>()) col.enabled = prendidas;
    }

    void Update()
    {
        if (Encajada || !EnMano) return;

        if (camara == null)
        {
            if (Camera.main == null) return;
            camara = Camera.main.transform;
        }

        // Por defecto la llave viaja adelante y abajo de la vista, como en la mano
        Vector3 destino = camara.position + camara.forward * 0.42f - camara.up * 0.2f;
        Quaternion giro = Quaternion.LookRotation(camara.forward, Vector3.up);

        bool cerca = ranura != null &&
                     Vector3.Distance(camara.position, ranura.position) <= distanciaParaEncajar;
        if (cerca)
        {
            destino = ranura.position;
            giro = ranura.rotation;

            // Llegó: queda colgada de la ranura, así después gira junto con ella
            if (Vector3.Distance(transform.position, destino) < 0.02f)
            {
                transform.SetParent(ranura, true);
                transform.position = destino;
                transform.rotation = giro;

                Encajada = true;
                EnMano = false;
                // Se le devuelven los colliders: ahora hay que tocarla para girarla
                Colisiones(true);
                alEncajar.Invoke();
                return;
            }
        }

        float t = 1f - Mathf.Exp(-velocidad * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, destino, t);
        transform.rotation = Quaternion.Slerp(transform.rotation, giro, t);
    }
}
