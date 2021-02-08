using UnityEngine;

public class CameraTarController : MonoBehaviour {

    private GameObject ourPlayer;
    [SerializeField]
    private float lerpSpeed;
    [SerializeField]
    private float offset;
    [SerializeField]
    private bool lerpMovement = false;

    
    public void SetCamFollowToPlayer(GameObject ply) {
        ourPlayer = ply;
    }

    void Update()
    {
        if (ourPlayer != null) {
            if (lerpMovement) {
                gameObject.transform.position = Vector3.Lerp(gameObject.transform.position, ourPlayer.transform.position + new Vector3(0, offset, 0), Time.smoothDeltaTime * lerpSpeed);
            } else {
                gameObject.transform.position = ourPlayer.transform.position + new Vector3(0, offset, 0);
            }
        }
    }
}
