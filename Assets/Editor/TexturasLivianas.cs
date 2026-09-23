using UnityEditor;

// Optimización: las texturas sueltas de los modelos de Sketchfab se importan a 1k.
//
// Los modelos bajados en formato glTF no traen las texturas adentro del archivo: vienen como
// PNG sueltos en una carpeta "textures". Sketchfab los entrega en 4K, y uno solo puede pesar
// 19 MB: el cuadro del Cuarto 2 venía con 40 MB de texturas. En el Quest eso llena la memoria
// de video y hace caer los FPS, y encima ni se nota, porque el cuadro se ve chico y de lejos.
//
// Este script se aplica solo a cada textura que Unity importa (es un AssetPostprocessor) y a
// las de Assets/Sketchfab les pone el mismo límite de 1k que usan las de Poly Haven. El archivo
// original no se toca: Unity solo lo guarda más chico para el juego.
public class TexturasLivianas : AssetPostprocessor
{
    // Si se cambia este número, Unity vuelve a importar las texturas con la configuración nueva
    public override uint GetVersion() => 1;

    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/Sketchfab/")) return;
        if (assetImporter is not TextureImporter importador) return;

        importador.maxTextureSize = 1024;
        importador.textureCompression = TextureImporterCompression.Compressed;

        // Los mapas de normales hay que marcarlos como tales o Unity los trata como color
        // y el relieve sale mal. Sketchfab los nombra siempre terminados en "normal".
        if (assetPath.EndsWith("normal.png"))
            importador.textureType = TextureImporterType.NormalMap;
    }
}
