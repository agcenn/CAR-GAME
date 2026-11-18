using UnityEngine;
namespace OriginalKartRacer.Track
{
    /// <summary>动态字体图集重建后，更新带深度测试的路牌材质。</summary>
    [RequireComponent(typeof(TextMesh))]
    public sealed class WorldLabel : MonoBehaviour
    {
        private TextMesh label;
        private void OnEnable(){label=GetComponent<TextMesh>();Font.textureRebuilt+=Refresh;Refresh(label.font);}
        private void Refresh(Font font){if(label!=null&&font==label.font)GetComponent<Renderer>().sharedMaterial.mainTexture=font.material.mainTexture;}
        private void OnDisable(){Font.textureRebuilt-=Refresh;}
    }
}
