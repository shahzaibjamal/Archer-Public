using System.Reflection;
using UnityEngine;
using UnityEngine.AI;

public class DebugMenu : MonoBehaviour
{
    private bool _isOpen = false;

    // Cached Reflection Info
    private FieldInfo _fPlayerMoveSpeed;
    private FieldInfo _fPlayerAttackInterval;
    private FieldInfo _fPlayerLockOnRadius;
    private FieldInfo _fCamYDist;
    private FieldInfo _fCamZDist;
    private FieldInfo _fCamAngle;

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
        InitializeReflection();
    }

    private void InitializeReflection()
    {
        _fPlayerMoveSpeed = typeof(PlayerController).GetField("_moveSpeed", BindingFlags.NonPublic | BindingFlags.Instance);
        _fPlayerAttackInterval = typeof(PlayerController).GetField("_attackInterval", BindingFlags.NonPublic | BindingFlags.Instance);
        _fPlayerLockOnRadius = typeof(PlayerController).GetField("_lockOnRadius", BindingFlags.NonPublic | BindingFlags.Instance);

        _fCamYDist = typeof(TopDownCameraFollow).GetField("yDistance", BindingFlags.NonPublic | BindingFlags.Instance);
        _fCamZDist = typeof(TopDownCameraFollow).GetField("zDistance", BindingFlags.NonPublic | BindingFlags.Instance);
        _fCamAngle = typeof(TopDownCameraFollow).GetField("angle", BindingFlags.NonPublic | BindingFlags.Instance);
    }

    private void Update()
    {
        // if (Input.GetKeyDown(KeyCode.Tab))
        // {
        //     _isOpen = !_isOpen;
        //     Cursor.visible = _isOpen;
        //     Cursor.lockState = _isOpen ? CursorLockMode.None : CursorLockMode.Confined;
        // }
    }

    private void OnGUI()
    {
        // Setup high-res scaling matrix (Target: 1080x1920 vertical canvas)
        float targetWidth = 1080f;
        float targetHeight = 1920f;
        float scaleX = Screen.width / targetWidth;
        float scaleY = Screen.height / targetHeight;
        
        // Preserve aspect ratio or just scale uniformly? Let's scale uniformly for a debug tool.
        Matrix4x4 oldMat = GUI.matrix;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scaleX, scaleY, 1f));

        // Adjust global font size for visibility on high-res mobile/portrait screens
        GUI.skin.label.fontSize = 24;
        GUI.skin.button.fontSize = 24;
        GUI.skin.horizontalSlider.fixedHeight = 40;
        GUI.skin.horizontalSliderThumb.fixedHeight = 50;
        GUI.skin.horizontalSliderThumb.fixedWidth = 50;

        if (!_isOpen)
        {
            if (GUI.Button(new Rect(20, 20, 250, 100), "DEBUG MENU")) _isOpen = true;
            GUI.matrix = oldMat;
            return;
        }

        DrawDebugWindow();
        GUI.matrix = oldMat;
    }

    private void DrawDebugWindow()
    {
        // Window is now relative to our 1080x1920 scale
        Rect windowRect = new Rect(50, 50, 980, 1500); 
        GUI.Box(windowRect, "DEBUG TUNER (REFLECTION)", GUI.skin.window);
        GUILayout.BeginArea(new Rect(windowRect.x + 30, windowRect.y + 100, windowRect.width - 60, windowRect.height - 150));

        GUIStyle headerStyle = new GUIStyle(GUI.skin.label);
        headerStyle.fontSize = 32;
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.normal.textColor = Color.cyan;

        // CAMERA (Dynamic Lookup)
        var cameraFollow = Object.FindObjectOfType<TopDownCameraFollow>();
        if (cameraFollow != null && _fCamYDist != null)
        {
            GUILayout.Label("CAMERA CONTROLS", headerStyle);
            GUILayout.Space(20);
            
            float yDist = (float)_fCamYDist.GetValue(cameraFollow);
            GUILayout.Label($"Height: {yDist:F1}");
            _fCamYDist.SetValue(cameraFollow, GUILayout.HorizontalSlider(yDist, 5f, 70f));
            GUILayout.Space(20);

            float angle = (float)_fCamAngle.GetValue(cameraFollow);
            GUILayout.Label($"Angle: {angle:F1}");
            _fCamAngle.SetValue(cameraFollow, GUILayout.HorizontalSlider(angle, 10f, 90f));
            GUILayout.Space(20);

            float zDist = (float)_fCamZDist.GetValue(cameraFollow);
            GUILayout.Label($"Distance Offset: {zDist:F1}");
            _fCamZDist.SetValue(cameraFollow, GUILayout.HorizontalSlider(zDist, -50f, 50f));
        }

        GUILayout.Space(60);

        // PLAYER (Dynamic Lookup)
        var player = Object.FindObjectOfType<PlayerController>();
        if (player != null && _fPlayerMoveSpeed != null)
        {
            GUILayout.Label("PLAYER STATS", headerStyle);
            GUILayout.Space(20);

            float mSpeed = (float)_fPlayerMoveSpeed.GetValue(player);
            GUILayout.Label($"Move Speed: {mSpeed:F1}");
            float newSpeed = GUILayout.HorizontalSlider(mSpeed, 1f, 40f);
            if (newSpeed != mSpeed)
            {
                _fPlayerMoveSpeed.SetValue(player, newSpeed);
                var agent = player.GetComponent<NavMeshAgent>();
                if (agent != null) agent.speed = newSpeed;
            }
            GUILayout.Space(20);

            float interval = (float)_fPlayerAttackInterval.GetValue(player);
            GUILayout.Label($"Attack Interval: {interval:F2}s");
            _fPlayerAttackInterval.SetValue(player, GUILayout.HorizontalSlider(interval, 0.05f, 4f));
            GUILayout.Space(20);

            float radius = (float)_fPlayerLockOnRadius.GetValue(player);
            GUILayout.Label($"Lock-on Radius: {radius:F1}");
            _fPlayerLockOnRadius.SetValue(player, GUILayout.HorizontalSlider(radius, 5f, 100f));
        }

        GUILayout.FlexibleSpace();
        if (GUILayout.Button("CLOSE", GUILayout.Height(100))) _isOpen = false;

        GUILayout.EndArea();
    }
}
