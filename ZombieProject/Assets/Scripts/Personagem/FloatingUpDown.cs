#region Using Statements
using UnityEngine;
#endregion

public class FloatingUpDown : MonoBehaviour
{
    #region Fields
    [SerializeField] float amplitude = 0.15f;
    [SerializeField] float speed = 1.2f;
    Vector3 startPos;
    #endregion

    #region Unity Methods
    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        float y = Mathf.Sin(Time.time * speed) * amplitude;
        transform.position = startPos + new Vector3(0f, y, 0f);
        transform.Rotate(0f, 40f * Time.deltaTime, 0f);
    }
    #endregion
}
