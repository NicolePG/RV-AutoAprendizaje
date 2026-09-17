using UnityEngine;

// Maneja el apagón del Cuarto 2.
//
// Sin energía: la luz ambiental queda casi negra, las luces del cuarto apagadas y
// las tiras LED moradas a full, así el cuarto se ve oscuro y morado de verdad.
// Al subir la llave del tablero se llama a Encender(): suben las luces del cuarto
// y la luz ambiental, las tiras LED bajan a un brillo suave, y despiertan los
// relojes y el teclado.
//
// La luz ambiental se maneja acá (y no con luces sueltas) porque es lo único que
// ilumina parejo todo el cuarto: con unos pocos focos, un cuarto de 6 x 7 metros
// sigue viéndose oscuro aunque estén encendidos.
public class ControlEnergia : MonoBehaviour
{
    [Header("Luces del cuarto (apagadas hasta que vuelve la energía)")]
    public Light[] lucesDelCuarto;

    [Header("Tiras LED moradas")]
    public Light[] lucesLed;
    public Renderer[] tirasLed;
    public Material materialLedFuerte;
    public Material materialLedSuave;

    [Header("Lo que despierta cuando vuelve la energía")]
    public GameObject[] objetosConEnergia;

    [Header("Luz ambiental")]
    public Color ambienteSinEnergia = new Color(0.035f, 0.025f, 0.06f);
    public Color ambienteConEnergia = new Color(0.30f, 0.29f, 0.27f);

    [Header("Intensidad de las tiras LED")]
    public float ledSinEnergia = 5f;
    public float ledConEnergia = 1.1f;

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

        foreach (var luz in lucesDelCuarto)
            if (luz != null) luz.enabled = energia;

        foreach (var led in lucesLed)
            if (led != null) led.intensity = energia ? ledConEnergia : ledSinEnergia;

        Material material = energia ? materialLedSuave : materialLedFuerte;
        if (material != null)
            foreach (var tira in tirasLed)
                if (tira != null) tira.sharedMaterial = material;

        foreach (var objeto in objetosConEnergia)
            if (objeto != null) objeto.SetActive(energia);
    }
}
