#region Using Statements
using UnityEngine;
#endregion

public class followPath : MonoBehaviour
{
    #region Fields
    [SerializeField] Transform[] allWayPoints;
    [SerializeField] float rotationSpeed = 6f;
    [SerializeField] float movementSpeed = 2f;
    [SerializeField] float arriveThreshold = 0.2f;
    [SerializeField] int currentTarget = 0;
    #endregion

    #region Unity Methods
    void Update()
    {
        if (allWayPoints == null || allWayPoints.Length == 0) return;
        Transform t = allWayPoints[currentTarget];
        Vector3 dir = (t.position - transform.position);
        Vector3 flat = new Vector3(dir.x, 0f, dir.z);
        if (flat.sqrMagnitude > 0.0001f)
        {
            Quaternion look = Quaternion.LookRotation(flat.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, look, rotationSpeed * 100f * Time.deltaTime);
        }
        transform.position += transform.forward * movementSpeed * Time.deltaTime;
        if (Vector3.Distance(transform.position, t.position) <= arriveThreshold) NextTarget();
    }
    #endregion

    #region Methods
    void NextTarget()
    {
        currentTarget++;
        if (currentTarget >= allWayPoints.Length) currentTarget = 0;
    }

    public void SetSpeed(float v)
    {
        movementSpeed = v;
    }

    public float GetSpeed()
    {
        return movementSpeed;
    }
    #endregion
}
