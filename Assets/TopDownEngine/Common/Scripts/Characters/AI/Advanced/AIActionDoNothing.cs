using MoreMountains.Tools;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MoreMountains.TopDownEngine
{
	/// <summary>
	/// As the name implies, an action that does nothing. Just waits there.
	/// </summary>
	[AddComponentMenu("TopDown Engine/Character/AI/Actions/AI Action Do Nothing")]
	public class AIActionDoNothing : AIAction
	{
		protected CharacterMovement _characterMovement;
		protected TopDownController _controller;
		protected Character _character;

		public override void Initialization()
		{
			if (!ShouldInitialize) return;
			base.Initialization();
			_character = this.gameObject.GetComponentInParent<Character>();
			_characterMovement = _character?.FindAbility<CharacterMovement>();
			_controller = this.gameObject.GetComponentInParent<TopDownController>();
		}

		/// <summary>
		/// On PerformAction we ensure movement is stopped
		/// </summary>
		public override void PerformAction()
		{
			StopCharacter();
		}

		public override void OnEnterState()
		{
			base.OnEnterState();
			StopCharacter();
		}

		protected virtual void StopCharacter()
		{
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
	}
}