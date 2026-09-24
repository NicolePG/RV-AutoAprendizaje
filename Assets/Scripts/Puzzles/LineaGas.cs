using UnityEngine;

// Acertijo 3 del Cuarto 4: abrir la línea de gas del laboratorio.
//
// Junta las dos llaves de paso (V1 y V2). Empiezan bloqueadas; la campana de extracción,
// al quedar andando, llama a Desbloquear(). Con las dos abiertas del todo se dispara
// "alResolverse": se encienden los mecheros de la mesada (el ensayo a la llama los necesita).
//
// En la escena: va en la línea de gas. ConstructorCuarto4 la arma y la conecta.
public class LineaGas : PuzzleBase
{
    public ValvulaGas[] valvulas;

    [Tooltip("Los mecheros que se encienden cuando llega el gas")]
    public Mechero[] mecheros;

    void Awake()
    {
        foreach (ValvulaGas v in valvulas) v.alAbrir.AddListener(Revisar);
    }

    public void Desbloquear()
    {
        foreach (ValvulaGas v in valvulas) v.Desbloquear();
    }

    void Revisar()
    {
        foreach (ValvulaGas v in valvulas)
            if (!v.Abierta) return;
        Resolver();
    }

    protected override void MostrarAcierto()
    {
        foreach (Mechero m in mecheros) m.Encender();
    }
}
