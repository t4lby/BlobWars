using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base class for all unit types, all units have HP, can attack and move.
/// At any time the unit is occupying one of these states.
/// The uniqueness comes from the speed the unit moves, and the power and range of the attack.
/// Villagers are also units!
/// </summary>
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody))]
public class Unit : MonoBehaviour
{
    public bool IsServerUnit;
    public ushort TeamID; // The ID of the team this unit belongs to.
    public float HP;
    public UnitStance Stance;
    /// <summary>
    /// If a unit has no target (null) it will stay Idle, once a target is set the unit
    /// will walk toward the target via whatever path finding is available.
    /// </summary>
    public Transform Target;

    // set the following in inspector prefab. Below are base villager stats.
    public float MaxHP = 50;
    public float MoveSpeed = 1; // m/s
    public float WalkAnimSpeedMultiplier = 1; // sync animation.
    public float AttackPower = 1; // hp/s
    public float AttackRange = 1; // m
    public float AttackRate = 1; // attack/s
    public float UnitRadius = 0.25f;

    private Animator _animator;
    private float _attackAnimLength;
    private float _walkAnimLength;

    private Rigidbody _rigidbody;

    private AttackCycle _attackCycle;

    // Should be single server controller present as parent.
    private ServerController _server;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _rigidbody = GetComponent<Rigidbody>();
        CheckAnimationClips(); // validate clips and record lengths.
        
    }

    private void Start()
    {
        if (IsServerUnit)
        {
            _server = GetComponentInParent<ServerController>();
        }
    }

    private void Update()
    {
        // QQ: don't animate server units!

        // depending on the unit state set the current animation.
        _animator.SetBool("Walk", Stance == UnitStance.Walk);
        _animator.SetBool("Attack", Stance == UnitStance.Attack);

        // sync walk animation to walk speed, and attack animation to attack speed.
        switch (Stance)
        {
            case UnitStance.Attack:
                _animator.speed = AttackRate * _attackAnimLength; break; //ensures 1 attack anim per attack.
            case UnitStance.Walk:
                _animator.speed = MoveSpeed * WalkAnimSpeedMultiplier * _walkAnimLength; break; //ensures 1 walk anim per meter walked.
            default:
                _animator.speed = 1;
                break;
        }
    }

    private void FixedUpdate()
    {
        if (!IsServerUnit)
        {
            return; // no fixed update cycle for non server units.
        }
        Vector3 moveVector = transform.position;
        // Simple initial state machine
        switch (Stance)
        {
            case UnitStance.Idle:
                {
                    // keep unit at correct height for map.
                    
                    if (Target != null)
                    {
                        Stance = UnitStance.Walk;
                    }
                    break;
                }
            case UnitStance.Walk:
                {
                    if (Target == null)
                    {
                        Stance = UnitStance.Idle; // transition back to idle.
                        break;
                    }
                    //move over height map and move in direction
                    //according to pathfinding (when exists)

                    moveVector = transform.position + Time.fixedDeltaTime * MoveSpeed *
                        (Target.position - transform.position).normalized;

                    var lookVector = Target.position - transform.position;
                    lookVector.y = 0;
                    lookVector.Normalize();

                    _rigidbody.MoveRotation(
                        Quaternion.LookRotation(lookVector));
                    // check for attack posibility.
                    float distFromTarget = (Target.position - transform.position).magnitude;
                    if (distFromTarget < AttackRange &&
                        TargetIsEnemy())
                    {
                        Stance = UnitStance.Attack;
                    }
                    if (distFromTarget < /*two radii combined plus buffer */0.9f &&
                        !TargetIsEnemy())
                    {
                        // arriving at non enemy target.
                        Target = null;
                        Stance = UnitStance.Idle;
                    }
                    break;
                }
            case UnitStance.Attack:
                {
                    // if no attack cycle running and target is enemy unit,
                    //      save reference to target and start attack cycle.
                    if (_attackCycle == null && TargetIsEnemy())
                    {
                        _attackCycle = new AttackCycle
                        {
                            Attackee = Target.GetComponent<Unit>(),
                            AttackStartTime = Time.fixedTime
                        };
                    }
                    else
                    {
                        // check target is still present, if not, end cycle and
                        // move to idle.
                        if (Target.transform != _attackCycle.Attackee.transform)
                        {
                            Stance = UnitStance.Idle;
                        }
                        else
                        {
                            //ensure still facing target.
                            _rigidbody.MoveRotation(
                                Quaternion.LookRotation(
                                (Target.position - transform.position).normalized));
                            //if attack cycle over, apply damage to attackee,
                            //and transition to walk.
                            if (_attackCycle.AttackStartTime + 1f / AttackRate <= Time.fixedTime)
                            {
                                Stance = UnitStance.Idle;
                                _attackCycle = null;
                                Target.GetComponent<Unit>().HP -= AttackPower;
                            }
                        }
                    }
                    break;
                }
        }
        ConstrainMoveVectorToMap(ref moveVector);
        _rigidbody.MovePosition(moveVector);
    }

    private void ConstrainMoveVectorToMap(ref Vector3 moveVector)
    {
        // clip vector to map and unit radius.
        moveVector.x = Mathf.Clamp(moveVector.x, UnitRadius, _server.Map.Width - UnitRadius);
        moveVector.z = Mathf.Clamp(moveVector.z, UnitRadius, _server.Map.Width - UnitRadius);
        // interpolate height.
        moveVector.y = _server.Map.GetHeight(moveVector.x, moveVector.z);
    }

    public bool CheckAnimationClips()
    {
        AnimationClip[] clips = _animator.runtimeAnimatorController.animationClips;

        bool attackExists = false;
        bool walkExists = false;
        foreach (AnimationClip clip in clips)
        {
            switch (clip.name)
            {
                case "Attack":
                    _attackAnimLength = clip.length;
                    attackExists = true;
                    break;
                case "Walk":
                    _walkAnimLength = clip.length;
                    walkExists = true;
                    break;
            }
        }

        bool attackTrigExists = false;
        bool walkTrigExists = false;
        foreach (var param in _animator.parameters)
        {
            switch (param.name)
            {
                case "Attack":
                    attackTrigExists = true;
                    break;
                case "Walk":
                    walkTrigExists = true;
                    break;
            }
        }

        if (!walkExists)
        {
            Debug.LogError("No walk animation present in unit animator, " +
                "requires animation named 'Walk' to be present");
            return false;
        }
        if (!attackExists)
        {
            Debug.LogError("No attack animation present in unit animator, " +
                "requires animation named 'Attack' to be present");
            return false;
        }
        if (!walkTrigExists)
        {
            Debug.LogError("No walk trigger present in animator, " +
                "requires bool param named 'Walk' to be present");
            return false;
        }
        if (!attackTrigExists)
        {
            Debug.LogError("No attack trigger present in animator, " +
                "requires bool param named 'Attack' to be present");
            return false;
        }
        return true;
    }

    private bool TargetIsEnemy()
    {
        if (Target.GetComponent<Unit>() != null)
        {
            return Target.GetComponent<Unit>().TeamID != TeamID;
        }
        return false;
    }
}

public enum UnitType
{
    Villager = 0,
    Warrior = 1
}

public enum UnitStance
{
    Walk,
    Attack,
    Idle
}

public class AttackCycle
{
    public Unit Attackee;
    public float AttackStartTime;
}
