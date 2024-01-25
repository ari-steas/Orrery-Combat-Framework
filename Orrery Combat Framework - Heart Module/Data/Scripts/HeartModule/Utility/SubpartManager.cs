using System.Collections.Generic;
using System.Linq;
using VRage.Game.Entity;
using VRage.ModAPI;
using VRageMath;

namespace Heart_Module.Data.Scripts.HeartModule.Utility
{
    public class SubpartManager
    {
        Dictionary<IMyEntity, Dictionary<string, MyEntitySubpart>> CachedSubparts = new Dictionary<IMyEntity, Dictionary<string, MyEntitySubpart>>();

        public MyEntitySubpart GetSubpart(IMyEntity entity, string name)
        {
            if (entity == null) return null;

            // Add entity if missing
            if (!CachedSubparts.ContainsKey(entity))
                CachedSubparts.Add(entity, new Dictionary<string, MyEntitySubpart>());

            // Check if subpart is cached
            if (!CachedSubparts[entity].ContainsKey(name))
            {
                MyEntitySubpart subpart;
                entity.TryGetSubpart(name, out subpart);
                if (subpart != null)
                    CachedSubparts[entity].Add(name, subpart);
                else
                    return null;
            }

            // Return subpart
            if (CachedSubparts[entity][name] == null)
            {
                MyEntitySubpart subpart = null;
                entity.TryGetSubpart(name, out subpart);

                if (CachedSubparts[entity][name] == null)
                {
                    CachedSubparts[entity].Remove(name);
                    return null;
                }
                else
                    CachedSubparts[entity][name] = subpart;
            }

            return CachedSubparts[entity][name];
        }

        /// <summary>
        /// Recursively find subparts.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public MyEntitySubpart RecursiveGetSubpart(IMyEntity entity, string name)
        {
            if (entity == null) return null;

            MyEntitySubpart desiredSubpart = GetSubpart(entity, name);
            if (desiredSubpart == null)
                foreach (var subpart in ((MyEntity)entity).Subparts.Values)
                    return RecursiveGetSubpart(subpart, name);
            return desiredSubpart;
        }

        public string[] GetAllSubparts(IMyEntity entity)
        {
            if (entity == null) return new string[0];
            return ((MyEntity)entity).Subparts.Keys.ToArray();
        }

        public void LocalRotateSubpart(MyEntitySubpart subpart, Matrix matrix)
        {
            Matrix refMatrix = matrix * subpart.PositionComp.LocalMatrixRef;
            refMatrix.Translation = subpart.PositionComp.LocalMatrixRef.Translation;
            subpart.PositionComp.SetLocalMatrix(ref refMatrix);
        }
        public void LocalRotateSubpartAbs(MyEntitySubpart subpart, Matrix matrix)
        {
            matrix.Translation = subpart.PositionComp.LocalMatrixRef.Translation;
            subpart.PositionComp.SetLocalMatrix(ref matrix);
        }
    }
}
