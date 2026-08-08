using System.Runtime.CompilerServices;
using Unity.MP_FPS;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem.Users;

// Partial salvage from the Netcode-for-Entities version, not a straight port. Movement math (jump/
// gravity/grounding/rotation-smoothing) carries over unchanged and still runs on built-in PhysX.
// Everything tied to ghost prediction, the ECS command stream, and shooting/animation was cut —
// see unpaid_interns/ngo_steam_migration_status.md. This is host-authoritative with client-side
// interpolation (via NetworkTransform, added at the scene-wiring step): the owner sends input to the
// server each frame, the server is the only one that moves the CharacterController, and everyone else
// sees the replicated, interpolated result. No rollback prediction.
[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : NetworkBehaviour
{
    private const float k_ResetMovementAdjustEpsilon = 1e-06f;

    public enum MovementType
    {
        Standing = 0,
        Jumping,
        Falling,
    }

    public struct ControllerState
    {
        public enum StateFlag
        {
            Jump = 1 << 0,
            Fall = 1 << 1,
            Land = 1 << 2,
            Shoot = 1 << 3,
            IsReloading = 1 << 4,
            IsHit = 1 << 5,
            JumpTrigger = 1 << 6,
            LandTrigger = 1 << 7
        }

        // booleans
        public uint StateFlags;

        public bool Jump
        {
            get => (StateFlags & (uint)StateFlag.Jump) != 0;
            set => SetFlag(StateFlag.Jump, value);
        }

        public bool Fall
        {
            get => (StateFlags & (uint)StateFlag.Fall) != 0;
            set => SetFlag(StateFlag.Fall, value);
        }

        public bool Land
        {
            get => (StateFlags & (uint)StateFlag.Land) != 0;
            set => SetFlag(StateFlag.Land, value);
        }

        public bool Shoot
        {
            get => (StateFlags & (uint)StateFlag.Shoot) != 0;
            set => SetFlag(StateFlag.Shoot, value);
        }

        public bool IsReloadingState
        {
            get => (StateFlags & (uint)StateFlag.IsReloading) != 0;
            set => SetFlag(StateFlag.IsReloading, value);
        }

        public bool IsHit
        {
            get => (StateFlags & (uint)StateFlag.IsHit) != 0;
            set => SetFlag(StateFlag.IsHit, value);
        }

        public bool JumpTriggered
        {
            get => (StateFlags & (uint)StateFlag.JumpTrigger) != 0;
            set => SetFlag(StateFlag.JumpTrigger, value);
        }

        public bool LandTriggered
        {
            get => (StateFlags & (uint)StateFlag.LandTrigger) != 0;
            set => SetFlag(StateFlag.LandTrigger, value);
        }

        public quaternion CurrentRotation;
        public float3 CurrentPosition;
        public float3 GroundNormal;

        public float3 MovementRequest;
        public MovementType MovementType;
        public MovementType PreviousMovementType;
        public float TimeInState;

        public float YawDegrees;
        public float PitchDegrees;
        public float MovementSpeed;
        public float JumpFallSpeed;
        public float AnimatorTargetSpeed;
        public float AnimatorTargetSpeedChangeRate;
        public float JumpTimeoutDelta;
        public float FallTimeoutDelta;
        public float FallHeight;

        public float RotationVelocity;

        public float3 AnimatorMotion;
        public float AnimatorMotionChangeRate;
        public float AnimatorSmoothedMotionX;
        public float AnimatorMotionSpeed;

        public float TeleportFreeze;

        private void SetFlag(StateFlag flag, bool set)
        {
            if (set)
            {
                StateFlags |= (uint)flag;
            }
            else
            {
                StateFlags &= ~(uint)flag;
            }
        }

        public void Init(in float3 worldPosition, in quaternion worldRotation)
        {
            CurrentPosition = worldPosition;
            CurrentRotation = worldRotation;

            Quaternion rot = worldRotation;
            PitchDegrees = rot.eulerAngles.y;
        }
    }

    public struct ControllerConsts
    {
        public struct StateConsts
        {
            public float Speed;
            public float SpeedChangeRate;
            public float RotationSmoothTime;
            public float LandingSpeedMult;
            public float AnimationMotionScale;
        }

        public StateConsts Walk;
        public StateConsts Sprint;

        public float JumpHeight;
        public float Gravity;
        public float StandingFallSpeed;
        public float JumpTimeout;
        public float FallTimeout;
        public float LandingTimeout;
        public float LandingAnimTimeout;
        public float StateChangeSafetyTimeout;
        public float GroundedOffset;
        public LayerMask GroundLayers;
        public float TerminalVelocity;
    }

    [field: Header("Cinemachine")]
    [field: SerializeField,
            Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
    public GameObject CinemachineCameraTarget { get; private set; }

    [SerializeField, Tooltip("A small offset for the grounded spherecast. Should be a small positive value.")]
    private float GroundedOffset = 0.2f;

    public PhysicsMaterial GroundPhysicsMaterial { get; private set; }

    [field: SerializeField] public Vector3 ControllerOffset { get; private set; } = new Vector3(0f, 0f, 0f);

    [Header("Movement Tuning")]
    [SerializeField]
    private ControllerConsts m_Consts = new ControllerConsts
    {
        Walk = new ControllerConsts.StateConsts
        {
            Speed = 5f,
            SpeedChangeRate = 20f,
            RotationSmoothTime = 0.12f,
            LandingSpeedMult = 1f,
            AnimationMotionScale = 0.4f,
        },
        Sprint = new ControllerConsts.StateConsts
        {
            Speed = 8f,
            SpeedChangeRate = 20f,
            RotationSmoothTime = 0.12f,
            LandingSpeedMult = 1f,
            AnimationMotionScale = 0.4f,
        },
        JumpHeight = 1.2f,
        Gravity = -15f,
        StandingFallSpeed = -2f,
        JumpTimeout = 0.1f,
        FallTimeout = 0.15f,
        LandingTimeout = 0.2f,
        LandingAnimTimeout = 0.2f,
        StateChangeSafetyTimeout = 0.5f,
        GroundedOffset = 0.2f,
        GroundLayers = ~0,
        TerminalVelocity = -53f,
    };

    private ControllerState m_State;
    private PlayerInput m_LatestInput;
    private float2 m_AccumulatedLook;

    private CharacterController m_Controller;
    public CharacterController CharacterController => m_Controller;

    private Camera m_PlayerCamera;
    public Camera GetPlayerCamera() => m_PlayerCamera;

    private const int k_NumPhysicsResults = 8;
    private readonly RaycastHit[] m_GroundCheckRaycastResults = new RaycastHit[k_NumPhysicsResults];

    private const float k_DefaultSpeedChange = 20f;

    private static readonly float3 k_UpVector = math.up();

    public float CachedJumpFallSpeed { get; private set; }
    public float CachedFallHeight { get; private set; }

#if UNITY_EDITOR || DEBUG
    private MovementType m_PrevMovementType;
#endif

    private void Awake()
    {
        TryGetComponent(out m_Controller);
        Debug.Assert(m_Controller, "[FIRSTPERSONCONTROLLER] Player has no CharacterController component");
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            m_State.Init(transform.position, transform.rotation);
        }

        if (IsOwner)
        {
            m_PlayerCamera = Camera.main;
            Utils.SetCursorVisible(false);
        }
    }

    private void Update()
    {
        if (IsOwner)
        {
            SampleAndSendInput();
        }

        if (IsServer)
        {
            var deltaTime = Time.deltaTime;
            var accumulatedMovement = float3.zero;
            AccumulateMovement(ref m_State, ref accumulatedMovement, in m_LatestInput, in m_Consts, deltaTime);
            ApplyMovementUpdate(ref m_State, in m_Consts, in accumulatedMovement, deltaTime);
        }
    }

    private void SampleAndSendInput()
    {
        var user = InputSystemManager.GetFirstInputUser();
        if (!user.valid)
            return;

        var controls = (InputSystem_Actions)user.actions;

        var input = new PlayerInput
        {
            MoveInput = controls.Player.Move.ReadValue<Vector2>(),
            Jump = controls.Player.Jump.triggered,
        };

        // matches the sensitivity/clamp previously applied in ClientInputReaderSystem
        const float sensitivity = 3.7f;
        float2 lookDelta = controls.Player.LookDelta.ReadValue<Vector2>() * sensitivity;

        m_AccumulatedLook.x += lookDelta.x;
        m_AccumulatedLook.y = math.clamp(m_AccumulatedLook.y - lookDelta.y, -85f, 85f);
        input.LookYawPitchDegrees = m_AccumulatedLook;

        SubmitInputServerRpc(input);
    }

    [ServerRpc]
    private void SubmitInputServerRpc(PlayerInput input)
    {
        m_LatestInput = input;
    }

    public void SetExcludeLayers(LayerMask excludeLayers)
    {
        m_Controller.excludeLayers = excludeLayers;
    }

    public void ApplyMovementUpdate(ref ControllerState state, in ControllerConsts consts,
        in float3 accumulatedMovement, float deltaTime)
    {
        ApplyMove(ref state, consts, accumulatedMovement, deltaTime);
        GroundedCheck(ref state, consts);

        // cache latest values for access outside of entity data
        CachedJumpFallSpeed = state.JumpFallSpeed;
        CachedFallHeight = state.FallHeight;
    }

    private static void SetMovementType(ref ControllerState state, MovementType type)
    {
        if (state.MovementType != type)
        {
            bool wasUpdatingFallHeight = ShouldUpdateFallHeight(state.MovementType);

            state.PreviousMovementType = state.MovementType;
            state.MovementType = type;
            state.TimeInState = 0;

            if (!wasUpdatingFallHeight && ShouldUpdateFallHeight(type))
            {
                state.FallHeight = 0f;
                state.FallTimeoutDelta = float.MaxValue;
                state.Fall = true;
            }
        }
    }

    private static bool ShouldUpdateFallHeight(MovementType movementType)
    {
        return movementType == MovementType.Falling;
    }

    private struct GroundCollisionVariables
    {
        private RaycastHit m_ClosestHit;
        private RaycastHit m_FlattestHit;

        private readonly float m_FlattestHitDot;

        public Vector3 FlattestHitPoint => m_FlattestHit.point;
        public Vector3 FlattestHitNormal => m_FlattestHit.normal;
        public PhysicsMaterial ClosestHitSurfaceType => m_ClosestHit.collider.sharedMaterial;

        public GroundCollisionVariables(RaycastHit closestHit, RaycastHit flattestHit, float flattestHitDot)
        {
            m_ClosestHit = closestHit;
            m_FlattestHit = flattestHit;
            m_FlattestHitDot = flattestHitDot;
        }
    }

    private bool ShouldUpdateGround(MovementType movementType)
    {
        return movementType != MovementType.Jumping;
    }

    private static Vector3 GetGroundRaycastOrigin(in ControllerState state, in CharacterController controller)
    {
        var origin = state.CurrentPosition;
        var delta = origin - (float3)controller.bounds.center;
        delta.y = 0f;

        origin -= delta;

        return origin;
    }

    private bool UpdateGround(in ControllerState state, in ControllerConsts consts,
        out GroundCollisionVariables groundCollision)
    {
        if (ShouldUpdateGround(state.MovementType))
        {
            var currentPos = GetGroundRaycastOrigin(state, m_Controller);
            // set sphere position, with offset
            var controllerCentre = transform.rotation * ControllerOffset;
            var testRadius = m_Controller.radius;
            var testStart = new Vector3(currentPos.x + controllerCentre.x, currentPos.y + +controllerCentre.y,
                currentPos.z + controllerCentre.z);
            var numHits = Physics.SphereCastNonAlloc(testStart, testRadius, Vector3.down,
                m_GroundCheckRaycastResults, GroundedOffset, consts.GroundLayers, QueryTriggerInteraction.Ignore);

            // Choose the best hit
            float largestDot = float.MinValue;
            float closestDistSq = float.MaxValue;
            int flattestHitIndex = -1;
            int closestHitIndex = -1;

            for (int i = 0; i < numHits; ++i)
            {
                var raycastResult = m_GroundCheckRaycastResults[i];

                float dot = math.dot(raycastResult.normal, k_UpVector);

                if (dot > largestDot) //select the most upright normal to avoid issues with corner collisions looking like a slope
                {
                    largestDot = dot;
                    flattestHitIndex = i;
                }

                float distSq = raycastResult.point.sqrMagnitude > 0f
                    ? math.distancesq(raycastResult.point, currentPos)
                    : 0f; //point will be (0,0,0) if the spherecast starts inside the collider

                if (distSq < closestDistSq)
                {
                    closestDistSq = distSq;
                    closestHitIndex = i;
                }
            }

            // Did we discard all of the collisions?
            if (flattestHitIndex >= 0)
            {
                Debug.Assert(closestHitIndex >= 0,
                    "[FIRSTPERSONCONTROLLER] flattest hit is valid but closest hit isn't, this shouldn't be possible!");

                RaycastHit flattest = m_GroundCheckRaycastResults[flattestHitIndex];
                RaycastHit closest = m_GroundCheckRaycastResults[closestHitIndex];

                groundCollision = new GroundCollisionVariables(closestHit: closest, flattestHit: flattest,
                    flattestHitDot: largestDot);
                return true;
            }
        }

        groundCollision = new GroundCollisionVariables();
        return false;
    }

    public void UpdateGround(ref ControllerState state, in ControllerConsts consts)
    {
        if (UpdateGround(state, consts, out var groundCollision))
        {
            GroundPhysicsMaterial = groundCollision.ClosestHitSurfaceType;
            state.GroundNormal = groundCollision.FlattestHitNormal;
        }
        else
        {
            GroundPhysicsMaterial = null;
            state.GroundNormal = k_UpVector;
        }
    }

    public void GroundedCheck(ref ControllerState state, in ControllerConsts consts)
    {
        bool isGrounded = UpdateGround(state, consts, out var groundCollision);

        if (isGrounded && state.MovementType == MovementType.Falling)
        {
            state.JumpTimeoutDelta = consts.LandingTimeout;
            state.Land = true;
            state.LandTriggered = true;
            state.Jump = false;
            SetMovementType(ref state, MovementType.Standing);
        }
        else if (!isGrounded && state.MovementType == MovementType.Standing)
        {
            SetMovementType(ref state, MovementType.Falling);
        }
        else if (isGrounded && state.MovementType == MovementType.Standing)
        {
            state.Land = false;
        }

        if (isGrounded)
        {
            state.GroundNormal = groundCollision.FlattestHitNormal;
            GroundPhysicsMaterial = groundCollision.ClosestHitSurfaceType;
        }
        else
        {
            state.GroundNormal = k_UpVector;
            GroundPhysicsMaterial = null;
        }
    }

    private void ApplyPosImmediate(in ControllerState state)
    {
        if (m_Controller.enabled)
        {
            m_Controller.enabled = false;
            transform.position = state.CurrentPosition;
            m_Controller.enabled = true;

#if UNITY_EDITOR || DEBUG
            float deltaSqrd = math.distancesq(state.CurrentPosition, transform.position);
            Debug.Assert(deltaSqrd <= k_ResetMovementAdjustEpsilon,
                $"ApplyPosImmediate has failed to move the controller to the correct location (target {state.CurrentPosition} actual {(float3)transform.position} delta {math.sqrt(deltaSqrd)} {state.CurrentPosition - (float3)transform.position} state {state.MovementType} prev {m_PrevMovementType})");
#endif
        }
        else
        {
            // set directly, no physics required
            transform.position = state.CurrentPosition;
        }
    }

    public void ApplyPosRotImmediate(in ControllerState state)
    {
        ApplyPosImmediate(state);
        transform.rotation = state.CurrentRotation;
    }

    private static void GetStateConsts(out ControllerConsts.StateConsts stateConsts, ref ControllerState state,
        in PlayerInput input, in ControllerConsts consts, float deltaTime)
    {
        stateConsts.Speed = 0f;
        stateConsts.RotationSmoothTime = 0f;
        stateConsts.SpeedChangeRate = k_DefaultSpeedChange; //just a large number to make it snap to the 0 speed
        stateConsts.LandingSpeedMult = 1f;
        stateConsts.AnimationMotionScale = 0.4f;

        // Freeze after teleport for a second
        if (state.TeleportFreeze > 0f)
        {
            state.TeleportFreeze -= deltaTime;
        }
        else
        {
            switch (state.MovementType)
            {
                case MovementType.Standing:
                case MovementType.Jumping:
                case MovementType.Falling:
                    stateConsts = consts.Walk;
                    break;
                default:
                    Debug.LogError(
                        $"[FIRSTPERSONCONTROLLER] GetStateConsts : Unhandled state {state.MovementType.ToString()}");
                    break;
            }
        }
    }

    private static float3 CalculateMovementFromInput(ref ControllerState state, in ControllerConsts consts,
        in ControllerConsts.StateConsts stateConsts, in PlayerInput input, bool updateRotation, float deltaTime)
    {
        if (updateRotation)
        {
            state.YawDegrees = input.LookYawPitchDegrees.x;
            state.PitchDegrees = input.LookYawPitchDegrees.y;
            state.CurrentRotation = quaternion.RotateY(math.radians(state.YawDegrees));
        }

        var rotQuat = state.CurrentRotation; // Use the rotation already calculated above
        var localMove = new float3(input.MoveInput.x, 0f, input.MoveInput.y);

        // Normalize it to get a pure direction vector with a length of 1.
        var localDir = math.normalizesafe(localMove);

        // Rotate the pure direction by the character's facing rotation.
        var worldDir = math.mul(rotQuat, localDir);

        // Multiply the pure direction by the final speed calculated in AccumulateMovement.
        return worldDir * state.MovementSpeed;
    }

    private static void AddMovementFromJumpFall(ref float3 moveDelta, in ControllerState state)
    {
        moveDelta.y += state.JumpFallSpeed;
    }

    public static void ProcessInputs(ref ControllerState state, in PlayerInput input, float deltaTime)
    {
    }

    public static void AccumulateMovement(ref ControllerState state,
        ref float3 accumulatedMovement, in PlayerInput input, in ControllerConsts consts, float deltaTime)
    {
        state.TimeInState += deltaTime;

        AccumulateJumpAndGravity(ref state, input, consts, deltaTime);

        GetStateConsts(out var stateConsts, ref state, in input, in consts, deltaTime);

        var updateRotation = true;

        float combinedMoveSpeedModifier = 1f;

        float modifiedTargetMoveSpeed =
            stateConsts.Speed * combinedMoveSpeedModifier; //don't apply modifiers to the aiming speed

        float inputMagnitude = math.length(input.MoveInput);

        // apply analog deadzone
        inputMagnitude = inputMagnitude >= 0.4f ? 1f : 0f;

        float applyTargetSpeed = modifiedTargetMoveSpeed * inputMagnitude;
        float blendAlpha = deltaTime * stateConsts.SpeedChangeRate;

        state.MovementSpeed = applyTargetSpeed;
        state.AnimatorTargetSpeedChangeRate = stateConsts.SpeedChangeRate;
        state.AnimatorTargetSpeed = stateConsts.Speed * inputMagnitude;
        state.AnimatorMotionSpeed = inputMagnitude > 0f ? state.MovementSpeed : 1f; //play the idle at 1x

        state.AnimatorMotion = float3.zero;
        state.AnimatorMotionChangeRate = 0f;

        var moveDelta = float3.zero;
        updateRotation &= applyTargetSpeed > 0f;

        switch (state.MovementType)
        {
            case MovementType.Standing:
                {
                    moveDelta = CalculateMovementFromInput(ref state, consts, stateConsts, input, true, deltaTime);
                    AddMovementFromJumpFall(ref moveDelta, state);
                }
                break;

            case MovementType.Jumping:
            case MovementType.Falling:
                {
                    moveDelta = CalculateMovementFromInput(ref state, consts, stateConsts, input, true, deltaTime);
                    AddMovementFromJumpFall(ref moveDelta, state);
                }
                break;

            default:
                {
                    Debug.LogError(
                        $"[FIRSTPERSONCONTROLLER] AccumulateMovement : Unhandled state {state.MovementType.ToString()}");
                }
                break;
        }

        accumulatedMovement += moveDelta * deltaTime;
        state.MovementRequest = accumulatedMovement;
    }

    private void ApplyMove(ref ControllerState state, in ControllerConsts consts, in float3 accumulatedMovement,
        float deltaTime)
    {
        var posDelta = (Vector3)state.CurrentPosition - transform.position;
        if (posDelta.sqrMagnitude > 0f)
        {
            MovementLog($"{name} - Reset Teleport to {state.CurrentPosition} (from {(float3)transform.position})");
            ApplyPosImmediate(state);
        }

        bool allowMove = math.lengthsq(accumulatedMovement) > 0f;
        if (allowMove)
        {
            // apply movement
            var movementToApply = accumulatedMovement;

            // This can be called before the controller is enabled (e.g. on spawn)
            // prevents errors of moving when controller is disabled or GameObject inactive
            if (m_Controller != null && m_Controller.enabled && gameObject.activeInHierarchy)
            {
                // just apply our move as normal
                m_Controller.Move(movementToApply);
            }
        }

        // apply rotation
        transform.rotation = state.CurrentRotation;

        if (ShouldUpdateFallHeight(state.MovementType))
        {
            float fallDist = state.CurrentPosition.y - transform.position.y;
            state.FallHeight += fallDist;
        }

        // store position
        state.CurrentPosition = transform.position;

#if UNITY_EDITOR || DEBUG
        m_PrevMovementType = state.MovementType;
#endif
    }

    private static void AccumulateJumpAndGravity(ref ControllerState state, in PlayerInput input,
        in ControllerConsts consts, float deltaTime)
    {
        switch (state.MovementType)
        {
            case MovementType.Standing:
                {
                    ClearFallingState(ref state);

                    if (AccumulateJump(ref state, in input, in consts, deltaTime) ||
                        state.TimeInState < consts.LandingTimeout)
                    {
                        //we've just started jumping or we're in the process of landing so apply gravity
                        AccumulateGravity(ref state, in consts, deltaTime);
                    }
                    else
                    {
                        //fully in standing state reset gravity to a minimal fall speed to keep player aligned on ground (necessary for uneven terrain)
                        state.JumpFallSpeed = consts.StandingFallSpeed;
                    }
                }
                break;

            case MovementType.Falling:
            case MovementType.Jumping:
                {
                    state.Jump = true;
                    state.JumpTimeoutDelta = math.max(consts.JumpTimeout, state.JumpTimeoutDelta);

                    // fall timeout
                    if (state.FallTimeoutDelta < consts.FallTimeout)
                    {
                        state.FallTimeoutDelta += deltaTime;
                    }
                    else
                    {
                        state.Fall = true;
                    }

                    AccumulateGravity(ref state, in consts, deltaTime);

                    var newMovementState = state.JumpFallSpeed >= 0f ? MovementType.Jumping : MovementType.Falling;
                    SetMovementType(ref state, newMovementState);
                }
                break;

            default:
                {
                    Debug.LogError(
                        $"[FIRSTPERSONCONTROLLER] AccumulateJumpAndGravity : Unhandled state {state.MovementType.ToString()}");
                }
                break;
        }
    }

    private static void ClearFallingState(ref ControllerState state)
    {
        // reset the fall timeout timer
        state.FallTimeoutDelta = 0f;

        // update animator if using character
        state.Jump = false;
        state.Fall = false;
    }

    private static void AccumulateGravity(ref ControllerState state, in ControllerConsts consts, float deltaTime)
    {
        // apply gravity over time if we haven't reached terminal (multiply by delta time twice to linearly speed up over time)
        var terminalVelocity = consts.TerminalVelocity;

        if (state.JumpFallSpeed > terminalVelocity)
        {
            state.JumpFallSpeed += consts.Gravity * deltaTime;
        }

        // Handle the terminal velocity becoming much smaller due to umbrella opening
        else if (state.JumpFallSpeed <= terminalVelocity)
        {
            state.JumpFallSpeed = Mathf.Lerp(state.JumpFallSpeed, terminalVelocity, deltaTime);
        }
    }

    private static bool AccumulateJump(ref ControllerState state, in PlayerInput input, in ControllerConsts consts,
        float deltaTime)
    {
        bool jumped = false;

        if (input.Jump
            && state.JumpTimeoutDelta <= 0f)
        {
            // the square root of H * -2 * G = how much velocity needed to reach desired height
            state.JumpFallSpeed = math.sqrt(consts.JumpHeight * -2f * consts.Gravity); // 6
            SetMovementType(ref state, MovementType.Jumping);
            state.Jump = true;
            state.JumpTriggered = true;
            jumped = true;
        }

        if (state.JumpTimeoutDelta > 0f)
        {
            state.JumpTimeoutDelta -= deltaTime;
        }

        return jumped;
    }

    // taken from DOTSSample MathHelper
    // Collection of converted classic Unity (Mathf, Vector3 etc.) + some homegrown math functions using Unity.Mathematics
    // These are made/converted for production and unlike a proper library they are lacking any tests, so use at your own peril!
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float SmoothDampAngle(
        float current,
        float target,
        ref float currentVelocity,
        float smoothTime,
        float maxSpeed,
        float deltaTime)
    {
        target = current + DeltaAngle(current, target);
        float result = SmoothDamp(current, target, ref currentVelocity, smoothTime, maxSpeed, deltaTime);
        result = Repeat360(result);

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float SmoothDamp(
        float current,
        float target,
        ref float currentVelocity,
        float smoothTime,
        float maxSpeed,
        float deltaTime)
    {
        // Based on Game Programming Gems 4 Chapter 1.10
        smoothTime = math.max(0.0001F, smoothTime);
        float omega = 2F / smoothTime;

        float x = omega * deltaTime;
        float exp = 1F / (1F + x + (0.48F * x * x) + (0.235F * x * x * x));
        float change = current - target;
        float originalTo = target;

        // Clamp maximum speed
        float maxChange = maxSpeed * smoothTime;
        change = math.clamp(change, -maxChange, maxChange);
        target = current - change;

        float temp = (currentVelocity + (omega * change)) * deltaTime;
        currentVelocity = (currentVelocity - (omega * temp)) * exp;
        float result = target + ((change + temp) * exp);

        // Prevent overshooting
        if ((originalTo - current > 0.0F) == (result > originalTo))
        {
            result = originalTo;
            currentVelocity = (result - originalTo) / deltaTime;
        }

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float DeltaAngle(float current, float target)
    {
        float delta = Repeat360(target - current);

        if (delta > 180.0F)
        {
            delta -= 360.0F;
        }

        float result = delta;

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Repeat360(float t)
    {
        const float repeat_length = 360f;
        const float inverse_length = 1 / repeat_length;

        return math.clamp(t - (math.floor(t * inverse_length) * repeat_length), 0.0f, repeat_length);
    }

    [System.Diagnostics.Conditional("ENABLE_MOVEMENT_DIAGNOSTICS")]
    public static void MovementLog(string message)
    {
        Debug.Log($"[{UnityEngine.Time.frameCount}] {message}");
    }
}
