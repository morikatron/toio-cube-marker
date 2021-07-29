using UnityEngine;
using UnityEngine.UI;
using static System.Linq.Enumerable;
using toio.Simulator;


namespace CubeMarker
{
    using static PUNProtocolUtils;

    public enum PenFitMode { Fill, Fit, }
    public interface IPen
    {
        // Color[] pixels { get; protected set; }
        Color Draw (Vector2Int size, Vector2Int loc, PenFitMode fitMode=PenFitMode.Fit);
    }
    public class TexturePen : IPen
    {
        public Texture2D tex { get; private set; }

        public TexturePen(Texture2D tex) { this.tex = tex; }
        public Color Draw (Vector2Int size, Vector2Int loc, PenFitMode fitMode=PenFitMode.Fit)
        {
            if (fitMode == PenFitMode.Fill)
            {
                int x = tex.width * loc.x / size.x;
                int y = tex.height * loc.y / size.y;
                return tex.GetPixel(x, y);
            }
            else if (fitMode == PenFitMode.Fit) // Center-Aligned
            {
                float wScale = (float)tex.width / size.x;
                float hScale = (float)tex.height / size.y;
                float scale = Mathf.Max(wScale, hScale);

                var cLoc = loc - size/2;
                int x = (int)(cLoc.x*scale + tex.width/2f);
                int y = (int)(cLoc.y*scale + tex.height/2f);

                if (x >= 0 && x < tex.width && y >= 0 && y < tex.height)
                    return tex.GetPixel(x, y);
            }
            return new Color(0, 0, 0, 0);
        }
    }
    public class SolidPen : IPen
    {
        public Color color { get; private set; }

        public SolidPen(Color color) { this.color = color; }
        public Color Draw (Vector2Int size, Vector2Int loc, PenFitMode fitMode=PenFitMode.Fit)
        {
            return color;
        }
    }



    public class Field : MonoBehaviour
    {

        public static readonly int pixWidth = 304;
        public static readonly int pixHeight = 216;
        public static readonly int matLeft = 98;
        public static readonly int matTop = 142;
        public static readonly int matWidth = 304;
        public static readonly int matHeight = 216;

        // Settings
        public static Vector2 settingPostion = new Vector2(828, -66);
        public static Vector2 settingSize = new Vector2(1344, 947);
        public static Vector3 settingEulers = Vector3.zero;


        protected RawImage rawImage;
        protected Color[] colors;
        protected byte[] occupancy;

        protected Color initialColor;
        protected Texture2D texture;


        private bool started = false;


        void Awake()
        {
        }

        void Start()
        {
            colors = new Color[pixWidth * pixHeight];
            occupancy = new byte[pixWidth * pixHeight];
            texture = new Texture2D(pixWidth, pixHeight);

            rawImage = GetComponent<RawImage>();
            initialColor = rawImage.color;
            rawImage.color = Color.white;
            started = true;

            Clear();
        }

        public void LoadSettings()
        {
            var transform = GetComponent<RectTransform>();
            transform.anchoredPosition = settingPostion;
            transform.sizeDelta = settingSize;
            transform.eulerAngles = settingEulers;
        }

        public void Calibrate(
            float uiX1, float uiY1, float matX1, float matY1,
            float uiX2, float uiY2, float matX2, float matY2
        )
        {
            var vx = matX2 - matX1; var vy = matY2 - matY1; // basis vector in mat coords
            var ux = uiX2 - uiX1; var uy = uiY2 - uiY1;     // basis vector in ui  coords
            var vmag2 = vx * vx + vy * vy;
            float vmag = Mathf.Sqrt(vmag2);
            float umag = Mathf.Sqrt(ux * ux + uy * uy);

            // Decomposite Left-Top corner w.r.t Point1 with basis
            // Eq. (matLeft, matTop) - (matX1, matY1) = a (vx, vy) + b (vy, -vx)         // In Mat Coords
            float x_ = Field.matLeft - matX1; float y_ = Field.matTop - matY1;
            float a = (x_ * vx + y_ * vy) / vmag2;
            float b = (x_ * vy - y_ * vx) / vmag2;

            // Apply a, b by:  (uiLeft, uiTop) - (uiX1, uiY1) = a (ux, uy) + b (-uy, ux)  // In UI Coords, where y axis is symmatric
            float uiLeftTop_x = a * ux - b * uy + uiX1;
            float uiLeftTop_y = a * uy + b * ux + uiY1;

            // Eq. (left, top) = a (vx, vy) + b (vy, -vx)       // In Mat Coords
            x_ = Field.matLeft+Field.matWidth - matX1; y_ = Field.matTop - matY1;
            a = (x_ * vx + y_ * vy) / vmag2;
            b = (x_ * vy - y_ * vx) / vmag2;

            // Apply a, b by:  (left, top) = a (ux, uy) + b (-uy, ux)       // In UI Coords, where y axis is symmatric
            float uiRightTop_x = a * ux - b * uy + uiX1;
            float uiRightTop_y = a * uy + b * ux + uiY1;

            float angle = Mathf.Atan2(uiRightTop_y-uiLeftTop_y, uiRightTop_x-uiLeftTop_x) * 180 / Mathf.PI;

            // Save Settings
            settingPostion = new Vector2(uiLeftTop_x, uiLeftTop_y);
            settingEulers = new Vector3(0, 0, angle);
            settingSize = new Vector2(matWidth * umag/vmag, matHeight * umag/vmag);

            LoadSettings();
        }


        public void Clear()
        {
            if (!started) return;
            for (int i=0; i<colors.Length; i++)
            {
                colors[i] = initialColor;
                occupancy[i] = (byte)255;
            }
            UpdateColors();
        }

        public void Draw(int matX, int matY, int radius, IPen pen, byte id)
        {
            int pixX = (matX - matLeft) * pixWidth / matWidth;
            int pixY = matHeight - (matY - matTop) * pixHeight / matHeight;

            for (int x=Mathf.Max(pixX-radius, 0); x <= Mathf.Min(pixX+radius, pixWidth-1); x++)
            {
                int dy = (int) Mathf.Sqrt(radius * radius - (x-pixX) * (x-pixX));
                for (int y=Mathf.Max(pixY-dy, 0); y <= Mathf.Min(pixY+dy, pixHeight-1); y++)
                {
                    int idx = y * pixWidth + x;

                    // Draw pixel from Pen
                    Color pix = pen.Draw(new Vector2Int(pixWidth, pixHeight), new Vector2Int(x, y));
                    colors[idx] = pix;

                    // Set occupancy
                    occupancy[idx] = id;
                }
            }

            UpdateColors();
        }

        public void UpdateColors()
        {
            this.texture.SetPixels(this.colors);
            this.texture.Apply();
            rawImage.texture = this.texture;
        }

        public float GetOccupancyByID(byte id)
        {
            float pixs = occupancy.Count(n => n==id);
            return pixs / occupancy.Length;
        }

        public byte[] GetOccupancyMap()
        {
            byte[] res = new byte[pixWidth * pixHeight];
            occupancy.CopyTo(res, 0);
            return res;
        }

        /// <summary>
        /// Returns translation and eulers, which shall be applied on Left-Top anchored RectTransform.
        /// </summary>
        public (Vector3, Vector3) CvtCoordsMat2UI(int xMat, int yMat, int degMat)
        {
            float kx = ((float)xMat - matLeft) / matWidth;
            float ky = ((float)yMat - matTop) / matHeight;

            var tr = GetComponent<RectTransform>();

            float uiLocalX = tr.sizeDelta.x * kx; float uiLocalY = -tr.sizeDelta.y * ky;
            float rad = tr.eulerAngles.z * Mathf.PI / 180;

            float xUI = uiLocalX * Mathf.Cos(rad) - uiLocalY * Mathf.Sin(rad) + tr.anchoredPosition.x;
            float yUI = uiLocalX * Mathf.Sin(rad) + uiLocalY * Mathf.Cos(rad) + tr.anchoredPosition.y;

            float degUI = tr.eulerAngles.z - degMat;

            return (new Vector3(xUI, yUI, 0), new Vector3(0, 0, degUI));
        }

        public float GetCubeWidthUI()
        {
            float cubeWidthMat = Mat.DotPerM * CubeSimulator.WidthM;
            float UIPerDot = GetComponent<RectTransform>().sizeDelta.x / matWidth;
            return cubeWidthMat * UIPerDot;
        }
    }

}
