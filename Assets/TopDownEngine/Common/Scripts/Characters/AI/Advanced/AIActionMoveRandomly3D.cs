using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoreMountains.Tools;

namespace MoreMountains.TopDownEngine
{
	/// <summary>
	/// Requires a CharacterMovement ability. 
	/// Makes the character move randomly, until it finds an obstacle in its path, in which case it'll pick a new direction at random
	/// </summary>
	[AddComponentMenu("TopDown Engine/Character/AI/Actions/AI Action Move Randomly 3D")]
	//[RequireComponent(typeof(CharacterMovement))]
	public class AIActionMoveRandomly3D : AIAction
	{
		[Header("Duration")]
		/// the maximum time a character can spend going in a direction without changing
		[Tooltip("the maximum time a character can spend going in a direction without changing")]
		public float MaximumDurationInADirection = 2f;

		[Header("Obstacles")]
		/// the layers the character will try to avoid
		[Tooltip("the layers the character will try to avoid")]
		public LayerMask ObstacleLayerMask = LayerManager.ObstaclesLayerMask;

		/// the minimum distance from the target this Character can reach.
		[Tooltip("the minimum distance from the target this Character can reach.")]
		public float ObstaclesDetectionDistance = 1f;

		/// the frequency (in seconds) at which to check for obstacles
		[Tooltip("the frequency (in seconds) at which to check for obstacles")]
		public float ObstaclesCheckFrequency = 1f;

		/// the minimal random direction to randomize from
		[Tooltip("the minimal random direction to randomize from")]
		public Vector2 MinimumRandomDirection = new Vector2(-1f, -1f);

		/// the maximum random direction to randomize from
		[Tooltip("the maximum random direction to randomize from")]
		public Vector2 MaximumRandomDirection = new Vector2(1f, 1f);

		[Header("Debug")]
		/// true if an obstacle was hit on the last check
		[Tooltip("true if an obstacle was hit on the last check")]
		[MMReadOnly]
		public bool DebugHitObstacle = false;

		protected CharacterMovement _characterMovement;
		protected Vector2 _direction;

		[Header("Collider")]
		/// The collider used for obstacle detection, to be assigned in the inspector
		[Tooltip("The collider used for obstacle detection, to be assigned in the inspector")]
		public Collider TargetCollider;

		protected float _lastObstacleDetectionTimestamp = 0f;
		protected float _lastDirectionChangeTimestamp = 0f;
		protected Vector3 _rayDirection;

		protected TopDownController _controller;
		protected Character _character;

		/// <summary>
		/// On start we grab our character movement component and pick a random direction
		/// </summary>
		public override void Initialization()
		{
			if (!ShouldInitialize) return;
			base.Initialization();
			_character = this.gameObject.GetComponentInParent<Character>();
			_characterMovement = _character?.FindAbility<CharacterMovement>();
			_controller = this.gameObject.GetComponentInParent<TopDownController>();
			TargetCollider = this.gameObject.GetComponentInParent<Collider>();
			PickRandomDirection();
		}

		/// <summary>
		/// On PerformAction we move
		/// </summary>
		public override void PerformAction()
		{
			CheckForObstacles();
			CheckForDuration();
			Move();
		}

		/// <summary>
		/// Moves the character
		/// </summary>
		protected virtual void Move()
		{
			_characterMovement.SetMovement(_direction);
		}

		/// <summary>
		/// Checks for obstacles by casting a ray
		/// </summary>
		protected virtual void CheckForObstacles()
		{
			if (Time.time - _lastObstacleDetectionTimestamp < ObstaclesCheckFrequency)
			{
				return;
			}

			_rayDirection = Vector3.zero;
			_rayDirection.x = _direction.x;
			_rayDirection.z = _direction.y;

			if (TargetCollider != null)
			{
				bool hit = Physics.BoxCast(TargetCollider.bounds.center, TargetCollider.bounds.extents,
					_rayDirection.normalized,
					this.transform.rotation, ObstaclesDetectionDistance, ObstacleLayerMask);

				DebugHitObstacle = hit;

				if (hit)
				{
					Debug.Log("[AIActionMoveRandomly3D] Obstacle hit");
					PickRandomDirection();
				}
			}

			_lastObstacleDetectionTimestamp = Time.time;
		}

		/// <summary>
		/// Checks whether or not we should pick a new direction at random
		/// </summary>
		protected virtual void CheckForDuration()
		{
			if (Time.time - _lastDirectionChangeTimestamp > MaximumDurationInADirection)
			{
				PickRandomDirection();
			}
		}

		/// <summary>
		/// Picks a random direction
		/// </summary>
		protected virtual void PickRandomDirection()
		{
			_direction.x = UnityEngine.Random.Range(MinimumRandomDirection.x, MaximumRandomDirection.x);
			_direction.y = UnityEngine.Random.Range(MinimumRandomDirection.y, MaximumRandomDirection.y);
			_lastDirectionChangeTimestamp = Time.time;
		}

		/// <summary>
		/// On exit state we stop our movement
		/// </summary>
		public override void OnExitState()
		{
			base.OnExitState();

			if (_characterMovement != null)
			{
				_characterMovement.StopMovementInstantly();
			}

			if (_controller != null)
			{
				_controller.SetMovement(Vector3.zero);
				_controller.Velocity = Vector3.zero;
				_controller.AddedForce = Vector3.zero;
				_controller.Acceleration = Vector3.zero;
			}

			if (_character != null && _character.MovementState.CurrentState != CharacterStates.MovementStates.Idle)
			{
				_character.MovementState.ChangeState(CharacterStates.MovementStates.Idle);
			}
		}

		/// <summary>
		/// Draws a gizmo to show the obstacle check direction and bounding box
		/// </summary>
		protected virtual void OnDrawGizmosSelected()
		{
			Collider col = TargetCollider;
			if (col == null)
			{
				col = this.gameObject.GetComponentInParent<Collider>();
			}

			if (col != null)
			{
				Vector3 rayDir = Vector3.zero;
				if (Application.isPlaying)
				{
					rayDir.x = _direction.x;
					rayDir.z = _direction.y;
				}
				else
				{
					// In edit mode, just show a preview using the forward vector
					rayDir = this.transform.forward;
				}

				if (rayDir.magnitude > 0.01f)
				{
					Gizmos.color = Color.red;
					Gizmos.DrawRay(col.bounds.center, rayDir.normalized * ObstaclesDetectionDistance);

					Gizmos.matrix = Matrix4x4.TRS(col.bounds.center + rayDir.normalized * ObstaclesDetectionDistance,
						this.transform.rotation, Vector3.one);
					Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
					Gizmos.DrawWireCube(Vector3.zero, col.bounds.extents * 2f);
				}
			}
		}
	}
}