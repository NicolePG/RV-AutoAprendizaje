using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Guarda y carga la partida en un archivo JSON que queda en el visor aunque se cierre el juego
// (Application.persistentDataPath: en el Quest es la carpeta de datos de la app; en el PC,
// AppData/LocalLow/<empresa>/<juego>).
//
//  - Guardar(cuarto, tiempo): escribe el archivo. Primero en uno temporal y después lo pone en
//    su lugar: si el juego se cierra justo mientras escribe, la partida anterior no se rompe.
//  - Ultima: la partida guardada, leída al empezar (null si no hay o si el archivo está roto).
//  - HayPartida: para que los menús sepan si ofrecer "Cargar partida".
// También lleva la cuenta de los acertijos resueltos: cada acertijo avisa al resolverse
// (PuzzleBase.AlResolverCualquiera).
//
// Quién decide cuándo guardar y qué hacer al cargar es GameManager.
//
// En la escena: va en el objeto "Sistema". ConstructorSistema lo arma.
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instancia { get; private set; }

    const string ARCHIVO = "partida.json";
    public static string Ruta => Path.Combine(Application.persistentDataPath, ARCHIVO);
    static string RutaTemporal => Ruta + ".tmp";

    public SaveData Ultima { get; private set; }
    public bool HayPartida => Ultima != null;

    // Cuántos acertijos tiene el juego (los que tienen su PuzzleData) y cuántos ya resolvió
    public int TotalAcertijos { get; private set; }
    public int AcertijosResueltos => resueltos.Count;

    readonly HashSet<string> resueltos = new HashSet<string>();

    void Awake()
    {
        Instancia = this;
        Ultima = LeerArchivo();
        PuzzleBase.AlResolverCualquiera += Anotar;
    }

    void Start()
    {
        foreach (PuzzleBase acertijo in FindObjectsByType<PuzzleBase>())
            if (TieneId(acertijo)) TotalAcertijos++;
    }

    void OnDestroy()
    {
        PuzzleBase.AlResolverCualquiera -= Anotar;
        if (Instancia == this) Instancia = null;
    }

    static bool TieneId(PuzzleBase acertijo) => acertijo.datos != null && !string.IsNullOrEmpty(acertijo.datos.id);

    // true si ese acertijo ya se resolvió en esta partida (lo usa la pausa para el progreso del cuarto)
    public bool Resuelto(PuzzleData datos) => datos != null && resueltos.Contains(datos.id);

    // Cuántos acertijos de un cuarto (sus RoomData.acertijos) ya se resolvieron
    public int ResueltosDe(RoomData cuarto)
    {
        int n = 0;
        if (cuarto != null)
            foreach (PuzzleData datos in cuarto.acertijos)
                if (Resuelto(datos)) n++;
        return n;
    }

    void Anotar(PuzzleBase acertijo)
    {
        if (TieneId(acertijo)) resueltos.Add(acertijo.datos.id);
    }

    // Al cargar una partida: los acertijos de los cuartos anteriores ya estaban resueltos
    public void Restaurar(SaveData datos)
    {
        foreach (string id in datos.acertijosResueltos)
            if (!string.IsNullOrEmpty(id) && !EsDelCuartoOPosterior(id, datos.cuartoActual)) resueltos.Add(id);
    }

    // Se guarda por punto de control: al cargar, el cuarto guardado arranca de cero. Por eso solo
    // cuentan los acertijos de los cuartos anteriores: si se guarda a mitad de un cuarto, lo que ya
    // se resolvió en él no se anota (al cargar hay que volver a resolverlo). Qué acertijos tiene
    // cada cuarto sale de sus RoomData.
    static bool EsDelCuartoOPosterior(string id, int cuarto)
    {
        for (int n = cuarto; n <= GameManager.TOTAL_CUARTOS; n++)
        {
            RoomData datos = GameManager.DatosCuarto(n);
            if (datos == null) continue;
            foreach (PuzzleData acertijo in datos.acertijos)
                if (acertijo != null && acertijo.id == id) return true;
        }
        return false;
    }

    public bool Guardar(int cuarto, float tiempoRestante)
    {
        var anotados = new List<string>();
        foreach (string id in resueltos)
            if (!EsDelCuartoOPosterior(id, cuarto)) anotados.Add(id);

        var datos = new SaveData
        {
            cuartoActual = cuarto,
            tiempoRestante = tiempoRestante,
            acertijosResueltos = anotados,
            fecha = DateTime.Now.ToString("dd/MM HH:mm")
        };

        try
        {
            File.WriteAllText(RutaTemporal, JsonUtility.ToJson(datos, true));
            if (File.Exists(Ruta)) File.Delete(Ruta);
            File.Move(RutaTemporal, Ruta);
            Ultima = datos;
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning("No se pudo guardar la partida en " + Ruta + ": " + e.Message);
            return false;
        }
    }

    // Lee la partida guardada. Si el juego se cerró justo al guardar, puede haber quedado solo
    // el archivo temporal: también sirve. Si falta algo o tiene valores imposibles, no hay partida.
    static SaveData LeerArchivo()
    {
        try
        {
            string ruta = File.Exists(Ruta) ? Ruta : File.Exists(RutaTemporal) ? RutaTemporal : null;
            if (ruta == null) return null;

            var datos = JsonUtility.FromJson<SaveData>(File.ReadAllText(ruta));
            if (datos == null || datos.cuartoActual < 1 || datos.cuartoActual > GameManager.TOTAL_CUARTOS ||
                datos.tiempoRestante <= 0f)
                return null;
            if (datos.acertijosResueltos == null) datos.acertijosResueltos = new List<string>();
            return datos;
        }
        catch (Exception e)
        {
            Debug.LogWarning("La partida guardada no se pudo leer (" + e.Message + "). El juego empieza de cero.");
            return null;
        }
    }

    // "Cuarto 3 · 06:12" para los menús
    public static string Resumen(SaveData datos) =>
        datos == null ? "" : "Cuarto " + datos.cuartoActual + " · " + TimerController.Formato(datos.tiempoRestante);
}
