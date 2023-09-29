using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrappleBlock : MonoBehaviour
{
    public Color defaultColor, selectedColor;

    private SpriteRenderer block;
    private SpringJoint2D sprintJoint;
    private LineRenderer lineRenderer;

    private GameObject player;

    private void Start()
    {
        block = GetComponent<SpriteRenderer>();
        player = GameObject.FindGameObjectWithTag("Player");

        sprintJoint = player.GetComponent<SpringJoint2D>();
        sprintJoint.enabled = false;
        sprintJoint.connectedBody = null;

        lineRenderer = player.GetComponent<LineRenderer>();
        lineRenderer.enabled = false;
    }

    private void Update()
    {
        if (sprintJoint.enabled)
        {
            lineRenderer.SetPosition(0, player.transform.position);            
        }

        if (Input.GetMouseButtonDown(1))
        {
            sprintJoint.connectedBody = null;
            sprintJoint.enabled = false;
            lineRenderer.enabled = false;
        }

    }

    private void OnMouseOver()
    {
        block.color = selectedColor;

        if(Input.GetMouseButtonDown(0)) 
        {
            sprintJoint.enabled = true;
            sprintJoint.connectedBody = GetComponent<Rigidbody2D>();

            lineRenderer.enabled = true;
            lineRenderer.SetPosition(1, transform.position);
        }
    }

    private void OnMouseExit()
    {
        block.color = defaultColor;        
    }
}
