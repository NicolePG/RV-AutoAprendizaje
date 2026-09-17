using UnityEngine;

// Se agregan más valores acá a medida que se arman los acertijos de los otros cuartos
public enum TipoAcertijo { Codigo }

// Datos de un acertijo, como asset reusable sin tocar código.
// Un asset por acertijo (por ejemplo "PuzzleData_Cuarto2"), con su solución y sus mensajes.
[CreateAssetMenu(fileName = "PuzzleData_", menuName = "EscapeRoom/Puzzle Data")]
public class PuzzleData : ScriptableObject
{
    public string id;
    public string nombreCuarto;

    [TextArea]
    public string pista;

    public TipoAcertijo tipo;

    [Tooltip("La respuesta correcta. Para el Cuarto 2: los 4 dígitos del teclado")]
    public string solucion;

    public string mensajeAcierto;
    public string mensajeError;
}
