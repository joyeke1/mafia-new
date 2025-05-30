using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Photon.Pun;
using UnityEngine.UI;
using TMPro;

public class Player : MonoBehaviourPun

{
    // Start is called before the first frame update
    void Start()
    {
        if (photonView.IsMine)
        {
            Debug.Log("This is my player!");
            // Enable controls, camera follow, etc.
        }
        else
        {
            Debug.Log("This belongs to another player.");
            // Disable local-only components here
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    //     [PunRPC]
    // public void SetRole(string role)
    // {
    //     myRole = role;
    //     Debug.Log("You are: " + role);
    //     ShowRoleUI(role); // You can show a panel here
    // }

}


