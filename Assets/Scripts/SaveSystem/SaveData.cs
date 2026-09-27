using System.Collections.Generic;

// Lo que se guarda de una partida. SaveManager lo convierte a JSON y lo escribe en un archivo.
//
// El juego guarda por cuarto (punto de control): al cargar, el jugador vuelve al principio del
// cuarto donde iba, con el tiempo que le quedaba. Por eso no hace falta guardar dónde estaba
// parado ni cómo quedó cada objeto: las puertas de los cuartos anteriores ya se cerraron detrás
// de él y no se puede volver.
[System.Serializable]
public class SaveData
{
    public int version = 1;

    // En qué cuarto va el jugador: 1 a 4
    public int cuartoActual = 1;

    // Segundos que le quedaban en el reloj
    public float tiempoRestante;

    // Los acertijos que ya resolvió (el "id" de su PuzzleData)
    public List<string> acertijosResueltos = new List<string>();

    // Cuándo se guardó, para mostrarlo en los menús ("24/09 11:40")
    public string fecha;
}
