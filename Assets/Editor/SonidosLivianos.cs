using UnityEditor;
using UnityEngine;

// Optimización: los sonidos del juego se importan como los necesita el Quest.
//
// Por qué hace falta. Unity importa el audio pensando en una PC: deja los archivos en
// estéreo y los descomprime enteros en memoria al cargar la escena. En el visor eso es
// caro por dos motivos:
//
//  1. ESTÉREO. Un sonido que suena en un lugar del cuarto (la puerta, un botón, un crujido)
//     tiene que ser MONO. Si es estéreo, Unity lo mezcla a mono igual para poder ubicarlo en
//     el espacio, pero mientras tanto ocupa el doble de memoria para nada. Y, peor, algunos
//     estéreos ni se ubican bien y se oyen "en la cabeza" en vez de venir de la puerta.
//
//  2. DESCOMPRIMIDO. Por defecto cada clip se descomprime entero en memoria. Los cortos no
//     molestan, pero los cuatro ambientes son largos y darían varios MB cada uno. Esos van en
//     streaming: se leen del disco mientras suenan.
//
// Se aplica solo a Assets/Resources/Audio, que es donde están los sonidos del juego (los del
// template de Unity no se tocan). Igual que MallasLivianas y TexturasLivianas, el archivo
// original no se modifica: Unity solo lo guarda distinto para el juego.
public class SonidosLivianos : AssetPostprocessor
{
    // Si se cambia este número, Unity vuelve a importar los sonidos con la configuración nueva
    public override uint GetVersion() => 1;

    void OnPreprocessAudio()
    {
        if (!assetPath.StartsWith("Assets/Resources/Audio/")) return;
        if (assetImporter is not AudioImporter importador) return;

        importador.forceToMono = true;

        AudioImporterSampleSettings ajustes = importador.defaultSampleSettings;
        ajustes.compressionFormat = AudioCompressionFormat.Vorbis;
        ajustes.quality = 0.6f;
        // Los ambientes son largos y van en bucle: se leen del disco mientras suenan.
        // Los demás son golpes cortos: se dejan comprimidos en memoria y salen al instante.
        ajustes.loadType = assetPath.Contains("/ambiente_")
            ? AudioClipLoadType.Streaming
            : AudioClipLoadType.CompressedInMemory;

        importador.defaultSampleSettings = ajustes;
    }
}
