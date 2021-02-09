using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlantDynamicsDriver : MonoBehaviour
{
    [SerializeField]
    private float stiffness = 1;
    [SerializeField]
    float damping = 0.1f; // 0 is no damping, 1 is a lot, I think
    float valueThreshold = 0.01f;
    [SerializeField]
    float velocityThreshold = 0.01f;
    [SerializeField]
    Transform velocityTrackerTransform;


    [System.Serializable]
    struct CurvatureRendererPair
    {
        public MeshRenderer meshRenderer;
        public float curvature;
    };

    [SerializeField]
    CurvatureRendererPair[] meshRenderers;

    int velocityHash = Shader.PropertyToID("Velocity");
    Vector3 lastVelocityTrackerPosition;
    Vector3 currentValue;
    Vector3 currentVelocity = Vector3.zero;
    Vector3 targetValue;

    private void Start()
    {
        targetValue = velocityTrackerTransform.localPosition;
        currentValue = targetValue;
        lastVelocityTrackerPosition = velocityTrackerTransform.position;

        foreach (CurvatureRendererPair r in meshRenderers)
        {
            Material mat = r.meshRenderer.sharedMaterial;
            mat.SetFloat("Curvature", r.curvature);
            r.meshRenderer.sharedMaterial = new Material(mat);
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Calculate the velocity and do our mass spring system
        CalculateDynamics();

        // sphere cast up and see if we have an object to collide with
        ClampY();

        // Send the dynamics to the shader
        ApplyDynamics();
    }

    void CalculateDynamics()
    {
        // Reset world position
        velocityTrackerTransform.position = lastVelocityTrackerPosition;

        // Apply spring delta
        currentValue = velocityTrackerTransform.localPosition;

        float dampingFactor = Mathf.Max(0, 1 - damping * Time.deltaTime);
        Vector3 acceleration = (targetValue - currentValue) * stiffness * Time.deltaTime;
        currentVelocity = currentVelocity * dampingFactor + acceleration;
        currentValue += currentVelocity * Time.deltaTime;

        float deltaSize = (currentValue - targetValue).magnitude;
        if (deltaSize < valueThreshold && currentVelocity.magnitude < velocityThreshold)
        {
            currentValue = targetValue;
            currentVelocity = Vector3.zero;
        }

        velocityTrackerTransform.localPosition = currentValue;

        // Cache new reset position
        lastVelocityTrackerPosition = velocityTrackerTransform.position;
    }
    void ClampY()
    {
        RaycastHit hit;
        if (Physics.SphereCast(transform.position, 0.3f, transform.up, out hit, 8 * transform.localScale.x))
        {
            float newY = Mathf.Clamp(velocityTrackerTransform.localPosition.y, 0, hit.distance - 0.75f);

            velocityTrackerTransform.localPosition = new Vector3(
                velocityTrackerTransform.localPosition.x,
                newY,
                velocityTrackerTransform.localPosition.z);
        }
    }
    void ApplyDynamics()
    {
        foreach (CurvatureRendererPair r in meshRenderers)
        {
            Vector3 deltaV = velocityTrackerTransform.localPosition - targetValue;
            // add 0.01f to sign respective x because zero vector fails in shader
            r.meshRenderer.sharedMaterial.SetVector(velocityHash, deltaV + new Vector3(0.01f * Mathf.Sign(deltaV.x), 0, 0));
        }
    }
}
