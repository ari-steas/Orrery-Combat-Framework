using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public string[] GetAllSubparts(IMyEntity entity)
        {
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
        public void RotateSubpartAbs(MyEntitySubpart subpart, MatrixD matrix)
        {
            matrix.Translation = subpart.PositionComp.WorldMatrixRef.Translation;
            subpart.PositionComp.SetWorldMatrix(ref matrix);
        }
    }
}
