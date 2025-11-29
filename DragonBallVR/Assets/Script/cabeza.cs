using UnityEngine;

public class Cabeza : MonoBehaviour
{
    public float moveSpeed = 0.5f;
    public float rotateSpeed = 60f;

    void Update()
    {
        Vector3 move = Vector3.zero;
        if (Input.GetKey(KeyCode.I)) move.z += 1f;
        if (Input.GetKey(KeyCode.K)) move.z -= 1f;
        if (Input.GetKey(KeyCode.J)) move.x -= 1f;
        if (Input.GetKey(KeyCode.L)) move.x += 1f;
        if (Input.GetKey(KeyCode.U)) move.y += 1f;
        if (Input.GetKey(KeyCode.O)) move.y -= 1f;

        transform.Translate(move * moveSpeed * Time.deltaTime, Space.World);

        if (Input.GetMouseButton(2)) // botón central del ratón
        {
            float rx = Input.GetAxis("Mouse X") * rotateSpeed * Time.deltaTime;
            float ry = -Input.GetAxis("Mouse Y") * rotateSpeed * Time.deltaTime;
            transform.Rotate(Vector3.up, rx, Space.World);
            transform.Rotate(Vector3.right, ry, Space.Self);
        }
    }
}
