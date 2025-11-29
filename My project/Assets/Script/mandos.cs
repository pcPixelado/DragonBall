using UnityEngine;
using UnityEngine.XR;

public class MetaQuest3_Controllers_Proxy : MonoBehaviour
{
    [Header("Visuals (si vacío crea cubos por defecto)")]
    public GameObject leftControllerVisual;
    public GameObject rightControllerVisual;

    [Header("Prefabs de reemplazo")]
    public GameObject leftReplacementPrefab;
    public GameObject rightReplacementPrefab;

    [Header("Movimiento del jugador")]
    public Transform playerRoot;
    public Transform cameraTransform;
    public float moveSpeed = 1.5f;

    [Header("Ajustes visuales")]
    public bool makeInvisible = false;
    public float proxyScale = 0.05f;

    // Internos
    GameObject leftGO;
    GameObject rightGO;
    InputDevice leftDevice;
    InputDevice rightDevice;
    Vector3 leftOffset;
    Vector3 rightOffset;
    bool leftReplacing = false;
    bool rightReplacing = false;

    void Start()
    {
        EnsureVisuals();

        // Intentar detectar dispositivos varias veces
        InvokeRepeating(nameof(SetupDevices), 0f, 1f);
    }

    void SetupDevices()
    {
        var leftDevices = new System.Collections.Generic.List<InputDevice>();
        var rightDevices = new System.Collections.Generic.List<InputDevice>();

        InputDevices.GetDevicesAtXRNode(XRNode.LeftHand, leftDevices);
        InputDevices.GetDevicesAtXRNode(XRNode.RightHand, rightDevices);

        if (leftDevices.Count > 0) leftDevice = leftDevices[0];
        if (rightDevices.Count > 0) rightDevice = rightDevices[0];

        if (leftDevices.Count == 0) Debug.LogWarning("❌ No se detectó mando izquierdo.");
        if (rightDevices.Count == 0) Debug.LogWarning("❌ No se detectó mando derecho.");

        if (leftDevice.isValid && rightDevice.isValid)
        {
            CancelInvoke(nameof(SetupDevices));
            Debug.Log("✅ Mandos XR detectados correctamente.");
        }
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
        }

        ApplyVisibility(leftGO);
        ApplyVisibility(rightGO);

        if (playerRoot != null)
        {
            leftGO.transform.SetParent(playerRoot, true);
            rightGO.transform.SetParent(playerRoot, true);
        }
    }

    void ApplyVisibility(GameObject go)
    {
        if (go == null) return;
        var rends = go.GetComponentsInChildren<Renderer>();
        foreach (var r in rends) r.enabled = !makeInvisible;
    }

    void Update()
    {
        if (!leftDevice.isValid || !rightDevice.isValid)
            return; // esperar hasta que se detecten los mandos

        UpdateDeviceTransform(leftDevice, leftGO);
        UpdateDeviceTransform(rightDevice, rightGO);

        MovePlayer();

        CheckReplaceButton(leftDevice, ref leftGO, leftReplacementPrefab, ref leftOffset, ref leftReplacing);
        CheckReplaceButton(rightDevice, ref rightGO, rightReplacementPrefab, ref rightOffset, ref rightReplacing);
    }

    void UpdateDeviceTransform(InputDevice device, GameObject target)
    {
        if (device.isValid && target != null)
        {
            Vector3 pos;
            Quaternion rot;

            if (device.TryGetFeatureValue(CommonUsages.devicePosition, out pos))
                target.transform.localPosition = pos;

            if (device.TryGetFeatureValue(CommonUsages.deviceRotation, out rot))
                target.transform.localRotation = rot;
        }
    }

    void MovePlayer()
    {
        if (playerRoot == null || cameraTransform == null || !leftDevice.isValid) return;

        Vector2 inputAxis;
        if (leftDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out inputAxis))
        {
            Vector3 move = cameraTransform.forward * inputAxis.y + cameraTransform.right * inputAxis.x;
            move.y = 0f; // mantener el movimiento en plano horizontal
            playerRoot.position += move * moveSpeed * Time.deltaTime;
        }
    }

    void CheckReplaceButton(InputDevice device, ref GameObject currentGO, GameObject replacementPrefab, ref Vector3 offset, ref bool isReplacing)
    {
        if (!device.isValid || replacementPrefab == null) return;

        bool button1Pressed, button2Pressed;
        device.TryGetFeatureValue(CommonUsages.secondaryButton, out button1Pressed);
        device.TryGetFeatureValue(CommonUsages.menuButton, out button2Pressed);

        // Solo reemplazar si ambos botones están presionados y no se ha hecho antes
        if (button1Pressed && button2Pressed && !isReplacing)
        {
            GameObject newGO = Instantiate(replacementPrefab, currentGO.transform.position, currentGO.transform.rotation);
            newGO.transform.SetParent(playerRoot, true);

            offset = newGO.transform.position - playerRoot.position - GetDevicePosition(device);

            Destroy(currentGO);
            currentGO = newGO;

            ApplyVisibility(currentGO);
            isReplacing = true;

            Debug.Log("🔁 Prefab de mando reemplazado.");
        }
        else if (!button1Pressed || !button2Pressed)
        {
            isReplacing = false; // permitir siguiente reemplazo
        }
    }

    Vector3 GetDevicePosition(InputDevice device)
    {
        Vector3 pos;
        device.TryGetFeatureValue(CommonUsages.devicePosition, out pos);
        return pos;
    }

    public void SetInvisible(bool invisible)
    {
        makeInvisible = invisible;
        ApplyVisibility(leftGO);
        ApplyVisibility(rightGO);
    }
}
