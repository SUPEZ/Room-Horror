using System;
using System.Threading.Tasks;

namespace TurnBasedStrategyFramework.Common.AI.BehaviourTrees
{
    /// <summary>
    /// Represents a debug node in a behavior tree, used for executing a custom action for debugging purposes.
    /// </summary>
    public readonly struct DebugNode : ITreeNode
    {
        /// <summary>
        /// The action to be executed by this debug node.
        /// </summary>
        private readonly Action _action;

        /// <summary>
        /// Initializes a new instance of the <see cref="DebugNode"/> struct with the specified action.
        /// </summary>
        /// <param name="action">The action that this debug node will execute.</param>
        public DebugNode(Action action)
        {
            _action = action;
        }

        /// <summary>
        /// Executes the debug node by invoking the specified action.
        /// Always returns true after the action is invoked.
        /// </summary>
        /// <returns>A task representing the execution, with a boolean result indicating success.</returns>
        public Task<bool> Execute(bool debugMode)
        {
            _action.Invoke();
            return Task.FromResult(true);
        }
    }
}