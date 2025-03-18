using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using Singletons;
using UnityEngine;

public class PlayerCameraFollow : Singleton<PlayerCameraFollow>
{
    private CinemachineVirtualCamera playerCamera;
    private CinemachineFreeLook freeLookCamera;
    private void Awake()
    {
        //playerCamera = GetComponent<CinemachineVirtualCamera>();
        freeLookCamera = GetComponent<CinemachineFreeLook>();
    }

    public void FollowPlayer(Transform transform)
    {
        playerCamera.Follow = transform;
        // freeLookCamera.Follow = transform;
        // freeLookCamera.m_LookAt = transform;
    }
}
