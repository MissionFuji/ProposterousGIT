using Photon.Pun;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class PropSpawner : MonoBehaviour
{

    //These spawners are to be set manually and simply instatiated by proxy via map prefab.

    [SerializeField]
    private List<GameObject> possibleProps = new List<GameObject>();
    [SerializeField]
    private bool delaySpawn = false;
    [SerializeField]
    private float delayTime = 0.5f;

    private GameplayController gController;
    private PhotonView gcpv;


    private void Awake() {
        gController = GameObject.FindGameObjectWithTag("GameplayController").GetComponent<GameplayController>(); //Set reference of our GameController.
        gcpv = gController.gameObject.GetPhotonView();

        if (gController != null && gcpv != null) {

        } else {
            Debug.LogError("Couldn't reference our GameController or its PhotonView?");
        }
    }

    private void Start() {
        if (PhotonNetwork.IsMasterClient) {
            if (!delaySpawn) {
                object[] instanceData = new object[1];
                instanceData[0] = 0;
                int r = Random.Range(0, possibleProps.Count);
                GameObject newNetworkProp = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", possibleProps[r].name), gameObject.transform.position, gameObject.transform.rotation, 0, instanceData);
                //We add our newly spawned prop to our list of props to be destroy on map-switch.
                gController.TellMasterClientToAddPropToDestroyList(newNetworkProp);
            } else {
                Invoke("DelaySpawn", delayTime);
            }
        }
    }

    private void DelaySpawn() {
        object[] instanceData = new object[1];
        instanceData[0] = 0;
        int r = Random.Range(0, possibleProps.Count);
        GameObject newNetworkProp = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", possibleProps[r].name), gameObject.transform.position, gameObject.transform.rotation, 0, instanceData);
        //We add our newly spawned prop to our list of props to be destroy on map-switch.
        gController.TellMasterClientToAddPropToDestroyList(newNetworkProp);
    }



}
