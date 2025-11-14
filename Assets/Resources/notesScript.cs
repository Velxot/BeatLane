using UnityEngine;

public class notes : MonoBehaviour
{
    // ノーツの移動速度
    public float notesSpeed = 1.0f;

    void Update()
    {
        // ノーツを前方（-Z方向）に移動させる
        transform.position -= transform.forward * Time.deltaTime * notesSpeed;
    }
}