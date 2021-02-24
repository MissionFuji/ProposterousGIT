using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class MapProperties : MonoBehaviour, IPunInstantiateMagicCallback {

    public List<GameObject> seekerSpawnPointList = new List<GameObject>();
    public List<GameObject> propSpawnPointList = new List<GameObject>();
    public GameObject seekerDoor = null;

    void IPunInstantiateMagicCallback.OnPhotonInstantiate(PhotonMessageInfo info) {

        object[] receivedListData = info.photonView.InstantiationData; //pull out data.
        //Start unpacking.
        int[] seekerList = (int[])receivedListData[0];
        int[] propList = (int[])receivedListData[1];

        int myID = -1;

        if (myID == -1) {
            foreach (int seekerID in seekerList) {
                PhotonView seekerPVID = PhotonView.Find(seekerID);
                if (seekerPVID.IsMine) {
                    myID = seekerID;
                    seekerPVID.gameObject.transform.position = seekerSpawnPointList[Random.Range(0, seekerSpawnPointList.Count - 1)].transform.position;
                }
            }
        }

        if (myID == -1) {
            foreach (int propID in propList) {
                PhotonView propPVID = PhotonView.Find(propID);
                if (propPVID.IsMine) {
                    myID = propID;
                    propPVID.gameObject.transform.position = propSpawnPointList[Random.Range(0, propSpawnPointList.Count - 1)].transform.position;
                }
            }
        }

        if (myID == -1) {
            Debug.LogError("myID was never found, and position to a new spawn was never set!");
        }

    }
}
