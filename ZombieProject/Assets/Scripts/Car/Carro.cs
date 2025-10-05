#region Using Statements
using UnityEngine;
#endregion

public class Carro : MonoBehaviour
{
    #region Fields
    [Header("Rodas (Colliders)")]
    [SerializeField] WheelCollider frontLeft;
    [SerializeField] WheelCollider frontRight;
    [SerializeField] WheelCollider rearLeft;
    [SerializeField] WheelCollider rearRight;

    [Header("Rodas (Meshes Visuais)")]
    [SerializeField] Transform frontLeftMesh;
    [SerializeField] Transform frontRightMesh;
    [SerializeField] Transform rearLeftMesh;
    [SerializeField] Transform rearRightMesh;

    [Header("Volante")]
    [SerializeField] Transform steeringWheel;
    [SerializeField] float steeringWheelMaxAngle = 360f;

    [Header("Forças")]
    [SerializeField] float motorTorque = 2200f;
    [SerializeField] float maxSteerAngle = 30f;
    [SerializeField] float brakeTorque = 3500f;

    [Header("Estabilidade")]
    [SerializeField] float centerOfMassY = -0.2f;
    [SerializeField] float downforce = 50f;
    [SerializeField] float substepSpeedThreshold = 5f;
    [SerializeField] int substepsBelow = 12;
    [SerializeField] int substepsAbove = 15;

    [Header("Luzes")]
    [SerializeField] Light farolEsq;
    [SerializeField] Light farolDir;
    [SerializeField] float farolIntensityOn = 6f;
    [SerializeField] float farolRangeOn = 35f;
    [SerializeField] Light brakeLeft;
    [SerializeField] Light brakeRight;
    [SerializeField] float brakeIntensityOn = 8f;
    [SerializeField] float brakeRangeOn = 4f;

    [Header("Iluminação Ambiente")]
    [SerializeField] Light directionalLight;
    [SerializeField] float dayIntensity = 1.1f;
    [SerializeField] float nightIntensity = 0.15f;

    [Header("Câmeras")]
    [SerializeField] Camera cam3P;
    [SerializeField] Camera camCockpit;
    [SerializeField] Camera camRoda;

    float motorInput;
    float steerInput;
    bool braking;
    bool farolOn;
    bool night;
    Rigidbody rb;
    Quaternion steeringWheelBaseRot;
    #endregion

    #region Unity Methods
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb) rb.centerOfMass += new Vector3(0f, centerOfMassY, 0f);
        ConfigureSubsteps(frontLeft);
        ConfigureSubsteps(frontRight);
        ConfigureSubsteps(rearLeft);
        ConfigureSubsteps(rearRight);
        if (steeringWheel) steeringWheelBaseRot = steeringWheel.localRotation;
    }

    void Start()
    {
        SetCameras(1);
        SetFarol(false);
        SetBrakeLights(false);
        if (directionalLight) directionalLight.intensity = dayIntensity;
    }

    void Update()
    {
        motorInput = Input.GetAxis("Vertical");
        steerInput = Input.GetAxis("Horizontal");
        braking = Input.GetKey(KeyCode.Space);

        UpdateSteeringWheel();

        if (Input.GetKeyDown(KeyCode.Q)) ToggleDayNight();
        if (Input.GetKeyDown(KeyCode.E)) ToggleFarol();

        if (Input.GetKeyDown(KeyCode.Alpha1)) SetCameras(1);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SetCameras(2);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SetCameras(3);
    }

    void FixedUpdate()
    {
        ApplyDrive();
        ApplySteer();
        ApplyBrake(braking);
        UpdateWheelVisual(frontLeft, frontLeftMesh);
        UpdateWheelVisual(frontRight, frontRightMesh);
        UpdateWheelVisual(rearLeft, rearLeftMesh);
        UpdateWheelVisual(rearRight, rearRightMesh);
        ApplyDownforce();
    }
    #endregion

    #region Methods
    void ConfigureSubsteps(WheelCollider wc)
    {
        if (!wc) return;
        wc.ConfigureVehicleSubsteps(substepSpeedThreshold, substepsBelow, substepsAbove);
    }

    void ApplyDrive()
    {
        float torque = braking ? 0f : motorInput * motorTorque;
        frontLeft.motorTorque = torque;
        frontRight.motorTorque = torque;
    }

    void ApplySteer()
    {
        float angle = steerInput * maxSteerAngle;
        frontLeft.steerAngle = angle;
        frontRight.steerAngle = angle;
    }

    void ApplyBrake(bool isBraking)
    {
        float b = isBraking ? brakeTorque : 0f;
        frontLeft.brakeTorque = b;
        frontRight.brakeTorque = b;
        rearLeft.brakeTorque = b;
        rearRight.brakeTorque = b;
        SetBrakeLights(isBraking);
    }

    void UpdateWheelVisual(WheelCollider col, Transform mesh)
    {
        if (!col || !mesh) return;
        Vector3 pos; Quaternion rot;
        col.GetWorldPose(out pos, out rot);
        mesh.position = pos;
        mesh.rotation = rot;
    }

    void UpdateSteeringWheel()
    {
        if (!steeringWheel) return;
        float angle = steerInput * steeringWheelMaxAngle;
        steeringWheel.localRotation = steeringWheelBaseRot * Quaternion.Euler(0f, 0f, -angle);
    }

    void ApplyDownforce()
    {
        if (!rb) return;
        rb.AddForce(-transform.up * (downforce * rb.linearVelocity.magnitude));
    }

    void ToggleFarol()
    {
        farolOn = !farolOn;
        SetFarol(farolOn);
    }

    void SetFarol(bool on)
    {
        if (farolEsq)
        {
            farolEsq.enabled = on;
            farolEsq.intensity = on ? farolIntensityOn : 0f;
            farolEsq.range = on ? farolRangeOn : 0.01f;
        }
        if (farolDir)
        {
            farolDir.enabled = on;
            farolDir.intensity = on ? farolIntensityOn : 0f;
            farolDir.range = on ? farolRangeOn : 0.01f;
        }
    }

    void SetBrakeLights(bool on)
    {
        if (brakeLeft)
        {
            brakeLeft.enabled = on;
            brakeLeft.intensity = on ? brakeIntensityOn : 0f;
            brakeLeft.range = on ? brakeRangeOn : 0.01f;
        }
        if (brakeRight)
        {
            brakeRight.enabled = on;
            brakeRight.intensity = on ? brakeIntensityOn : 0f;
            brakeRight.range = on ? brakeRangeOn : 0.01f;
        }
    }

    void ToggleDayNight()
    {
        night = !night;
        if (directionalLight) directionalLight.intensity = night ? nightIntensity : dayIntensity;
    }

    void SetCameras(int index)
    {
        if (cam3P) cam3P.enabled = index == 1;
        if (camCockpit) camCockpit.enabled = index == 2;
        if (camRoda) camRoda.enabled = index == 3;
    }
    #endregion
}
