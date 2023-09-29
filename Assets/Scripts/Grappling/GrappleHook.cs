using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrapplingHook : MonoBehaviour
{
    private Camera mainCamera;
    private SpringJoint2D springJoint;
    private LineRenderer lineRenderer;
    private SpriteRenderer sprite;

    private void Start()
    {    

        springJoint = GetComponent<SpringJoint2D>();
        springJoint.enabled = false;
        springJoint.connectedBody = null;

        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.enabled = false;

        sprite = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (springJoint.enabled)
        {
            lineRenderer.SetPosition(0, transform.position);            
        }
    }

    public void GrappleToObject(Transform grappleObject)
    {
        springJoint.enabled = true;
        springJoint.connectedBody = grappleObject.GetComponent<Rigidbody2D>();

        //lineRenderer.SetPosition(0, grappleObject.position);
        //lineRenderer.SetPosition(1, transform.position);
        //lineRenderer.enabled = true;       
    }

    public void ReleaseGrapple()
    {
        //Debug.Log("You have released the grappling hook");

        springJoint.connectedBody = null;
        springJoint.enabled = false;
        lineRenderer.enabled = false;
    }

    public void DisconnectJoint()
    {
        springJoint.connectedBody = null;
        springJoint.enabled = false;
    }

    public void FaceLeft()
    {
        if (springJoint.enabled)
            sprite.flipX = true;
    }

    public void FaceRight()
    {
        if (springJoint.enabled)
            sprite.flipX = false;
    }
}
