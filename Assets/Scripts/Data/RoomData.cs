using System.Collections.Generic;
using UnityEngine;

// Datos de un cuarto del juego, guardados como asset (ScriptableObject): su número, su nombre,
// sus acertijos y el tiempo sugerido para resolverlo.
//
// Por qué un ScriptableObject: los cuartos no están escritos en el código. GameManager recibe la
// lista de assets y de ahí sacan el nombre del cuarto el reloj, el menú de pausa, la pantalla de
// tiempo agotado y el menú de inicio; la pausa además muestra cuántos acertijos de ESTE cuarto lleva
// el jugador y el tiempo sugerido. Agregar, renombrar o reordenar un cuarto es crear o editar un
// asset, sin tocar GameManager. Los tiempos sugeridos suman los 15 minutos de la partida: sirven
// para balancearla (si un cuarto pide más, otro tiene que pedir menos).
//
// Un asset por cuarto, en Assets/ScriptableObjects/Rooms. ConstructorSistema los crea la primera vez
// (después respeta lo que se cambie en el Inspector) y les carga los acertijos que encuentra en cada
// cuarto. Crear uno a mano: clic derecho en Project > Create > Escape Room > Datos de cuarto.
[CreateAssetMenu(fileName = "Cuarto", menuName = "Escape Room/Datos de cuarto")]
public class RoomData : ScriptableObject
{
    [Tooltip("En qué orden se recorre: 1 a 4")]
    public int numeroCuarto = 1;

    public string nombre;

    [Tooltip("Qué espacio del colegio es y qué se hace ahí (para el equipo)")]
    [TextArea] public string descripcion;

    [Tooltip("Los acertijos de este cuarto (sus PuzzleData)")]
    public List<PuzzleData> acertijos = new List<PuzzleData>();

    [Tooltip("Minutos sugeridos para resolverlo. Entre los cuatro suman los 15 de la partida")]
    public float tiempoSugerido = 3.75f;
}
