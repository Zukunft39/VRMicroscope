using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;
using Unity.XR.CoreUtils;
public class Move : TInstance<Move>
{
    public InputActionAsset inputActions;
    public ProgressControl progressControl;
    InputAction inputAction;
    public MoveTest moveTest;
    public XRRayInteractor interactor;//右
    public float speed;
    public GameObject cinema;
    private Vector3 direction=Vector3.zero;
    public Vector3 targetPos=Vector2.zero;

    [Header("Grip Teleport")]
    [SerializeField] private bool useInstantTeleportOnGrip = true;
    [SerializeField] private bool fallbackToAutoMoveWhenTeleportFails = false;
    [SerializeField] private float maxTeleportDistance = 30f;
    [SerializeField] private float maxTeleportSurfaceAngle = 60f;
    [SerializeField] private LayerMask teleportSurfaceMask = ~0;
    [SerializeField] private TeleportationProvider teleportationProvider;
    [SerializeField] private XROrigin xrOrigin;

    private bool isGripMovementBlocked;

    public void SetGripMovementBlocked(bool isBlocked)
    {
        isGripMovementBlocked = isBlocked;
        if (isBlocked && moveTest != null)
        {
            moveTest.isAutoNavigating = false;
        }
    }

    private void autoMove(InputAction.CallbackContext  context)
    {
        if (isGripMovementBlocked)
        {
            return;
        }

        if (useInstantTeleportOnGrip)
        {
            if (TryTeleportToReachableRayPoint() || !fallbackToAutoMoveWhenTeleportFails)
            {
                return;
            }
        }

        interactor.endPointDistance=1000f;
        moveTest.SetAutoMoveTarget(interactor.rayEndPoint);
    }
    private void Start() {
        EnsureTeleportReferences();
        inputActions.FindActionMap("Roaming").FindAction("AutoMove").started+=autoMove;
        inputActions.FindActionMap("XRI LeftHand Locomotion").FindAction("Move").performed += (context) =>
        {
            if (context.ReadValue<Vector2>() != Vector2.zero)
            {
                moveTest.isAutoNavigating = false;
            }
        };
    }
    private void FixedUpdate() {
        if(progressControl.isAutoMoving){
            transform.position=cinema.GetComponent<CinemachineBrain>().OutputCamera.transform.position;
        }
    }

    private void EnsureTeleportReferences()
    {
        if (xrOrigin == null)
        {
            xrOrigin = GetComponent<XROrigin>();
        }

        if (teleportationProvider == null)
        {
            teleportationProvider = FindObjectOfType<TeleportationProvider>();
        }
    }

    private bool TryTeleportToReachableRayPoint()
    {
        EnsureTeleportReferences();

        if (interactor == null || xrOrigin == null)
        {
            return false;
        }

        Vector3 destinationPosition;
        RaycastHit hitInfo;
        if (!TryGetTeleportDestination(out destinationPosition, out hitInfo))
        {
            return false;
        }

        Vector3 originPosition = xrOrigin.Origin != null ? xrOrigin.Origin.transform.position : transform.position;
        if ((destinationPosition - originPosition).sqrMagnitude > maxTeleportDistance * maxTeleportDistance)
        {
            return false;
        }

        if (hitInfo.collider != null && Vector3.Angle(hitInfo.normal, Vector3.up) > maxTeleportSurfaceAngle)
        {
            return false;
        }

        if (!TryQueueTeleport(destinationPosition))
        {
            MoveDirectlyTo(destinationPosition);
        }

        if (moveTest != null)
        {
            moveTest.isAutoNavigating = false;
        }

        return true;
    }

    private bool TryGetTeleportDestination(out Vector3 destinationPosition, out RaycastHit hitInfo)
    {
        destinationPosition = Vector3.zero;
        hitInfo = default;

        if (interactor.TryGetCurrent3DRaycastHit(out hitInfo))
        {
            destinationPosition = hitInfo.point;
            return true;
        }

        Vector3 rayEndPoint = interactor.rayEndPoint;
        if (rayEndPoint == Vector3.zero)
        {
            return false;
        }

        Vector3 rayOrigin = interactor.transform.position;
        if (Physics.Linecast(rayOrigin, rayEndPoint, out hitInfo, teleportSurfaceMask, QueryTriggerInteraction.Ignore))
        {
            destinationPosition = hitInfo.point;
            return true;
        }

        return false;
    }

    private bool TryQueueTeleport(Vector3 destinationPosition)
    {
        if (teleportationProvider == null || !teleportationProvider.isActiveAndEnabled)
        {
            return false;
        }

        TeleportRequest teleportRequest = new TeleportRequest
        {
            destinationPosition = destinationPosition,
            destinationRotation = xrOrigin.Origin != null ? xrOrigin.Origin.transform.rotation : transform.rotation,
            matchOrientation = MatchOrientation.None,
            requestTime = Time.time
        };

        return teleportationProvider.QueueTeleportRequest(teleportRequest);
    }

    private void MoveDirectlyTo(Vector3 destinationPosition)
    {
        Vector3 up = xrOrigin.Origin != null ? xrOrigin.Origin.transform.up : Vector3.up;
        Vector3 cameraDestination = destinationPosition + up * xrOrigin.CameraInOriginSpaceHeight;
        xrOrigin.MoveCameraToWorldLocation(cameraDestination);
    }
}
