using Photon.Pun;
using UnityEngine;

public class PropRigidbodyTransformView : MonoBehaviour, IPunObservable {


    Rigidbody rb;
    Vector3 latestPos;
    Quaternion latestRot;
    Vector3 velocity;
    Vector3 angularVelocity;
    [SerializeField]
    float lerpSpeed;

    bool valuesReceived = false;
    bool isCurrentlyAttached = false;


    void ResetRB() {
        rb = GetComponent<Rigidbody>();
    }

    private void Awake() {
        ResetRB();
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info) {
        if (!isCurrentlyAttached) {
            if (stream.IsWriting) {
                stream.SendNext(transform.position);
                stream.SendNext(transform.rotation);
                stream.SendNext(rb.velocity);
                stream.SendNext(rb.angularVelocity);
            } else {
                latestPos = (Vector3)stream.ReceiveNext();
                latestRot = (Quaternion)stream.ReceiveNext();
                velocity = (Vector3)stream.ReceiveNext();
                angularVelocity = (Vector3)stream.ReceiveNext();

                valuesReceived = true;
            }
        }
    }

    private void Update() {
        if (gameObject.transform.parent != null) {
            if (isCurrentlyAttached == false) {
                isCurrentlyAttached = true;
            }
        } else {
            if (isCurrentlyAttached == true) {
                isCurrentlyAttached = false;
            }
        }
    }


    void FixedUpdate()
    {
        if (!isCurrentlyAttached) {
            //Let's make sure our vars are up-to-date and gtg.
            if (rb == null) {
                ResetRB();
            }

            // Update Transform and Physics under same update cycle. If we put transform changes under regular update, this causes the movement to become in-organic.
            //Lerp rot and pos, but don't lerp physics unless an issue appears. Lerping physics, if done poorly, will result in rubber-banding.
            transform.position = Vector3.Lerp(transform.position, latestPos, Time.deltaTime * lerpSpeed);
            transform.rotation = Quaternion.Lerp(transform.rotation, latestRot, Time.deltaTime * lerpSpeed);
            rb.velocity = velocity;
            rb.angularVelocity = angularVelocity;
        }
    }
}
