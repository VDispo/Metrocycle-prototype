using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movement : MonoBehaviour
{
    Vector3 Vec;
    private Rigidbody rigid;
    //private float TurnSpeed = 2f; // commented out as it is unused [Unity gives warning]
    // Start is called before the first frame update
    void Start()
    {
        rigid = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {

        Vec = transform.localPosition;
        Vec.y += Input.GetAxis("Jump") * Time.fixedDeltaTime * 20;
        Vec.x += Input.GetAxis("Horizontal") * Time.fixedDeltaTime * 20;
        Vec.z += Input.GetAxis("Vertical") * Time.fixedDeltaTime * 20;
        rigid.MovePosition(Vec);
    }
}
