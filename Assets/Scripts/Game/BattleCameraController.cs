using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Switches from the map overview to the fixed-angle orthographic player camera.
/// Uses Unity's Cinemachine package for camera tracking only, never for enemy navigation.
/// API reference: https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/CinemachineFollow.html
/// </summary>
public sealed class BattleCameraController : MonoBehaviour
{
    [SerializeField] private CinemachineCamera overviewCamera;
    [SerializeField] private CinemachineCamera playerCamera;

    public void ShowBattle(Transform player)
    {
        playerCamera.Follow = player;
        playerCamera.PreviousStateIsValid = false;
        overviewCamera.Priority = 0;
        playerCamera.Priority = 20;
    }

    public void ShowMenu()
    {
        playerCamera.Priority = 0;
        overviewCamera.Priority = 20;
    }

    private void Start() => ShowMenu();
}
