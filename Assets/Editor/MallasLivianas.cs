using UnityEditor;

// Optimización: los modelos FBX de Poly Haven se importan con "Mesh LOD".
//
// Mesh LOD es una función de Unity 6: al importar un modelo, Unity arma varias versiones del
// mismo objeto, cada una con menos triángulos, y al jugar usa sola la que conviene según qué
// tan grande se ve el objeto en pantalla. De cerca se ve el modelo completo; lejos o en un
// rincón, uno más liviano que a esa distancia se ve igual. Algunos modelos de Poly Haven son
// muy pesados para el Quest (la planta tiene 70 mil triángulos, las cañerías 95 mil) y así se
// alivianan en todos los cuartos sin tocar los constructores.
//
// Este script se aplica solo a cada modelo que Unity importa (es un AssetPostprocessor). Los
// .gltf y .glb no pasan por acá (los importa glTFast): esos se eligen livianos en el constructor.
public class MallasLivianas : AssetPostprocessor
{
    // Si se cambia este número, Unity vuelve a importar los modelos con la configuración nueva
    public override uint GetVersion() => 1;

    void OnPreprocessModel()
    {
        if (!assetPath.StartsWith("Assets/PolyHaven/") && !assetPath.StartsWith("Assets/Modelos/")) return;
        if (assetImporter is ModelImporter importador)
            importador.generateMeshLods = true;
    }
}
