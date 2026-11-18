using OriginalKartRacer.Race;
using OriginalKartRacer.Replay;
using OriginalKartRacer.Vehicle;
using UnityEngine;

namespace OriginalKartRacer.UI
{
    /// <summary>
    /// Runtime arcade-racing HUD on a fixed 1920x1080 design canvas.
    /// The course map is a clean line drawing rather than an aerial RenderTexture.
    /// </summary>
    public sealed class DemoRuntimeHUD : MonoBehaviour
    {
        private const int DesignWidth = 1920;
        private const int DesignHeight = 1080;
        private const int VisibleCompletedLaps = 4;

        private Vector2[] courseMapPoints = new Vector2[0];
        private Vector3 mapCenter;
        private float mapSpan=1;
        private RacingPath path;

        private static readonly string[] RpmLabels = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };

        public RaceManager RaceManager;
        public RaceClock RaceClock;
        public KartController Kart;
        public DriftNitroSystem Nitro;
        public RacePositionBoard PositionBoard;
        public RuntimeMinimap Minimap; // Kept for scene compatibility; HUD no longer samples its texture.
        public ReplayTelemetry ReplayTelemetry;

        private readonly float[] recentLapTimes = new float[VisibleCompletedLaps];
        private readonly string[] recentLapTexts = new string[VisibleCompletedLaps];
        private int recentLapCount;
        private bool subscribed;

        private string countdown = string.Empty;
        private string raceStatus = string.Empty;
        private string totalTimeText = "0'00\"00";
        private string currentLapTimeText = "0'00\"00";
        private string lapText = "1/1";
        private string positionText = "1/1";
        private string speedText = "000";
        private string gearText = "N";

        private int cachedRaceCentiseconds = int.MinValue;
        private int cachedLapCentiseconds = int.MinValue;
        private int cachedLap = int.MinValue;
        private int cachedTotalLaps = int.MinValue;
        private int cachedPosition = int.MinValue;
        private int cachedRacerCount = int.MinValue;
        private int cachedSpeed = int.MinValue;

        private GUIStyle heading;
        private GUIStyle value;
        private GUIStyle small;
        private GUIStyle largeCenter;
        private GUIStyle rightHeading;
        private GUIStyle rightValue;
        private GUIStyle speedStyle;
        private GUIStyle rpmStyle;
        private GUIStyle finishStyle;
        private Texture2D gaugeFace;
        private Texture2D pixel;
        private Texture2D marker;

        private void OnEnable() => Subscribe();
        private void Start()
        {
            Subscribe();
            path=FindObjectOfType<RacingPath>();
            if(path==null||path.Count<2)return;
            var bounds=new Bounds(path.GetPoint(0),Vector3.zero);
            for(int i=1;i<path.Count;i++)bounds.Encapsulate(path.GetPoint(i));
            mapCenter=bounds.center;mapSpan=Mathf.Max(bounds.size.x,bounds.size.z)*1.08f;
            courseMapPoints=new Vector2[path.Count];
            for(int i=0;i<path.Count;i++)courseMapPoints[i]=WorldToMap(path.GetPoint(i));
        }
        private void OnDisable() => Unsubscribe();

        private void OnDestroy()
        {
            if (gaugeFace != null) Destroy(gaugeFace);
            if (pixel != null) Destroy(pixel);
            if (marker != null) Destroy(marker);
        }

        private void Subscribe()
        {
            if (subscribed || RaceManager == null)
                return;

            RaceManager.CountdownChanged += SetCountdown;
            RaceManager.RaceFinished += SetFinish;
            RaceManager.LapCompleted += RecordCompletedLap;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed || RaceManager == null)
                return;

            RaceManager.CountdownChanged -= SetCountdown;
            RaceManager.RaceFinished -= SetFinish;
            RaceManager.LapCompleted -= RecordCompletedLap;
            subscribed = false;
        }

        private void SetCountdown(string text) => countdown = text;
        private void SetFinish(float time) => raceStatus = "FINISH  " + FormatArcadeTime(time);

        private void RecordCompletedLap(float lapTime, float unusedBestLap)
        {
            int moveFrom = Mathf.Min(recentLapCount, VisibleCompletedLaps - 1);
            for (int i = moveFrom; i > 0; i--)
            {
                recentLapTimes[i] = recentLapTimes[i - 1];
                recentLapTexts[i] = recentLapTexts[i - 1];
            }

            recentLapTimes[0] = lapTime;
            recentLapTexts[0] = FormatArcadeTime(lapTime);
            recentLapCount = Mathf.Min(recentLapCount + 1, VisibleCompletedLaps);
        }

        private void EnsureResources()
        {
            if (pixel == null)
            {
                pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                pixel.SetPixel(0, 0, Color.white);
                pixel.Apply();
            }

            if (heading != null)
                return;

            Font font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            Color referenceRed = new Color(1f, 0f, 0f);
            heading = MakeStyle(font, 46, FontStyle.Bold, referenceRed, TextAnchor.UpperLeft);
            value = MakeStyle(font, 52, FontStyle.Bold, Color.white, TextAnchor.UpperLeft);
            small = MakeStyle(font, 22, FontStyle.Bold, Color.white, TextAnchor.UpperLeft);
            largeCenter = MakeStyle(font, 160, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            rightHeading = MakeStyle(font, 46, FontStyle.Bold, referenceRed, TextAnchor.UpperCenter);
            rightValue = MakeStyle(font, 52, FontStyle.Bold, Color.white, TextAnchor.UpperCenter);
            speedStyle = MakeStyle(font, 58, FontStyle.Bold, new Color(1f, 0.13f, 0.09f), TextAnchor.MiddleCenter);
            rpmStyle = MakeStyle(font, 16, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            finishStyle = MakeStyle(font, 68, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            gaugeFace = BuildGaugeFace(300);
            marker = BuildCircleMarker(32);
        }

        private static GUIStyle MakeStyle(Font font, int size, FontStyle style, Color color, TextAnchor anchor) => new GUIStyle
        {
            font = font,
            fontSize = size,
            fontStyle = style,
            alignment = anchor,
            normal = { textColor = color }
        };

        private void OnGUI()
        {
            EnsureResources();
            UpdateCachedTelemetry();

            float scale = Mathf.Min(Screen.width / (float)DesignWidth, Screen.height / (float)DesignHeight);
            Matrix4x4 originalMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);

            DrawTimeBlock();
            DrawProgressBlock();
            DrawMinimap();
            DrawSpeedometer();

            if (!string.IsNullOrEmpty(countdown))
                DrawShadowed(new Rect(0, 270, DesignWidth, 190), countdown, largeCenter);
            if (!string.IsNullOrEmpty(raceStatus) && !(ReplayTelemetry != null && ReplayTelemetry.IsPlaying))
            {
                DrawShadowed(new Rect(0, 330, DesignWidth, 100), raceStatus, finishStyle);
                DrawShadowed(new Rect(685, 445, 700, 45), "R  REPLAY     ENTER  RACE AGAIN", small);
            }
            if (ReplayTelemetry != null && ReplayTelemetry.IsPlaying)
                DrawShadowed(new Rect(875, 36, 200, 34), "REPLAY", small);
            if(Time.timeSinceLevelLoad<10f)
                DrawShadowed(new Rect(390,1010,1200,40),"WASD / ARROWS  DRIVE     SPACE  DRIFT     SHIFT  NITRO     BACKSPACE  RESET",small);

            GUI.matrix = originalMatrix;
        }

        private void UpdateCachedTelemetry()
        {
            bool replay=ReplayTelemetry!=null&&ReplayTelemetry.IsPlaying;
            ReplayFrame frame=replay?ReplayTelemetry.CurrentFrame:default;
            if (RaceClock != null)
            {
                CacheTime(replay?frame.totalTime:RaceClock.RaceTime, ref cachedRaceCentiseconds, ref totalTimeText);
                CacheTime(replay?frame.lapTime:RaceClock.CurrentLapTime, ref cachedLapCentiseconds, ref currentLapTimeText);
            }

            int currentLap = RaceManager == null ? 1 : RaceManager.CurrentLap;
            if(replay)currentLap=frame.lap;
            int totalLaps = RaceManager == null ? 1 : RaceManager.TotalLaps;
            if (currentLap != cachedLap || totalLaps != cachedTotalLaps)
            {
                cachedLap = currentLap;
                cachedTotalLaps = totalLaps;
                lapText = currentLap + "/" + totalLaps;
            }

            int position = PositionBoard == null || Kart == null ? 1 : PositionBoard.GetPosition(Kart);
            if(replay)position=Mathf.Max(1,frame.positionInRace);
            int racerCount = PositionBoard == null ? 1 : PositionBoard.RacerCount;
            if (position != cachedPosition || racerCount != cachedRacerCount)
            {
                cachedPosition = position;
                cachedRacerCount = racerCount;
                positionText = position + "/" + racerCount;
            }

            int roundedSpeed = Kart == null ? 0 : Mathf.RoundToInt(Kart.SpeedKph);
            if(replay)roundedSpeed=Mathf.RoundToInt(frame.speedKph);
            if (roundedSpeed != cachedSpeed)
            {
                cachedSpeed = roundedSpeed;
                speedText = roundedSpeed.ToString("000");
            }
            int gear=replay?frame.gear:(Kart==null?0:Kart.Gear);
            gearText=gear<0?"R":gear==0?"N":gear.ToString();
        }

        private static void CacheTime(float seconds, ref int cachedCentiseconds, ref string text)
        {
            int centiseconds = Mathf.Max(0, Mathf.FloorToInt(seconds * 1000f));
            if (centiseconds == cachedCentiseconds)
                return;

            cachedCentiseconds = centiseconds;
            text = FormatArcadeTimeFromCentiseconds(centiseconds);
        }

        private void DrawTimeBlock()
        {
            DrawShadowed(new Rect(50, 43, 360, 50), "Total Time", heading);
            DrawShadowed(new Rect(76, 96, 360, 62), totalTimeText, value);
            DrawShadowed(new Rect(50, 165, 360, 50), "Lap Time", heading);
            DrawShadowed(new Rect(76, 217, 360, 56), currentLapTimeText, value);
            if(RaceManager!=null&&!float.IsInfinity(RaceManager.BestLap))
                DrawShadowed(new Rect(52,560,400,35),"BEST  "+FormatArcadeTime(RaceManager.BestLap),small);

            int fastestIndex = GetFastestCompletedLapIndex();
            for (int i = 0; i < recentLapCount; i++)
            {
                GUIStyle rowStyle = i == fastestIndex ? heading : value;
                DrawShadowed(new Rect(76, 272 + i * 55, 360, 56), recentLapTexts[i], rowStyle);
            }
        }

        private int GetFastestCompletedLapIndex()
        {
            int fastestIndex = -1;
            float fastestTime = float.PositiveInfinity;
            for (int i = 0; i < recentLapCount; i++)
            {
                if (recentLapTimes[i] >= fastestTime)
                    continue;

                fastestTime = recentLapTimes[i];
                fastestIndex = i;
            }
            return fastestIndex;
        }

        private void DrawProgressBlock()
        {
            DrawShadowed(new Rect(1540, 43, 400, 50), "Lap", rightHeading);
            DrawShadowed(new Rect(1540, 96, 400, 62), lapText, rightValue);
            DrawShadowed(new Rect(1540, 165, 400, 50), "Position", rightHeading);
            DrawShadowed(new Rect(1540, 218, 400, 62), positionText, rightValue);
        }

        private void DrawMinimap()
        {
            const float x = 48f;
            const float y = 728f;
            const float width = 250f;
            const float height = 245f;

            // The dark outline preserves contrast without a square UI panel.
            DrawCourseMapLines(x, y, width, height, 14f, new Color(0f, 0f, 0f, 0.58f));
            DrawCourseMapLines(x, y, width, height, 9f, Color.white);

            if(PositionBoard!=null)
                foreach(var racer in PositionBoard.Racers)
                    if(racer!=null&&racer!=Kart)DrawMapMarker(ToMapPoint(WorldToMap(racer.transform.position),x,y,width,height),new Color(.1f,1,.28f));
            if(Kart!=null)DrawMapMarker(ToMapPoint(WorldToMap(Kart.transform.position),x,y,width,height),new Color(1,.12f,.08f));
        }

        private void DrawCourseMapLines(float x, float y, float width, float height, float thickness, Color color)
        {
            for (int i = 0; i < courseMapPoints.Length; i++)
            {
                Vector2 a = ToMapPoint(courseMapPoints[i], x, y, width, height);
                Vector2 b = ToMapPoint(courseMapPoints[(i + 1) % courseMapPoints.Length], x, y, width, height);
                DrawGuiLine(a, b, thickness, color);
            }
        }

        private static Vector2 ToMapPoint(Vector2 normalized, float x, float y, float width, float height) =>
            new Vector2(x + normalized.x * width, y + normalized.y * height);
        private Vector2 WorldToMap(Vector3 world) => new Vector2(.5f+(world.x-mapCenter.x)/mapSpan,.5f-(world.z-mapCenter.z)/mapSpan);

        private void DrawMapMarker(Vector2 point, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(new Rect(point.x - 7f, point.y - 7f, 14f, 14f), marker);
            GUI.color = previous;
        }

        private void DrawGuiLine(Vector2 a, Vector2 b, float thickness, Color color)
        {
            Vector2 direction = b - a;
            float length = direction.magnitude;
            if (length < 0.001f)
                return;

            Matrix4x4 previousMatrix = GUI.matrix;
            Color previousColor = GUI.color;
            GUI.color = color;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            GUIUtility.RotateAroundPivot(angle, a);
            GUI.DrawTexture(new Rect(a.x, a.y - thickness * 0.5f, length, thickness), pixel);
            GUI.matrix = previousMatrix;
            GUI.color = previousColor;
        }

        private void DrawSpeedometer()
        {
            float speed = Kart == null ? 0f : Kart.SpeedKph;
            bool replay=ReplayTelemetry!=null&&ReplayTelemetry.IsPlaying;
            if(replay)speed=ReplayTelemetry.CurrentFrame.speedKph;
            const float gaugeX = 1610f;
            const float gaugeY = 790f;
            const float gaugeSize = 250f;

            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(gaugeX, gaugeY, gaugeSize, gaugeSize), gaugeFace, ScaleMode.StretchToFill, true);
            DrawNeedle(new Vector2(gaugeX + gaugeSize * 0.5f, gaugeY + gaugeSize * 0.5f),
                -Mathf.Lerp(220f, -40f, Mathf.Clamp01((replay?ReplayTelemetry.CurrentFrame.engineRpm:(Kart==null?0:Kart.EngineRpm)) / 9000f)), 87f);
            DrawRpmLabels(gaugeX, gaugeY, gaugeSize);

            GUI.color = new Color(0f, 0f, 0f, 0.9f);
            GUI.DrawTexture(new Rect(1645, 720, 120, 64), pixel);
            GUI.DrawTexture(new Rect(1834, 720, 55, 64), pixel);
            GUI.color = Color.white;
            DrawShadowed(new Rect(1645, 718, 120, 64), speedText, speedStyle);
            DrawShadowed(new Rect(1768, 742, 68, 34), "km/h", small);
            DrawShadowed(new Rect(1834, 718, 55, 64), gearText, speedStyle);
            float charge=replay?ReplayTelemetry.CurrentFrame.nitroCharge:(Nitro==null?0:Nitro.Charge);
            GUI.color=new Color(.08f,.1f,.15f,.85f);GUI.DrawTexture(new Rect(1615,1050,240,10),pixel);
            GUI.color=Color.Lerp(new Color(1,.25f,.04f),new Color(1,.7f,.07f),charge/100);
            GUI.DrawTexture(new Rect(1615,1050,240*charge/100,10),pixel);GUI.color=Color.white;
        }

        private void DrawRpmLabels(float x, float y, float size)
        {
            Vector2 center = new Vector2(x + size * 0.5f, y + size * 0.5f);
            float radius = size * 0.34f;
            for (int i = 0; i < RpmLabels.Length; i++)
            {
                float angle = Mathf.Lerp(220f, -40f, i / 9f) * Mathf.Deg2Rad;
                Vector2 point = center + new Vector2(Mathf.Cos(angle), -Mathf.Sin(angle)) * radius;
                DrawShadowed(new Rect(point.x - 14f, point.y - 10f, 28f, 22f), RpmLabels[i], rpmStyle);
            }
            DrawShadowed(new Rect(center.x - 50f, center.y - 14f, 100f, 22f), "x1000rpm", rpmStyle);
        }

        private void DrawNeedle(Vector2 pivot, float angleDegrees, float length)
        {
            Matrix4x4 old = GUI.matrix;
            GUIUtility.RotateAroundPivot(angleDegrees, pivot);
            GUI.color = new Color(1f, 0.1f, 0.08f);
            GUI.DrawTexture(new Rect(pivot.x, pivot.y - 3f, length, 6f), pixel);
            GUI.color = Color.white;
            GUI.matrix = old;
        }

        private void DrawShadowed(Rect rect, string text, GUIStyle style)
        {
            Color original = style.normal.textColor;
            style.normal.textColor = new Color(0f, 0f, 0f, 0.75f);
            GUI.Label(new Rect(rect.x + 3f, rect.y + 3f, rect.width, rect.height), text, style);
            style.normal.textColor = original;
            GUI.Label(rect, text, style);
        }

        private static string FormatArcadeTime(float seconds) =>
            FormatArcadeTimeFromCentiseconds(Mathf.Max(0, Mathf.FloorToInt(seconds * 1000f)));

        private static string FormatArcadeTimeFromCentiseconds(int centiseconds)
        {
            int minutes = centiseconds / 60000;
            int remaining = centiseconds % 60000;
            return minutes.ToString("00") + ":" + (remaining / 1000).ToString("00") + "." + (remaining % 1000).ToString("000");
        }

        private static Texture2D BuildGaugeFace(int size)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float radius = size * 0.48f;
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                texture.SetPixel(x, y, Vector2.Distance(new Vector2(x, y), center) <= radius
                    ? new Color(0.015f, 0.02f, 0.027f, 0.96f) : Color.clear);

            for (int i = 0; i <= 18; i++)
            {
                float angle = Mathf.Lerp(220f, -40f, i / 18f) * Mathf.Deg2Rad;
                float tickLength = i % 3 == 0 ? 18f : 10f;
                Vector2 outer = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (radius - 9f);
                Vector2 inner = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (radius - 9f - tickLength);
                DrawTextureLine(texture, outer, inner, Color.white);
            }
            texture.Apply();
            return texture;
        }

        private static Texture2D BuildCircleMarker(int size)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float radius = (size - 1) * 0.5f;
            Vector2 center = new Vector2(radius, radius);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                texture.SetPixel(x, y, distance <= radius ? Color.white : Color.clear);
            }
            texture.Apply();
            return texture;
        }

        private static void DrawTextureLine(Texture2D texture, Vector2 a, Vector2 b, Color color)
        {
            int steps = Mathf.CeilToInt(Vector2.Distance(a, b));
            for (int i = 0; i <= steps; i++)
            {
                Vector2 point = Vector2.Lerp(a, b, i / (float)Mathf.Max(1, steps));
                int x = Mathf.RoundToInt(point.x);
                int y = Mathf.RoundToInt(point.y);
                if (x >= 0 && x < texture.width && y >= 0 && y < texture.height)
                    texture.SetPixel(x, y, color);
            }
        }
    }
}
