using System;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_HYBRID_RENDERER_V2
using Unity.Entities;
using Unity.Physics.Authoring;
#endif

namespace Deform
{
    public partial class DeformableManager
    {
#if ENABLE_HYBRID_RENDERER_V2
        Dictionary<Deformable, Entity> m_DeformableEntityMapping = new Dictionary<Deformable, Entity>();
        GameObjectConversionSettings m_Settings;
        World m_World;
        BlobAssetStore m_BlobStore;

        IEnumerable<KeyValuePair<Deformable, Entity>> DeformableEntityPairs => m_DeformableEntityMapping;

        public void OnDestroy()
        {
            m_BlobStore.Dispose();
        }
#endif

        void InitializeEntityBridge()
        {
#if ENABLE_HYBRID_RENDERER_V2
            Debug.Log($"Initializing ECS bridge.");
            m_BlobStore = new BlobAssetStore();
            m_World = World.DefaultGameObjectInjectionWorld;
            m_Settings = GameObjectConversionSettings.FromWorld(m_World, m_BlobStore);
            m_Settings.ConversionFlags = GameObjectConversionUtility.ConversionFlags.AssignName;
            m_Settings.FilterFlags = WorldSystemFilterFlags.HybridGameObjectConversion;

#endif
        }

        void AddToEntityBridge(Deformable deformable)
        {
#if ENABLE_HYBRID_RENDERER_V2
            if (!deformable.TryGetComponent<PhysicsShapeAuthoring>(out var _))
            {
                Debug.LogWarning($"{deformable} has no DOTS physics component, refusing to register...");
                return;
            }
            var entity = GameObjectConversionUtility.ConvertGameObjectHierarchy(
                deformable.gameObject, m_Settings);
            m_DeformableEntityMapping.Add(deformable, entity);
            // The hybrid renderer will be rendering the mesh - the Deformable go becomes an invisible proxy
            deformable.TargetRenderer.enabled = false;
#endif
        }

        void RemoveFromEntityBridge(Deformable deformable)
        {
#if ENABLE_HYBRID_RENDERER_V2
            // TODO? Maybe we should destroy the entity here, but that's hard :(
            if (!m_DeformableEntityMapping.Remove(deformable))
            {
                Debug.LogWarning($"No entry for {deformable} in look-up table...");
            }
#endif
        }

    }
}
