using UnityEngine;

// Cartel que aparece delante de la vista del jugador para poder leerlo cómodo, y que
// lo acompaña si mueve la cabeza. Se usa para las pistas de la computadora y para el
// acertijo del cuaderno, porque leer texto chico pegado a un objeto es incómodo en VR.
//
// En la escena: va en un objeto vacío, con el cartel (fondo + texto) como hijo,
// asignado en "contenido". La cámara del jugador tiene que tener el tag MainCamera.
public class PanelEnLaCara : MonoBehaviour
{
    [Tooltip("El cartel que se muestra y se oculta")]
    public GameObject contenido;

    [Tooltip("A cuántos metros de la vista se pone")]
    public float distancia = 0.6f;

    [Tooltip("Cuánto se baja respecto de la línea de la vista, en metros")]
    public float bajar = 0.12f;

    [Tooltip("Qué tan rápido acompaña el movimiento de la cabeza")]
    public float velocidad = 10f;

    public bool Visible { get; private set; }

    Transform camara;

    void Start()
    {
        if (contenido != null) contenido.SetActive(false);
    }

    public void Alternar()
    {
        if (Visible) Ocultar();
        else Mostrar();
    }

    public void Mostrar()
    {
        if (camara == null && Camera.main != null) camara = Camera.main.transform;
        if (camara == null) return;

        Visible = true;
        if (contenido != null) contenido.SetActive(true);
        Colocar(1f);   // aparece ya en posición, sin viajar desde donde estaba
    }

    public void Ocultar()
    {
        Visible = false;
        if (contenido != null) contenido.SetActive(false);
    }

    void LateUpdate()
    {
        if (Visible) Colocar(1f - Mathf.Exp(-velocidad * Time.deltaTime));
    }

    void Colocar(float t)
    {
        Vector3 destino = camara.position + camara.forward * distancia - camara.up * bajar;
        // El frente del cartel mira a la cámara, así el texto se lee derecho
        Quaternion rotacion = Quaternion.LookRotation(-camara.forward, camara.up);

        transform.position = Vector3.Lerp(transform.position, destino, t);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotacion, t);
    }
}
