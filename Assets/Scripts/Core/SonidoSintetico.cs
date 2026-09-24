using System.Collections.Generic;
using UnityEngine;

// Sonidos cortos armados por código: pitidos, el zumbido de error, el arranque de una
// computadora y un chirrido metálico. El proyecto casi no tiene audios, así que en vez de
// buscar archivos se generan acá, como una onda que se escribe muestra por muestra.
// Cada sonido se crea una sola vez y se guarda para reusarlo.
//
// Se usa desde los scripts: SonidoSintetico.Tocar(SonidoSintetico.Pitido(1200f, 0.08f), posicion);
public static class SonidoSintetico
{
    const int MuestrasPorSegundo = 44100;
    static readonly Dictionary<string, AudioClip> guardados = new Dictionary<string, AudioClip>();

    // Tono puro, como el pitido de una tecla
    public static AudioClip Pitido(float hz, float segundos)
        => Crear("pitido_" + hz + "_" + segundos, segundos, t => Mathf.Sin(2f * Mathf.PI * hz * t));

    // Tono áspero y grave (onda cuadrada): el sonido de "error"
    public static AudioClip Zumbido(float hz, float segundos)
        => Crear("zumbido_" + hz + "_" + segundos, segundos, t => Mathf.Sin(2f * Mathf.PI * hz * t) > 0f ? 0.55f : -0.55f);

    // Tono que sube de desde a hasta: el arranque de una computadora
    public static AudioClip Subida(float desde, float hasta, float segundos)
        => Crear("subida_" + desde + "_" + hasta + "_" + segundos, segundos,
                 t => Mathf.Sin(2f * Mathf.PI * (desde * t + (hasta - desde) * t * t / (2f * segundos))));

    // Chirrido de metal arrastrado: un tono agudo que tiembla, mezclado con ruido
    public static AudioClip Chirrido(float segundos)
    {
        var azar = new System.Random(7);
        return Crear("chirrido_" + segundos, segundos, t =>
        {
            float ruido = (float)azar.NextDouble() * 2f - 1f;
            float hz = 950f + 350f * Mathf.Sin(2f * Mathf.PI * 3f * t) + 120f * ruido;
            return 0.65f * Mathf.Sin(2f * Mathf.PI * hz * t) + 0.35f * ruido;
        });
    }

    // Golpe seco y grave, como un pestillo metálico que se suelta
    public static AudioClip Golpe()
        => Crear("golpe", 0.25f, t => Mathf.Sin(2f * Mathf.PI * 95f * t) * Mathf.Exp(-t * 18f) * 2f);

    // Ruido continuo para usar en loop: el siseo del gas, el rugido de un mechero o el viento
    // de un extractor. "suavidad" le saca lo agudo: 0 = siseo fino, 0.95 = rugido grave.
    public static AudioClip Ruido(float suavidad, float segundos = 2f)
    {
        var azar = new System.Random(11);
        float anterior = 0f;
        return Crear("ruido_" + suavidad + "_" + segundos, segundos, t =>
        {
            float blanco = (float)azar.NextDouble() * 2f - 1f;
            anterior = Mathf.Lerp(blanco, anterior, suavidad);
            return anterior * (1f + suavidad * 3f);   // al filtrarlo baja el volumen: se compensa
        }, true);
    }

    public static void Tocar(AudioClip clip, Vector3 donde, float volumen = 1f)
    {
        if (clip != null) AudioSource.PlayClipAtPoint(clip, donde, volumen);
    }

    // enLoop: el sonido se repite sin cortes, así que no se le suavizan los bordes
    static AudioClip Crear(string nombre, float segundos, System.Func<float, float> onda, bool enLoop = false)
    {
        if (guardados.TryGetValue(nombre, out AudioClip guardado) && guardado != null) return guardado;

        int cantidad = Mathf.CeilToInt(segundos * MuestrasPorSegundo);
        var muestras = new float[cantidad];
        for (int i = 0; i < cantidad; i++)
        {
            float t = i / (float)MuestrasPorSegundo;
            // Sube y baja el volumen en los bordes: sin esto se oye un "clic" al empezar y al terminar
            float borde = enLoop ? 1f : Mathf.Min(1f, t / 0.005f) * Mathf.Min(1f, (segundos - t) / 0.03f);
            muestras[i] = Mathf.Clamp(onda(t), -1f, 1f) * borde * 0.5f;
        }

        AudioClip clip = AudioClip.Create(nombre, cantidad, 1, MuestrasPorSegundo, false);
        clip.SetData(muestras, 0);
        guardados[nombre] = clip;
        return clip;
    }
}
