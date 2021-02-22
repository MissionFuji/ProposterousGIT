using Photon.Pun;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class PropSpawner : MonoBehaviour
{

    //These spawners are to be set manually and simply instatiated by proxy via map prefab.

    [SerializeField]
    private List<GameObject> possibleProps = new List<GameObject>();


    private GameplayController gc;
    private PhotonView gcpv;


    private void Awake() {
        gc = GameObject.FindGameObjectWithTag("GameplayController").GetComponent<GameplayController>(); //Set reference of our GameController.
        gcpv = gc.gameObject.GetPhotonView();

        if (gc != null && gcpv != null) {

        } else {
            Debug.LogError("Couldn't reference our GameController or its PhotonView?");
        }
    }

    private void Start() {
        if (PhotonNetwork.IsMasterClient) {
            object[] instanceData = new object[1];
            instanceData[0] = 0;
            int r = Random.Range(0, possibleProps.Count);
            GameObject newNetworkProp = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", possibleProps[r].name), gameObject.transform.position, gameObject.transform.rotation, 0, instanceData);
        }
    }



}
