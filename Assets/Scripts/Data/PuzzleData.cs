using UnityEngine;

// Qué tipo de acertijo es. Sirve para describirlo; el comportamiento lo pone cada script de acertijo.
public enum TipoAcertijo { Llave, Codigo, Computadoras, Laboratorio, Luces, Red }

// Datos de un acertijo guardados como asset (ScriptableObject).
//
// Por qué un ScriptableObject: los textos y la solución de cada acertijo viven en un archivo que se edita
// en el Inspector, sin tocar código. Cada acertijo del juego es un asset en Assets/ScriptableObjects/Puzzles,
// y el sistema de guardado usará su "id" para recordar cuáles se resolvieron.
//
// Crear uno a mano: clic derecho en Project > Create > Escape Room > Datos de acertijo.
[CreateAssetMenu(fileName = "NuevoAcertijo", menuName = "Escape Room/Datos de acertijo")]
public class PuzzleData : ScriptableObject
{
    [Tooltip("Identificador único, sin espacios (lo usa el guardado)")]
    public string id;

    public string nombreCuarto;

    [Tooltip("Qué tiene que descubrir el jugador (para el equipo, no se muestra en el juego)")]
    [TextArea] public string pista;

    public TipoAcertijo tipo;

    [Tooltip("La respuesta: un código, el nombre de un objeto, un orden de palancas...")]
    public string solucion;

    [Tooltip("Texto que se muestra al resolverlo")]
    public string mensajeAcierto = "ABIERTO";

    [Tooltip("Texto que se muestra mientras no está resuelto o al equivocarse")]
    public string mensajeError = "BLOQUEADO";
}
