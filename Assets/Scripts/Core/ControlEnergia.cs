using UnityEngine;

// Maneja el apagón del Cuarto 2.
//
// Sin energía: luz ambiental casi negra, niebla, las luces del cuarto apagadas y
// solo las luces rojas de emergencia en las esquinas del techo, algunas parpadeando.
// Al subir la llave del tablero se llama a Encender(): se prenden las luces, sube la
// luz ambiental, se abre la niebla, se apagan las rojas y despiertan los relojes y
// el teclado. El cuarto queda con su aspecto normal.
//
// La luz ambiental se maneja acá (y no con luces sueltas) porque es lo único que
// ilumina parejo todo el cuarto: con unos pocos focos, un cuarto de 6 x 7 metros
// sigue viéndose oscuro aunque estén encendidos.
public class ControlEnergia : MonoBehaviour
{
    [Header("Luces del cuarto (apagadas hasta que vuelve la energía)")]
    public Light[] lucesDelCuarto;

    [Header("Lo que se enciende cuando vuelve la energía")]
    public GameObject[] objetosConEnergia;

    [Header("Lo que solo existe a oscuras (clima de terror)")]
    public GameObject[] objetosSinEnergia;
    public Behaviour[] efectosSinEnergia;

    [Header("Luz ambiental")]
    public Color ambienteSinEnergia = new Color(0.02f, 0.02f, 0.035f);
    public Color ambienteConEnergia = new Color(0.55f, 0.53f, 0.5f);

    [Header("Reflejos del cielo (hacen brillar el piso y los vidrios aunque no haya luz)")]
    public float reflejosSinEnergia = 0.05f;
    public float reflejosConEnergia = 1f;

    [Header("Niebla")]
    public Color nieblaSinEnergia = new Color(0.02f, 0.02f, 0.04f);
    public Color nieblaConEnergia = new Color(0.3f, 0.3f, 0.3f);
    public float densidadSinEnergia = 0.06f;
    public float densidadConEnergia = 0.008f;

    public bool HayEnergia { get; private set; }

    void Start() => Aplicar(false);

    public void Encender()
    {
        if (HayEnergia) return;
        Aplicar(true);
    }

    void Aplicar(bool energia)
    {
        HayEnergia = energia;

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = energia ? ambienteConEnergia : ambienteSinEnergia;
        RenderSettings.reflectionIntensity = energia ? reflejosConEnergia : reflejosSinEnergia;

        // La niebla cierra la visibilidad mientras está oscuro y se abre con la luz
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogColor = energia ? nieblaConEnergia : nieblaSinEnergia;
        RenderSettings.fogDensity = energia ? densidadConEnergia : densidadSinEnergia;

        foreach (var luz in lucesDelCuarto)
            if (luz != null) luz.enabled = energia;

        foreach (var objeto in objetosConEnergia)
            if (objeto != null) objeto.SetActive(energia);

        foreach (var objeto in objetosSinEnergia)
            if (objeto != null) objeto.SetActive(!energia);

        foreach (var efecto in efectosSinEnergia)
            if (efecto != null) efecto.enabled = !energia;
    }
}
