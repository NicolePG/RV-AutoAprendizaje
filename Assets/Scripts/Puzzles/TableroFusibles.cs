using TMPro;
using UnityEngine;

// Acertijo 1 del Cuarto 4: el tablero eléctrico.
//
// El laboratorio está sin luz. En el tablero, junto a la entrada, faltan los fusibles de
// los tres circuitos (A, B y C). El diagrama de circuitos pegado al lado dice cuánto consume
// cada uno y la regla del electricista: "fusible normalizado inmediatamente superior al
// consumo". En la caja de repuestos hay seis fusibles de distinto amperaje (y color).
// Con los tres correctos puestos vuelve la luz: se dispara "alResolverse".
//
// En la escena: va en el tablero. ConstructorCuarto4 lo arma y lo conecta.
public class TableroFusibles : PuzzleBase
{
    [Tooltip("Los portafusibles de los circuitos A, B y C")]
    public PortaFusible[] portas;

    [Tooltip("Visor del tablero: dice cuántos circuitos están bien")]
    public TMP_Text visor;

    void Awake()
    {
        foreach (PortaFusible p in portas) p.alCambiar.AddListener(Revisar);
    }

    void Start() => Revisar();

    void Revisar()
    {
        if (Resuelto) return;
        int bien = 0;
        foreach (PortaFusible p in portas)
            if (p.Correcto) bien++;
        if (visor != null) visor.text = "CIRCUITOS OK: " + bien + " / " + portas.Length;
        if (bien == portas.Length) Resolver();
    }

    protected override void MostrarAcierto()
    {
        if (visor != null)
        {
            visor.text = "ENERGÍA RESTABLECIDA";
            visor.color = new Color(0.4f, 1f, 0.55f);
        }
    }
}
