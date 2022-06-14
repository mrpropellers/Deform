using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using UnityEngine;
using Unity.Jobs;

namespace Deform
{
	/// <summary>
	/// Manages scheduling deformables.
	/// </summary>
    [HelpURL ("https://github.com/keenanwoodall/Deform/wiki/DeformableManager")]
	public partial class DeformableManager : MonoBehaviour
	{
		private static readonly string DEF_MANAGER_NAME = "DefaultDeformableManager";

		private static DeformableManager defaultInstance;

        /// <summary>
		/// Returns the default manager.
		/// </summary>
		/// <param name="createIfMissing">If true, a manager will be created if one doesn't exist.</param>
		/// <returns></returns>
		public static DeformableManager GetDefaultManager (bool createIfMissing)
		{
			if (defaultInstance == null && createIfMissing)
			{
                // if (Globals.IsHybridRenderer)
                // {
                //     Debug.LogWarning(
                //         "Since we're using the Hybrid Renderer, this class doesn't need to be constructed. " +
                //         "The DeformableSystem will manage our deformables.");
                // }
				defaultInstance = new GameObject(DEF_MANAGER_NAME).AddComponent<DeformableManager>();
				GameObject.DontDestroyOnLoad(defaultInstance.gameObject);
                if (Globals.IsHybridRenderer)
                {
                    defaultInstance.InitializeEntityBridge();
                }
            }

			return defaultInstance;
		}

		/// <summary>
		/// Should the manager update?
		/// </summary>
		public bool update = true;

		private HashSet<IDeformable> deformables = new HashSet<IDeformable> ();
		private HashSet<IDeformable> immediateDeformables = new HashSet<IDeformable> ();

		/// <summary>
		/// Temporary storage for added deformables to allow them to be updated immediately on the first frame they're added
		/// </summary>
		private HashSet<IDeformable> addedDeformables = new HashSet<IDeformable> ();

        public IEnumerable<IDeformable> AllDeformables => deformables.Union(immediateDeformables);
        public int NumDeformables { get; private set; } = 0;

		private void Update ()
		{
			if (update)
			{
				CompleteDeformables (deformables);
				ScheduleDeformables (deformables);
				ScheduleDeformables (immediateDeformables);
			}

			// Move added deformables into the main deformables collection
			foreach (var added in addedDeformables)
			{
				if (added != null)
				{
					if (added.UpdateFrequency == UpdateFrequency.Default)
						deformables.Add(added);
					else
						immediateDeformables.Add(added);
				}
			}

			addedDeformables.Clear();
		}

        private void LateUpdate()
		{
			if (update)
				CompleteDeformables (immediateDeformables);
		}

		private void OnDisable ()
		{
			CompleteDeformables (deformables);
			CompleteDeformables (immediateDeformables);
		}

        /// <summary>
        /// Creates a chain of work from the deformables and schedules it.
        /// </summary>
        public void ScheduleDeformables(HashSet<IDeformable> deformables)
        {
            foreach (var deformable in deformables)
            {
                deformable.PreSchedule();
            }

            foreach (var deformable in deformables)
            {
                deformable.Schedule();
            }

            // Schedule the chain.
			JobHandle.ScheduleBatchedJobs ();
		}

		/// <summary>
		/// Finishes the scheduled work chain.
		/// </summary>
		public void CompleteDeformables (HashSet<IDeformable> deformables)
		{
			foreach (var deformable in deformables)
			{
				deformable.Complete();
				deformable.ApplyData();
			}
		}

		/// <summary>
		/// Registers a deformable to be updated by this manager.
		/// </summary>
		public void AddDeformable (IDeformable deformable)
		{
			addedDeformables.Add (deformable);
			// Force an immediate update so the deformable isn't undeformed on the first frame.
			deformable.ForceImmediateUpdate ();
			// Since changes from the previous frame are applied on the next, schedule changes now so that
			// when the next frame arrives the reset data from the immediate update isn't applied.
			deformable.PreSchedule ();
			deformable.Schedule ();
            if (Globals.IsHybridRenderer)
            {
                AddToEntityBridge(deformable as Deformable);
            }

            NumDeformables++;
        }

		/// <summary>
		/// Unregisters a deformable from this manager.
		/// </summary>
		public void RemoveDeformable (IDeformable deformable)
		{
            if (Globals.IsHybridRenderer)
            {
                RemoveFromEntityBridge(deformable as Deformable);
            }
			addedDeformables.Remove (deformable);
			deformables.Remove (deformable);
			immediateDeformables.Remove(deformable);
            NumDeformables--;
        }
	}
}
