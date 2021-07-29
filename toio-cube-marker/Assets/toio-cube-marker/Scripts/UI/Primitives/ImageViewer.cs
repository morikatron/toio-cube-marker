using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CubeMarker
{
    public class ImageViewer : MonoBehaviour
    {
        public RawImage image;

        public void SetTexture(Texture2D tex)
        {
            image.texture = tex;

            var texSize = new Vector2(tex.width, tex.height);
            var bgSize = (this.transform as RectTransform).rect.size;

            var wScale = bgSize.x / texSize.x;
            var hScale = bgSize.y / texSize.y;
            var rectSize = texSize * Mathf.Min(hScale, wScale);

            var anchorDiff = image.rectTransform.anchorMax - image.rectTransform.anchorMin;
            var parentSize = (image.transform.parent as RectTransform).rect.size;
            var anchorSize = parentSize * anchorDiff;

            image.rectTransform.sizeDelta = rectSize - anchorSize;
        }
    }

}
