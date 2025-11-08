using UnityEngine;
using UnityEngine.XR;

// MetaQuest3_Controllers_Proxy.cs
// Script para Unity 2022.3 que enlaza la posición/rotación de los mandos (XR) a GameObjects
// No necesita plugins externos: usa UnityEngine.XR (API integrada). Requiere que el sistema XR esté activo (OpenXR, etc.)
// - Si no asignas GameObjects, el script crea cubos por defecto.
// - Puedes elegir que los objetos sean invisibles (renderer desactivado) para usar "colisiones" u otros comportamientos.
// - Incluye una simulación simple en Editor para probar sin casco conectado.

public class MetaQuest3_Controllers_Proxy : MonoBehaviour
{
    [Header("Visuals (si vacío crea cubos por defecto)")]
    public GameObject leftControllerVisual;
    public GameObject rightControllerVisual;

    [Header("Ajustes")]
    public bool makeInvisible = false; // si true, desactiva el Renderer
    public float proxyScale = 0.05f; // tamaño de los cubos si se crean

    [Header("Simulación en Editor (pruebas sin casco)")]
    public bool simulateInEditor = true;
    public float simMoveSpeed = 0.5f;
    public float simRotateSpeed = 60f;

    // Internals
    GameObject leftGO;
    GameObject rightGO;

    InputDevice leftDevice;
    InputDevice rightDevice;

    void Start()
    {
        SetupDevices();
        EnsureVisuals();
    }

    void SetupDevices()
    {
        var leftDevices = new System.Collections.Generic.List<InputDevice>();
        var rightDevices = new System.Collections.Generic.List<InputDevice>();

        InputDevices.GetDevicesAtXRNode(XRNode.LeftHand, leftDevices);
        InputDevices.GetDevicesAtXRNode(XRNode.RightHand, rightDevices);

        if (leftDevices.Count > 0) leftDevice = leftDevices[0];
        if (rightDevices.Count > 0) rightDevice = rightDevices[0];
    }

    void EnsureVisuals()
    {
        // Left
        if (leftControllerVisual != null)
        {
            leftGO = leftControllerVisual;
        }
        else
        {
            leftGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftGO.name = "LeftControllerProxy";
            leftGO.transform.localScale = Vector3.one * proxyScale;
            // quitar collider si solo quieres visual
            // Destroy(leftGO.GetComponent<Collider>());
        }

        // Right
        if (rightControllerVisual != null)
        {
            rightGO = rightControllerVisual;
        }
        else
        {
            rightGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightGO.name = "RightControllerProxy";
            rightGO.transform.localScale = Vector3.one * proxyScale;
            // Destroy(rightGO.GetComponent<Collider>());
        }

        // Aplicar invisibilidad si corresponde
        ApplyVisibility(leftGO);
        ApplyVisibility(rightGO);
    }

    void ApplyVisibility(GameObject go)
    {
        if (go == null) return;
        var rend = go.GetComponent<Renderer>();
        if (rend != null) rend.enabled = !makeInvisible;
        else
        {
            // buscar renderers hijos
            var rends = go.GetComponentsInChildren<Renderer>();
            foreach (var r in rends) r.enabled = !makeInvisible;
        }
    }

    void Update()
    {
        // Si los dispositivos no están inicializados (por ejemplo cambio de escena o conexión tardía), reintentar
        if (!leftDevice.isValid || !rightDevice.isValid)
        {
            SetupDevices();
        }

        // Leer pos/rot de mandos
        UpdateDeviceTransform(leftDevice, leftGO);
        UpdateDeviceTransform(rightDevice, rightGO);

        // Simulación en Editor (si no hay casco o la lectura falla)
        if (simulateInEditor && Application.isEditor)
        {
            SimulateControls();
        }
    }

    void UpdateDeviceTransform(InputDevice device, GameObject target)
    {
        if (device.isValid && target != null)
        {
            Vector3 pos;
            Quaternion rot;
            bool hasPos = device.TryGetFeatureValue(CommonUsages.devicePosition, out pos);
            bool hasRot = device.TryGetFeatureValue(CommonUsages.deviceRotation, out rot);

            if (hasPos) target.transform.position = pos;
            if (hasRot) target.transform.rotation = rot;
        }
    }

    // Simple simulación: usa teclas para mover los proxies cuando estés en Editor
    void SimulateControls()
    {
        if (leftGO == null || rightGO == null) return;

        // Mover left con WASD + QE para altura
        Vector3 leftMove = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
        if (Input.GetKey(KeyCode.Q)) leftMove.y += 1f;
        if (Input.GetKey(KeyCode.E)) leftMove.y -= 1f;
        leftGO.transform.Translate(leftMove * simMoveSpeed * Time.deltaTime, Space.World);

        // Rotar left con mouse (mantener botón izquierdo)
        if (Input.GetMouseButton(0))
        {
            float rx = Input.GetAxis("Mouse X") * simRotateSpeed * Time.deltaTime;
            float ry = -Input.GetAxis("Mouse Y") * simRotateSpeed * Time.deltaTime;
            leftGO.transform.Rotate(Vector3.up, rx, Space.World);
            leftGO.transform.Rotate(Vector3.right, ry, Space.Self);
        }

        // Mover right con flechas + PageUp/PageDown
        Vector3 rightMove = Vector3.zero;
        if (Input.GetKey(KeyCode.LeftArrow)) rightMove.x -= 1f;
        if (Input.GetKey(KeyCode.RightArrow)) rightMove.x += 1f;
        if (Input.GetKey(KeyCode.UpArrow)) rightMove.z += 1f;
        if (Input.GetKey(KeyCode.DownArrow)) rightMove.z -= 1f;
        if (Input.GetKey(KeyCode.PageUp)) rightMove.y += 1f;
        if (Input.GetKey(KeyCode.PageDown)) rightMove.y -= 1f;
        rightGO.transform.Translate(rightMove * simMoveSpeed * Time.deltaTime, Space.World);

        // Rotar right con mouse botón derecho
        if (Input.GetMouseButton(1))
        {
            float rx = Input.GetAxis("Mouse X") * simRotateSpeed * Time.deltaTime;
            float ry = -Input.GetAxis("Mouse Y") * simRotateSpeed * Time.deltaTime;
            rightGO.transform.Rotate(Vector3.up, rx, Space.World);
            rightGO.transform.Rotate(Vector3.right, ry, Space.Self);
        }
    }

    // Método público para forzar que los proxies sean visibles/invisibles en runtime
    public void SetInvisible(bool invisible)
    {
        makeInvisible = invisible;
        ApplyVisibility(leftGO);
        ApplyVisibility(rightGO);
    }
}
