using TMPro;
using UnityEngine;

// Cartel del cuarto que dice qué hay que hacer ahora.
// No es un menú de ayuda: es un objeto más del cuarto (un tablero de avisos colgado
// en la pared), y el texto va cambiando solo a medida que el jugador avanza.
//
// Cada paso del acertijo llama a MostrarPaso() desde su evento en el Inspector:
// la llave del tablero muestra el paso 1, la bitácora el 2, y así.
public class PanelObjetivo : MonoBehaviour
{
    [Tooltip("El texto del cartel")]
    public TMP_Text texto;

    [Tooltip("Los mensajes, en orden. El 0 es el que se ve al entrar")]
    [TextArea(2, 5)]
    public string[] pasos;

    int actual;

    void Start() => Mostrar();

    // Solo avanza: si el jugador vuelve a tocar algo que ya hizo, no retrocede el cartel
    public void MostrarPaso(int paso)
    {
        if (paso <= actual) return;
        actual = Mathf.Clamp(paso, 0, pasos.Length - 1);
        Mostrar();
    }

    void Mostrar()
    {
        if (texto != null && pasos.Length > 0) texto.text = pasos[actual];
    }
}
