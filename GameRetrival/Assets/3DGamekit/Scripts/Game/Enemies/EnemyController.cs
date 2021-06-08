using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.AI;

namespace Gamekit3D
{
//this assure it's runned before any behaviour that may use it, as the animator need to be fecthed
    [DefaultExecutionOrder(-1)]
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyController : MonoBehaviour
    {
        public bool interpolateTurning = false;
        public bool applyAnimationRotation = false;

        public Animator animator { get { return m_Animator; } }
        public Vector3 externalForce { get { return m_ExternalForce; } }
        public NavMeshAgent navmeshAgent { get { return m_NavMeshAgent; } }
        public bool followNavmeshAgent { get { return m_FollowNavmeshAgent; } }
        public bool grounded { get { return m_Grounded; } }

        protected NavMeshAgent m_NavMeshAgent;
        protected bool m_FollowNavmeshAgent;
        protected Animator m_Animator;
        protected bool m_UnderExternalForce;
        protected bool m_ExternalForceAddGravity = true;
        protected Vector3 m_ExternalForce;
        protected bool m_Grounded;

        protected Rigidbody m_Rigidbody;

        const float k_GroundedRayDistance = .8f;

        //add variables for custom enemies
        public string State;
        public float RunSpeed = 1;
        public float WalkSpeed = 1;

        private Vector3 deltaPosition
        {
            get
            {
                Vector3 delta = m_Animator.deltaPosition;
                if (State == "Run") delta *= RunSpeed;
                else if (State == "Walk") delta *= WalkSpeed;
                return delta;
            }
        }
        void OnEnable()
        {
            m_NavMeshAgent = GetComponent<NavMeshAgent>();
            m_Animator = GetComponent<Animator>();
            m_Animator.updateMode = AnimatorUpdateMode.AnimatePhysics;

            m_NavMeshAgent.updatePosition = false;

            m_Rigidbody = GetComponentInChildren<Rigidbody>();
            if (m_Rigidbody == null)
                m_Rigidbody = gameObject.AddComponent<Rigidbody>();

            m_Rigidbody.isKinematic = true;
            m_Rigidbody.useGravity = false;
            m_Rigidbody.interpolation = RigidbodyInterpolation.Interpolate;

            m_FollowNavmeshAgent = true;

            /*Add custom enemy, link the animator of both*/
            if(m_Animator.avatar != null) return;
            foreach(Animator a in GetComponentsInChildren<Animator>())
            {
                if(a != m_Animator)
                {
                    m_Animator.avatar = a.avatar;
                    a.enabled = false;
                }
            }
        }

        private void FixedUpdate()
        {
            animator.speed = PlayerInput.Instance != null && PlayerInput.Instance.HaveControl() ? 1.0f : 0.0f;

            CheckGrounded();

            if (m_UnderExternalForce)
                ForceMovement();
        }

        void CheckGrounded()
        {
            RaycastHit hit;
            Ray ray = new Ray(transform.position + Vector3.up * k_GroundedRayDistance * 0.5f, -Vector3.up);
            m_Grounded = Physics.Raycast(ray, out hit, k_GroundedRayDistance, Physics.AllLayers,
                QueryTriggerInteraction.Ignore);
        }

        void ForceMovement()
        {
            if(m_ExternalForceAddGravity)
                m_ExternalForce += Physics.gravity * Time.deltaTime;

            RaycastHit hit;
            Vector3 movement = m_ExternalForce * Time.deltaTime;
            if (!m_Rigidbody.SweepTest(movement.normalized, out hit, movement.sqrMagnitude))
            {
                m_Rigidbody.MovePosition(m_Rigidbody.position + movement);
            }

            m_NavMeshAgent.Warp(m_Rigidbody.position);
        }

        private void OnAnimatorMove()
        {
            if (m_UnderExternalForce)
                return;

            if (m_FollowNavmeshAgent)
            {
                //use the private virable - deltaPosition, instead of referring from m_Animator, to get custom enemy running
                m_NavMeshAgent.speed = (deltaPosition / Time.deltaTime).magnitude;// m_NavMeshAgent.speed = (m_Animator.deltaPosition / Time.deltaTime).magnitude;
                transform.position = m_NavMeshAgent.nextPosition;
            }
            else
            {
                RaycastHit hit;
                if (!m_Rigidbody.SweepTest(deltaPosition.normalized, out hit,
                    deltaPosition.sqrMagnitude))
                {
                    //use the private virable - deltaPosition, instead of referring from m_Animator, to get custom enemy running
                    m_Rigidbody.MovePosition(m_Rigidbody.position + deltaPosition);//m_Rigidbody.MovePosition(m_Rigidbody.position + m_Animator.deltaPosition);
                }
            }

            if (applyAnimationRotation)
            {
                transform.forward = m_Animator.deltaRotation * transform.forward;
            }
        }

        // used to disable position being set by the navmesh agent, for case where we want the animation to move the enemy instead (e.g. Chomper attack)
        public void SetFollowNavmeshAgent(bool follow)
        {
            if (!follow && m_NavMeshAgent.enabled)
            {
                m_NavMeshAgent.ResetPath();
            }
            else if(follow && !m_NavMeshAgent.enabled)
            {
                m_NavMeshAgent.Warp(transform.position);
            }

            m_FollowNavmeshAgent = follow;
            m_NavMeshAgent.enabled = follow;
        }

        public void AddForce(Vector3 force, bool useGravity = true)
        {
            if (m_NavMeshAgent.enabled)
                m_NavMeshAgent.ResetPath();

            m_ExternalForce = force;
            m_NavMeshAgent.enabled = false;
            m_UnderExternalForce = true;
            m_ExternalForceAddGravity = useGravity;
        }

        public void ClearForce()
        {
            m_UnderExternalForce = false;
            m_NavMeshAgent.enabled = true;
        }

        public void SetForward(Vector3 forward)
        {
            Quaternion targetRotation = Quaternion.LookRotation(forward);

            if (interpolateTurning)
            {
                targetRotation = Quaternion.RotateTowards(transform.rotation, targetRotation,
                    m_NavMeshAgent.angularSpeed * Time.deltaTime);
            }

            transform.rotation = targetRotation;
        }

        public bool SetTarget(Vector3 position)
        {
            return m_NavMeshAgent.SetDestination(position);
        }
    }
}