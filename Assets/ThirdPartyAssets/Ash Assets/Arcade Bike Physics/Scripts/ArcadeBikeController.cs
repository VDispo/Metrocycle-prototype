using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ArcadeBP
{
    public class ArcadeBikeController : MonoBehaviour
    {
        public enum groundCheck { rayCast, sphereCaste };
        public enum MovementMode { Velocity, AngularVelocity };
        public MovementMode movementMode;
        public groundCheck GroundCheck;
        public LayerMask drivableSurface;

        public float MaxSpeed, MaxReverseSpeed, accelaration, turn;
        public Rigidbody rb, carBody;

        [HideInInspector]
        public RaycastHit hit;
        public AnimationCurve frictionCurve;
        public AnimationCurve turnCurve;
        public AnimationCurve leanCurve;
        public PhysicMaterial frictionMaterial;
        [Header("Visuals")]
        public Transform BodyMesh;
        public Transform Handle;
        public Transform[] Wheels = new Transform[2];
        [HideInInspector]
        public Vector3 carVelocity;

        [Range(-70, 70)]
        public float BodyTilt;
        [Header("Audio settings")]
        public AudioSource engineSound;
        [Range(0, 1)]
        public float minPitch;
        [Range(1, 5)]
        public float MaxPitch;
        public AudioSource SkidSound;

        public float skidWidth;


        private float radius, horizontalInput, verticalInput;
        private Vector3 origin;

        [Header("Android Controls")]
        [SerializeField] private bool isAndroid = false;
        [SerializeField] private bool hasGyroscope = false;
        [SerializeField] private Slider throttleSlider;

        private void Start()
        {
            radius = rb.GetComponent<SphereCollider>().radius;
            if (movementMode == MovementMode.AngularVelocity)
            {
                Physics.defaultMaxAngularSpeed = 150;
            }
            rb.centerOfMass = Vector3.zero;

            isAndroid = Application.platform == RuntimePlatform.Android;
            isAndroid = true; // debug

            if (isAndroid && SystemInfo.supportsGyroscope)
            {
                Input.gyro.enabled = true;
                hasGyroscope = true;
            }
        }
        private void Update()
        {
            if (isAndroid) 
            {
                //turning input
                if (hasGyroscope) // use gyroscope if available
                {
                    horizontalInput += -Input.gyro.rotationRate.z * Time.deltaTime; // gyro.rotationrate outputs a DELTA or change in the rotation, hence we add here (also it is inverted by default hence the negative)
                    //Debug.LogWarning("using gyroscope: " + horizontalInput);
                }
                else // use accelerometer if no gyroscope
                {
                    horizontalInput = Input.acceleration.x; 
                    //Debug.LogWarning("using accelerometer: " + horizontalInput);
                }

                verticalInput = throttleSlider.value; //acceleration input
            }
            else
            {
                horizontalInput = Input.GetAxis("Horizontal"); //turning input
                verticalInput = Input.GetAxis("Vertical"); //acceleration input
            }
            Visuals();
            AudioManager();

        }
        public void AudioManager()
        {
            engineSound.pitch = Mathf.Lerp(minPitch, MaxPitch, Mathf.Abs(carVelocity.z) / MaxSpeed);
            if (Mathf.Abs(carVelocity.x) > 10 && grounded())
            {
                SkidSound.mute = false;
            }
            else
            {
                SkidSound.mute = true;
            }
        }

        public float getSpeed()
        {
            float speed = carVelocity.magnitude*3;    // HACK: *3 is just based on "feel" for now
            // Debug.Log("RB: " + rb.velocity.magnitude*3 + " carVelocity: " + speed);
            return speed;
        }

        void FixedUpdate()
        {
            carVelocity = carBody.transform.InverseTransformDirection(carBody.velocity);

            if (Mathf.Abs(carVelocity.x) > 0)
            {
                //changes friction according to sideways speed of car
                frictionMaterial.dynamicFriction = frictionCurve.Evaluate(Mathf.Abs(carVelocity.x / 100));
            }


            if (grounded())
            {
                //turnlogic
                float sign = Mathf.Sign(carVelocity.z);
                float TurnMultiplyer = turnCurve.Evaluate(carVelocity.magnitude / MaxSpeed);
                if (verticalInput > 0.1f || carVelocity.z > 1)
                {
                    carBody.AddTorque(Vector3.up * horizontalInput * sign * turn * 10 * TurnMultiplyer);
                }
                else if (verticalInput < -0.1f || carVelocity.z < -1)
                {
                    carBody.AddTorque(Vector3.up * horizontalInput * sign * turn * 10 * TurnMultiplyer);
                }

                //brakelogic
                if (Input.GetAxis("Jump") > 0.1f)
                {
                    // rb.constraints = RigidbodyConstraints.FreezeRotationX;
                }
                else
                {
                    rb.constraints = RigidbodyConstraints.None;
                }

                //accelaration logic

                if (movementMode == MovementMode.AngularVelocity)
                {
                    if (Mathf.Abs(verticalInput) > 0.1f)
                    {
                        rb.angularVelocity = Vector3.Lerp(rb.angularVelocity, carBody.transform.right * verticalInput * MaxSpeed / radius, accelaration * Time.deltaTime);
                    }
                }
                else if (movementMode == MovementMode.Velocity)
                {
                    float accel = accelaration;
                    float targetSpeed = MaxSpeed;
                    bool hasVerticalInput = Mathf.Abs(verticalInput) > 0.1f;
                    bool isPressingBackward = verticalInput < -0.1f && hasVerticalInput;
                    bool isBikeReversing = Vector3.Dot(carBody.transform.forward, carBody.velocity.normalized) < -0.1f
                                            && getSpeed() > 1f;

                    if (isPressingBackward && isBikeReversing) {
                        // Debug.Log("Slow Reverse");
                        // when bike is going in reverse, limit speed
                        targetSpeed = MaxReverseSpeed;
                    } else if ((Input.GetAxis("Jump") > 0.1f) && (getSpeed() <= MaxReverseSpeed)) {
                        rb.velocity = new Vector3(0, 0, 0);
                        // Debug.Log("Full stop");
                    } else if (!isBikeReversing && (isPressingBackward || (Input.GetAxis("Jump") > 0.1f))) {
                        // Debug.Log("BRAKE");
                        // use half acceleration (braking is "slower" than accelarating)
                        accel = accelaration / 2;
                        verticalInput = -1f;
                        hasVerticalInput = true;
                    }

                    if (hasVerticalInput)
                    {
                        rb.velocity = Vector3.Lerp(rb.velocity, carBody.transform.forward * verticalInput * targetSpeed, accel / 10 * Time.deltaTime);
                    }
                }

                //body tilt
                carBody.MoveRotation(Quaternion.Slerp(carBody.rotation, Quaternion.FromToRotation(carBody.transform.up, hit.normal) * carBody.transform.rotation, 0.09f));
            }
            else
            {
                carBody.MoveRotation(Quaternion.Slerp(carBody.rotation, Quaternion.FromToRotation(carBody.transform.up, Vector3.up) * carBody.transform.rotation, 0.02f));
            }

        }
        public void Visuals()
        {
            Handle.localRotation = Quaternion.Slerp(Handle.localRotation, Quaternion.Euler(Handle.localRotation.eulerAngles.x,
                                   20 * horizontalInput, Handle.localRotation.eulerAngles.z), 0.1f);

            Wheels[0].localRotation = rb.transform.localRotation;
            Wheels[1].localRotation = rb.transform.localRotation;

            //Body
            if (carVelocity.z > 1)
            {
                BodyMesh.localRotation = Quaternion.Slerp(BodyMesh.localRotation, Quaternion.Euler(0,
                                   BodyMesh.localRotation.eulerAngles.y, BodyTilt * horizontalInput * leanCurve.Evaluate(carVelocity.z / MaxSpeed)), 0.02f);
            }
            else
            {
                BodyMesh.localRotation = Quaternion.Slerp(BodyMesh.localRotation, Quaternion.Euler(0, 0, 0), 0.02f);
            }



        }

        public bool grounded() //checks for if vehicle is grounded or not
        {
            origin = rb.position + rb.GetComponent<SphereCollider>().radius * Vector3.up;
            var direction = -transform.up;
            var maxdistance = rb.GetComponent<SphereCollider>().radius + 0.2f;

            if (GroundCheck == groundCheck.rayCast)
            {
                if (Physics.Raycast(rb.position, Vector3.down, out hit, maxdistance, drivableSurface))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            else if (GroundCheck == groundCheck.sphereCaste)
            {
                if (Physics.SphereCast(origin, radius + 0.1f, direction, out hit, maxdistance, drivableSurface))
                {
                    return true;

                }
                else
                {
                    return false;
                }
            }
            else { return false; }
        }

        private void OnDrawGizmos()
        {
            //debug gizmos
            radius = rb.GetComponent<SphereCollider>().radius;
            float width = 0.02f;
            if (!Application.isPlaying)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(rb.transform.position + ((radius + width) * Vector3.down), new Vector3(2 * radius, 2 * width, 4 * radius));
                if (GetComponent<BoxCollider>())
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireCube(transform.position, GetComponent<BoxCollider>().size);
                }
                if (GetComponent<CapsuleCollider>())
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireCube(transform.position, GetComponent<CapsuleCollider>().bounds.size);
                }

            }

        }

    }
}
