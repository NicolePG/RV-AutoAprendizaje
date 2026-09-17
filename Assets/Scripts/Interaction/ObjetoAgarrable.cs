using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// Completa al XRGrabInteractable para que los objetos se comporten como en la vida real:
//  - Si empieza guardado (dentro de un cajón) queda quieto, sin física, y se mueve con el cajón.
//    Al soltarlo por primera vez ya cae y choca normalmente.
//  - Al soltarlo, si quedó metido en la mesa o en una pared, sale hacia afuera antes de caer.
//  - Mientras se sostiene no se "duerme" su física: así la punta del destornillador sigue detectando
//    el tornillo aunque la mano se quede quieta.
//  - Si se pierde, aparece sobre el mostrador (a la vista) para que el acertijo nunca quede imposible.
//    "Perderse" es: salir del cuarto o quedar atrapado dentro de un mueble o una pared.
//
// El agarre firme (la herramienta queda fija en la mano, en su posición correcta) lo configura
// Cuarto1Builder en el XRGrabInteractable: punto de agarre fijo, movimiento Instantaneous y "traer a la mano".
//
// En la escena: va en el objeto junto a su XRGrabInteractable y su Rigidbody. Cuarto1Builder lo agrega solo.
[RequireComponent(typeof(XRGrabInteractable))]
public class ObjetoAgarrable : MonoBehaviour
{
    [Tooltip("Dónde aparece si se pierde (en el mundo): un lugar a la vista, por ejemplo sobre el mostrador")]
    public Vector3 puntoDeRescate = new Vector3(0, 1.2f, 0);

    [Tooltip("Zona del cuarto donde el objeto puede estar (en el mundo). Fuera de ella se rescata")]
    public Bounds zonaPermitida = new Bounds(new Vector3(0, 1.3f, 0), new Vector3(4f, 2.6f, 4f));

    [Tooltip("Cada cuántos segundos se revisa si se perdió")]
    public float intervaloDeRevision = 0.3f;

    XRGrabInteractable agarre;
    Rigidbody cuerpo;
    Collider forma;
    float proximaRevision;
    readonly Collider[] tocados = new Collider[8];

    void Awake()
    {
        agarre = GetComponent<XRGrabInteractable>();
        cuerpo = GetComponent<Rigidbody>();
        forma = GetComponent<Collider>();
    }

    void OnEnable() => agarre.selectExited.AddListener(AlSoltar);
    void OnDisable() => agarre.selectExited.RemoveListener(AlSoltar);

    void AlSoltar(SelectExitEventArgs args)
    {
        // Si lo suelta un encaje (socket), el encaje decide qué pasa con el objeto
        if (args.interactorObject is XRSocketInteractor) return;
        // Al soltarlo con la mano ya tiene física de verdad, aunque haya empezado quieto en un cajón
        cuerpo.isKinematic = false;
        SacarDeSuperficies();
    }

    // La mano atraviesa todo, así que al soltar un objeto puede quedar metido en la mesa o en una pared.
    // Se calcula cuánto está metido en cada cosa fija que toca y se corre hacia afuera, antes de que la física
    // lo empuje hacia el lado equivocado (así fue como la llave atravesó la mesa y se cayó).
    void SacarDeSuperficies()
    {
        if (forma == null) return;
        for (int intento = 0; intento < 3; intento++)
        {
            Bounds limites = forma.bounds;
            int cantidad = Physics.OverlapBoxNonAlloc(limites.center, limites.extents, tocados, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
            bool seMovio = false;
            for (int i = 0; i < cantidad; i++)
            {
                Collider otro = tocados[i];
                if (otro.attachedRigidbody != null) continue; // solo cuenta lo fijo: mesas, paredes, muebles
                if (Physics.ComputePenetration(forma, transform.position, transform.rotation,
                        otro, otro.transform.position, otro.transform.rotation, out Vector3 direccion, out float distancia))
                {
                    transform.position += direccion * (distancia + 0.002f);
                    seMovio = true;
                }
            }
            if (!seMovio) break;
            Physics.SyncTransforms(); // que la física vea la nueva posición antes de revisar otra vez
        }
    }

    void FixedUpdate()
    {
        if (agarre.isSelected) cuerpo.WakeUp();
    }

    void Update()
    {
        // No se revisa mientras está en la mano ni mientras está quieto guardado (en un cajón o puesto en la cerradura)
        if (agarre.isSelected || cuerpo.isKinematic || Time.time < proximaRevision) return;
        proximaRevision = Time.time + intervaloDeRevision;

        Vector3 centro = forma != null ? forma.bounds.center : transform.position;
        bool perdido = !zonaPermitida.Contains(centro) || AtrapadoEnAlgoFijo(centro);
        if (perdido) Rescatar();
    }

    // true si el centro del objeto quedó dentro de algo fijo (pared, mueble). Un objeto apoyado encima de algo
    // no cuenta: su centro queda por arriba de la superficie.
    bool AtrapadoEnAlgoFijo(Vector3 centro)
    {
        int cantidad = Physics.OverlapSphereNonAlloc(centro, 0.002f, tocados, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < cantidad; i++)
            if (tocados[i].attachedRigidbody == null) return true;
        return false;
    }

    void Rescatar()
    {
        cuerpo.linearVelocity = Vector3.zero;
        cuerpo.angularVelocity = Vector3.zero;
        transform.SetParent(null, true);
        transform.SetPositionAndRotation(puntoDeRescate, Quaternion.identity);
        Debug.Log(name + " se perdió y apareció sobre el mostrador.");
    }
}
